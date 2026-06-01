# A2A Protocol Integration — Architecture Exploration

> **Status:** Exploration / Pre-implementation  
> **Branch:** `AI_explorations`  
> **Date:** 2026-06-01  
> **Protocol version evaluated:** A2A v1.0.0 ([a2a-protocol.org](https://a2a-protocol.org))

---

## 1. What is the A2A Protocol?

[Agent2Agent (A2A)](https://a2a-protocol.org) is an open standard that enables AI agents from different vendors and frameworks to discover each other, delegate tasks, and exchange results — without sharing internal state, memory, or tool implementations.

### Core building blocks

| Concept | Description |
|---|---|
| **Agent Card** | JSON metadata at `/.well-known/agent.json` — declares identity, skills, endpoint, auth |
| **Task** | Stateful unit of work (`submitted → working → completed / failed / canceled`) |
| **Message / Part** | A turn in the conversation (text, file reference, or structured data) |
| **Artifact** | Output produced by the agent (document, bytes, structured result) |
| **Streaming** | SSE or gRPC for real-time progress; webhooks for push notifications |

### Protocol bindings

A2A supports three concrete transport bindings (all carry the same canonical proto data model):

- **JSON-RPC 2.0 over HTTP** — the most common binding
- **gRPC** — for high-throughput or strongly-typed clients
- **HTTP+JSON/REST** — simpler but less feature-complete

### Relationship to MCP

> *MCP connects agents to structured tools and data. A2A connects agents to other agents.*

Both are complementary. DocumentHub is a natural fit for **A2A** (it is an autonomous skill an AI agent could delegate document operations to) and also for **MCP** (individual file-system tools).

---

## 2. Why Expose DocumentHub as an A2A Agent?

DocumentHub manages files across Local, SharePoint Online, and DOM storage backends behind a consistent API. Exposing it as an A2A Remote Agent enables:

- Any A2A-compatible AI system (LangChain, Google ADK, Copilot, custom agents) to upload, search, and retrieve documents from DataMiner — without knowing the underlying storage details.
- Multi-agent workflows where a DocumentHub agent acts as a document repository for orchestrating agents.
- Standardized discovery: clients just fetch the Agent Card and know exactly what DocumentHub can do.

---

## 3. Integration Options

### Option A — Via DataMiner UDAPI (User Defined API)

DataMiner's [User-Defined API](https://docs.dataminer.services) feature exposes Automation Scripts as HTTP endpoints at `https://{dma}/api/custom/...`. An Automation Script acts as a JSON-RPC dispatcher.

#### Gap analysis

| A2A Requirement | UDAPI Capability | Gap |
|---|---|---|
| Agent Card (`GET /.well-known/agent.json`) | ✅ Static endpoint via UDAPI | None |
| `message/send` (initiate task) | ✅ POST → Automation Script | Script is sync; long tasks need DOM state store |
| `tasks/get` (poll status) | ✅ GET → Script reads DOM | None |
| `tasks/list` | ✅ GET → Script queries DOM | None |
| `tasks/cancel` | ✅ POST → Script updates DOM | None |
| SSE Streaming | ❌ UDAPI is request/response only | **Hard limitation** |
| Push Notifications (outbound webhooks) | ⚠️ Possible via `HttpWebRequest`, no retry | Partial |
| Bearer token auth | ✅ Native UDAPI token support | None |

**Verdict:** Viable for ~85% of the A2A spec. SSE streaming is the main gap. Clients poll `tasks/get` instead of receiving live updates. The A2A spec allows `"streaming": false` in the Agent Card — this is legitimate and widely supported.

---

### Option B — Standalone Microservice

A .NET / ASP.NET Core service deployed alongside DataMiner:
- Fully implements A2A (JSON-RPC + SSE + gRPC)
- Calls DocumentHub NuGet packages internally via `IConnection`
- Full protocol fidelity including streaming

**Pros:** 100% spec coverage, native SSE/gRPC  
**Cons:** Extra deployment unit, outside DataMiner's native lifecycle management

---

### Recommendation: Start with UDAPI, migrate to microservice if streaming is needed

UDAPI is the pragmatic starting point. The polling profile (`streaming: false`) is explicitly allowed by the spec and fits DataMiner's Automation Script execution model. The architecture below is designed so that the core A2A logic (dispatcher, skill handlers, DOM state store) can be extracted to a microservice later with minimal changes.

---

## 4. Required Architecture Changes

### 4.1 New SDM Module — `A2A` (DOM task state store)

A new DOM module to persist A2A task state across script invocations.

```
DevPack/SDM/
└── A2A/
    ├── Models/
    │   ├── A2ATask.cs            // taskId (string), status, skillId, contextId, created, updated
    │   ├── A2AMessage.cs         // role ("user"/"agent"), parts (JSON blob), taskId FK
    │   └── A2AArtifact.cs        // name, mimeType, reference to DocHub file path / DOM attachment
    ├── Repositories/
    │   ├── A2ATaskRepository.cs  // CRUD + filter by status, contextId
    │   └── A2AMessageRepository.cs
    └── Exposers/
        └── A2AExposers.cs
```

### 4.2 New API Layer — `A2A` (protocol handling)

```
DevPack/API/
└── A2A/
    ├── AgentCard/
    │   └── AgentCardBuilder.cs       // builds the /.well-known/agent.json response
    ├── JsonRpc/
    │   ├── A2ADispatcher.cs          // routes JSON-RPC method → skill handler
    │   └── A2ARequest.cs / A2AResponse.cs
    ├── Skills/
    │   ├── IDocHubSkill.cs           // interface: Task HandleAsync(A2ATask, Message)
    │   ├── UploadDocumentSkill.cs    // message → DocHubClient.Files.UploadFile()
    │   ├── SearchDocumentsSkill.cs   // message → DocHubClient.Files.ReadFiles()
    │   └── RetrieveDocumentSkill.cs  // message → DocHubClient.Files.GetBytes() → Artifact
    └── Mappers/
        └── ArtifactMapper.cs         // A2A Artifact ↔ IDocHubFile
```

### 4.3 New DevPack Package — `DevPack.A2A`

```
DevPack.A2A/
└── Extensions/
    └── IEngineA2AExtensions.cs       // engine.GetDocHubA2AAgent()
```

### 4.4 New Automation Script — `DocumentHub_A2A_Endpoint`

Registered as a UDAPI endpoint. Routes incoming HTTP calls to `A2ADispatcher` based on the JSON-RPC `method` field.

```
Script routes:
  GET  /.well-known/agent.json     → AgentCardBuilder.Build()
  POST /a2a  { method: "message/send"   } → A2ADispatcher → UploadDocumentSkill
                                                            → SearchDocumentsSkill
                                                            → RetrieveDocumentSkill
  POST /a2a  { method: "tasks/get"      } → A2ATaskRepository.GetById()
  POST /a2a  { method: "tasks/list"     } → A2ATaskRepository.ListByContext()
  POST /a2a  { method: "tasks/cancel"   } → A2ATaskRepository.UpdateStatus(Canceled)
```

---

## 5. Agent Card (Draft)

The Agent Card is what remote A2A clients discover to understand DocumentHub's capabilities.

```json
{
  "name": "DataMiner DocumentHub Agent",
  "description": "Manages documents across Local, SharePoint Online, and DOM storage backends within a DataMiner system.",
  "url": "https://{dataminer-hostname}/api/custom/documenthub/a2a",
  "version": "1.0.0",
  "documentationUrl": "https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub",
  "capabilities": {
    "streaming": false,
    "pushNotifications": false
  },
  "authentication": {
    "schemes": ["Bearer"]
  },
  "skills": [
    {
      "id": "upload-document",
      "name": "Upload Document",
      "description": "Upload a file to a named DocumentHub bucket. Supports Local, SharePoint, and DOM storage.",
      "tags": ["documents", "upload", "storage"],
      "inputModes": ["text", "file"],
      "outputModes": ["text"]
    },
    {
      "id": "search-documents",
      "name": "Search Documents",
      "description": "List or search files in a DocumentHub bucket, optionally filtered by name.",
      "tags": ["documents", "search", "list"],
      "inputModes": ["text"],
      "outputModes": ["text", "data"]
    },
    {
      "id": "retrieve-document",
      "name": "Retrieve Document",
      "description": "Retrieve the bytes of a file from a DocumentHub bucket and return it as an A2A artifact.",
      "tags": ["documents", "download", "retrieve"],
      "inputModes": ["text"],
      "outputModes": ["file"]
    }
  ]
}
```

---

## 6. A2A Task Lifecycle in DocumentHub

```
Client                         DocumentHub A2A Endpoint          DOM State Store
  │                                      │                              │
  │── POST /a2a {method: message/send} ──▶                              │
  │                                      │── Create A2ATask (submitted) ──▶
  │                                      │── Route to skill handler      │
  │                                      │── Execute (upload/search/...) │
  │                                      │── Update status (working)   ──▶
  │                                      │── Produce Artifact            │
  │                                      │── Update status (completed) ──▶
  │◀── Response: Task{id, status} ───────│                              │
  │                                      │                              │
  │── POST /a2a {method: tasks/get} ─────▶                              │
  │                                      │── Read A2ATask ◀─────────────│
  │◀── Response: Task{status, artifacts}─│                              │
```

For short-lived operations (upload, search), the task can complete synchronously within the same `message/send` call and return status `completed` immediately.

---

## 7. Interaction Examples

### Upload a document via A2A

```http
POST https://{dma}/api/custom/documenthub/a2a
Authorization: Bearer <token>
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": "req-001",
  "method": "message/send",
  "params": {
    "message": {
      "role": "user",
      "parts": [
        {
          "kind": "text",
          "text": "Upload the file at C:\\Reports\\Q1.pdf to the 'Reports' bucket under the path qualifier '2024/Q1'."
        }
      ]
    },
    "configuration": {
      "acceptedOutputModes": ["text"]
    }
  }
}
```

**Response (task completed synchronously):**

```json
{
  "jsonrpc": "2.0",
  "id": "req-001",
  "result": {
    "id": "task-abc123",
    "status": { "state": "completed" },
    "artifacts": [
      {
        "name": "upload-result",
        "parts": [
          { "kind": "text", "text": "File uploaded successfully to /reports/2024/Q1/Q1.pdf" }
        ]
      }
    ]
  }
}
```

### Search documents via A2A

```http
POST https://{dma}/api/custom/documenthub/a2a
Authorization: Bearer <token>
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": "req-002",
  "method": "message/send",
  "params": {
    "message": {
      "role": "user",
      "parts": [
        { "kind": "text", "text": "List all PDF files in the 'Reports' bucket matching 'monthly'." }
      ]
    }
  }
}
```

---

## 8. Dependencies & Prerequisites

| Dependency | Minimum Version | Notes |
|---|---|---|
| DataMiner | 10.3.6+ | Required for UDAPI |
| UserDefinableApiEndpoint DxM | Latest | Must be installed on the DMA |
| Indexing database | Elasticsearch / OpenSearch / STaaS | Required for UDAPI token storage |
| DocumentHub solution | Latest | Must be deployed before the A2A endpoint |

---

## 9. Open Questions

1. **Natural language intent parsing** — `message/send` receives free-text. The Automation Script must map user intent to a specific skill (`upload-document`, `search-documents`, etc.). Options: (a) require structured input using a `data` Part, (b) use a lightweight LLM call for intent classification, (c) declare a strict JSON input schema per skill in the Agent Card.

2. **File transfer** — A2A supports `file` Parts with inline base64 or a URI reference. For large files, URI references pointing to an already-accessible path are preferable. Inline base64 upload would require base64 decode + temp file write before calling `DocHubClient.Files.UploadFile()`.

3. **DOM module naming** — The A2A task state DOM module needs a reserved module ID (e.g., `documenthub_a2a_tasks`).

4. **Streaming upgrade path** — If SSE streaming becomes required, the natural upgrade is an ASP.NET Core sidecar service that references the same `DevPack` NuGet packages. The skill handlers (`IDocHubSkill`) would be reused unchanged.

---

## 10. References

- [A2A Protocol Specification v1.0.0](https://a2a-protocol.org)
- [A2A GitHub Repository](https://github.com/a2aproject/A2A)
- [DataMiner User-Defined APIs](https://docs.dataminer.services)
- [DocumentHub Getting Started](../Getting%20Started.md)
- [DocumentHub Quick Reference](../Quick%20Reference.md)
- [DocumentHub A2A Skill Definition](./SKILL-A2A.md)
