# DocumentHub A2A Agent — AI Skill Definition

> **Status:** Draft (pre-implementation)  
> **Protocol:** A2A v1.0.0  
> **Transport:** JSON-RPC 2.0 over HTTP (polling profile — streaming not supported)

---

## Overview

The **DocumentHub A2A Agent** exposes DataMiner's DocumentHub solution as an A2A Remote Agent. It enables AI agents to upload, search, and retrieve documents stored across Local, SharePoint Online, and DOM storage backends — without knowing the underlying storage details.

Use this skill when you need to:
- Upload files to a DataMiner-managed document bucket
- List or filter files in a bucket
- Retrieve file contents from a bucket
- Query available buckets, DOM sources, or SharePoint configurations

**Discovery:** Fetch the Agent Card from the well-known URL to confirm available skills and auth requirements.

---

## Agent Card Endpoint

```
GET https://{dataminer-hostname}/api/custom/documenthub/.well-known/agent.json
```

No authentication required to fetch the Agent Card.

---

## Base Endpoint

```
https://{dataminer-hostname}/api/custom/documenthub/a2a
```

All A2A operations are submitted as JSON-RPC 2.0 `POST` requests to this URL.

---

## Authentication

All requests (except Agent Card discovery) require a Bearer token:

```http
Authorization: Bearer <your-api-token>
```

Tokens are created in DataMiner Cube under **System Center → User-Defined APIs**.

---

## Capabilities

| Capability | Supported |
|---|---|
| Streaming (SSE) | ❌ No |
| Push Notifications | ❌ No |
| Synchronous task completion | ✅ Yes (short operations complete in-band) |
| Async polling (`tasks/get`) | ✅ Yes |

---

## Skills

### `upload-document`

Upload a file to a named DocumentHub bucket.

**Input:** Free-text instruction or structured data part specifying:

| Field | Required | Description |
|---|---|---|
| `bucket` | Yes | Name of the target Document Bucket |
| `filePath` | Yes | Full path of the file on the DataMiner Agent file system |
| `customName` | No | Custom filename (without extension) |
| `pathQualifier` | No | Subfolder path within the bucket (not supported for DOM storage) |
| `domInstanceId` | No | GUID of a DOM instance to link the file to |

**Output:** Text artifact confirming the uploaded path.

**Example message:**
```json
{
  "role": "user",
  "parts": [
    {
      "kind": "text",
      "text": "Upload C:\\Reports\\Q1.pdf to the 'Reports' bucket under path qualifier '2024/Q1'."
    }
  ]
}
```

---

### `search-documents`

List or search files in a DocumentHub bucket.

**Input:**

| Field | Required | Description |
|---|---|---|
| `bucket` | Yes | Name of the Document Bucket to search |
| `filter` | No | Case-insensitive substring filter on filename |

**Output:** Data artifact — array of matching file descriptors (`name`, `path`, `storageType`).

**Example message:**
```json
{
  "role": "user",
  "parts": [
    {
      "kind": "text",
      "text": "List all files in the 'Reports' bucket that match 'monthly'."
    }
  ]
}
```

---

### `retrieve-document`

Retrieve the bytes of a file from a DocumentHub bucket and return it as an A2A file artifact.

**Input:**

| Field | Required | Description |
|---|---|---|
| `bucket` | Yes | Name of the Document Bucket |
| `fileName` | Yes | Name of the file to retrieve (with extension) |

**Output:** File artifact containing the raw file bytes (base64-encoded inline) or a URI reference.

**Example message:**
```json
{
  "role": "user",
  "parts": [
    {
      "kind": "text",
      "text": "Retrieve the file 'Q1.pdf' from the 'Reports' bucket."
    }
  ]
}
```

---

## JSON-RPC Methods Reference

| Method | Description |
|---|---|
| `message/send` | Send a user message; initiates a new task or continues an existing context |
| `tasks/get` | Poll the status and artifacts of an existing task |
| `tasks/list` | List tasks, optionally filtered by contextId |
| `tasks/cancel` | Request cancellation of a running task |

---

## Full JSON-RPC Request/Response Examples

### Initiate a task (`message/send`)

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
          "text": "Upload C:\\Temp\\report.pdf to the 'Reports' bucket."
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
          { "kind": "text", "text": "File uploaded to /reports/report.pdf" }
        ]
      }
    ]
  }
}
```

---

### Poll task status (`tasks/get`)

```http
POST https://{dma}/api/custom/documenthub/a2a
Authorization: Bearer <token>
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": "req-002",
  "method": "tasks/get",
  "params": {
    "id": "task-abc123"
  }
}
```

**Response:**

```json
{
  "jsonrpc": "2.0",
  "id": "req-002",
  "result": {
    "id": "task-abc123",
    "status": { "state": "completed" },
    "artifacts": [...]
  }
}
```

---

### Cancel a task (`tasks/cancel`)

```http
POST https://{dma}/api/custom/documenthub/a2a
Authorization: Bearer <token>
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": "req-003",
  "method": "tasks/cancel",
  "params": {
    "id": "task-abc123"
  }
}
```

---

## Task States

| State | Description |
|---|---|
| `submitted` | Task received and queued |
| `working` | Skill handler is executing |
| `completed` | Task finished successfully; artifacts are available |
| `failed` | Task encountered an error; check `status.message` |
| `canceled` | Task was canceled by the client |

---

## Error Codes

Standard JSON-RPC 2.0 error codes plus A2A-defined codes:

| Code | Meaning |
|---|---|
| `-32700` | Parse error |
| `-32600` | Invalid request |
| `-32601` | Method not found |
| `-32602` | Invalid params |
| `-32603` | Internal error |
| `-32001` | Task not found |
| `-32002` | Task not cancelable |
| `-32003` | Unsupported operation |

---

## Limitations

- **No streaming** — SSE is not supported; use `tasks/get` polling for long-running tasks
- **No push notifications** — no outbound webhooks
- **File deletion** — only supported for `Local` storage buckets (same constraint as the REST API)
- **File upload via path** — `filePath` must be accessible on the DataMiner Agent file system; inline base64 upload is not yet supported
- **Auth token rotation** — token secrets are not retrievable after creation; rotate with a new token if lost
- Requires **DataMiner 10.3.6+** with `UserDefinableApiEndpoint` DxM installed
- Requires an **indexing database** (Elasticsearch, OpenSearch, or STaaS)

---

## See Also

- [A2A Protocol Integration Architecture](./A2A-Protocol-Integration.md)
- [DocumentHub Getting Started](../Getting%20Started.md)
- [DocumentHub REST API Skill](../../SKILL.md)
- [A2A Protocol Specification](https://a2a-protocol.org)
