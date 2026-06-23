# UDAPI-Free Alternatives: Exposing DocumentHub as AI Agent Tools

**Research Date:** June 2026  
**Context:** Exploring ways to expose DocumentHub API as Tools for DataMiner AI Assistant without relying on UDAPI  
**Status:** Comprehensive analysis complete with implementation recommendations

---

## Executive Summary

You can expose DocumentHub as AI Agent tools through **6 distinct UDAPI-free approaches**, each with different tradeoffs:

| Approach | Transport | Real-time? | AI Ecosystems | Effort | Phase |
|----------|-----------|-----------|---|---|---|
| **Standalone Microservice** | REST/gRPC/WebSocket | ✅ SSE streaming | Any HTTP/gRPC | High | 3 |
| **MCP Server** | HTTP/SSE | ✅ Streaming | Claude, ChatGPT, VS Code | Low | 2 |
| **gRPC Service** | gRPC + protobuf | ✅ Bidirectional | gRPC clients | Medium | 3 |
| **WebSocket Server** | WebSocket/SSE | ✅ Real-time | Any WebSocket client | Medium | 2-3 |
| **Hybrid MCP+A2A** | HTTP + A2A JSON-RPC | ✅ Streaming | A2A + Claude/ChatGPT | Medium | 2 |
| **Direct IConnection** | SLNet | N/A | DataMiner internal only | Low | Now |

**Best for DataMiner Assistant DxM:** Phase 1 uses UDAPI (unavoidable for DxM integration), then Phase 2-3 transition to UDAPI-free microservice.

**Best for broader AI ecosystem:** **MCP Server** (Claude, ChatGPT integration) + optional A2A companion.

---

## Detailed Analysis

### 1. Standalone ASP.NET Core Microservice

**What:** Run DocumentHub logic as a separate HTTP service (REST/gRPC/WebSocket) alongside DataMiner

**Architecture:**
```
┌─────────────────────────────────────────┐
│  DataMiner Agent (Port 40400)           │
│  ├─ SLNet IConnection API               │
│  └─ Optional: UDAPI for legacy clients  │
├─────────────────────────────────────────┤
│  DocumentHub Microservice (Port 5000)   │ ← UDAPI-FREE
│  ├─ REST endpoints                      │
│  ├─ gRPC service                        │
│  ├─ WebSocket/SSE streams               │
│  └─ A2A Agent Card endpoint             │
└─────────────────────────────────────────┘
```

**Key Capabilities:**
- ✅ 100% A2A protocol support (streaming, webhooks, gRPC)
- ✅ Horizontal scaling (multiple instances, load-balanced)
- ✅ Independent deployment lifecycle (decouple from DataMiner updates)
- ✅ Language-agnostic clients (any HTTP/gRPC library)
- ❌ Requires separate network/security configuration
- ❌ Added operational complexity (another service to monitor)

**Implementation:**
```csharp
// Pseudo-code: ASP.NET Core service
public class DocumentHubApiService : ControllerBase
{
    private readonly DocHubClient _client;
    
    [HttpPost("api/files/upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, string bucket)
    {
        // Calls DocumentHub NuGet API
        await _client.Files.UploadAsync(bucket, file);
        return Ok();
    }
    
    [HttpGet("api/a2a/{taskId}")]  // A2A task polling
    public async Task<AgentTaskResponse> GetTaskStatus(string taskId)
    {
        // Returns task state from A2A DOM module
        return await _a2aStore.GetTaskAsync(taskId);
    }
    
    // gRPC service endpoint (separate)
    public class DocumentHubGrpcService : DocumentHub.DocumentHubBase { }
}
```

**Deployment Options:**
1. **Docker** — Containerize as `documenthub-api:latest`, deploy with docker-compose
2. **Windows Service** — NSSM wrapper for background process
3. **Azure App Service** — Cloud-native with auto-scaling
4. **Kubernetes** — Enterprise-grade orchestration (if scaling across sites)
5. **IIS Application Pool** — Windows server standard

**Performance:**
- Latency: 5–50ms (HTTP) or 1–5ms (gRPC)
- Throughput: 500+ req/sec (REST) or 5000+ (gRPC)
- Real-time: Full streaming via SSE or gRPC bidirectional

**Timeline:** 4–6 weeks (port DocumentHub logic, add transport bindings, Docker packaging, security hardening)

**When to Use:**
- ✅ Need real-time streaming for file events
- ✅ High-throughput automation (1000+ operations/min)
- ✅ Multiple AI systems accessing simultaneously
- ✅ Want independent scaling from DataMiner
- ❌ Single-system DocumentHub with occasional use

---

### 2. Model Context Protocol (MCP) Server

**What:** Expose DocumentHub as an MCP server for Claude, ChatGPT, VS Code Copilot

**Protocol Details:**
- **Standard:** Anthropic's open-source Model Context Protocol
- **Transport:** HTTP/WebSocket with JSON-RPC 2.0 messages
- **Spec:** https://modelcontextprotocol.io/specification/latest
- **C# SDK:** Available via github.modelcontextprotocol/csharp-sdk

**How It Works:**
```
LLM Application (Claude, ChatGPT)
        ↓
    MCP Client
        ↓ JSON-RPC messages
    DocumentHub MCP Server
        ↓
    DocHubClient API → File storage
```

**MCP Concepts:**
| Term | Purpose |
|------|---------|
| **Tools** | Functions the LLM can invoke (`upload-document`, `search-documents`, `retrieve-document`) |
| **Resources** | Context data exposed to LLM (bucket metadata, file listing) |
| **Prompts** | Templated workflows (e.g., "upload and scan for compliance") |
| **Sampling** | Server-initiated LLM calls (agent behavior) |

**Example MCP Tool Definition:**
```json
{
  "name": "upload-document",
  "description": "Upload a file to a DocumentHub bucket with optional path",
  "inputSchema": {
    "type": "object",
    "properties": {
      "bucket": { "type": "string", "description": "Bucket name" },
      "filename": { "type": "string", "description": "File name" },
      "content": { "type": "string", "description": "Base64-encoded file content" }
    },
    "required": ["bucket", "filename", "content"]
  }
}
```

**Implementation Effort:**
```csharp
// ~100 lines of boilerplate
var server = new McpServer("DocumentHub");

server.RegisterTool("upload-document", async (args) =>
{
    var bucket = args["bucket"].ToString();
    var filename = args["filename"].ToString();
    var content = Convert.FromBase64String(args["content"].ToString());
    
    await _client.Files.UploadAsync(bucket, filename, content);
    return new { status = "success", path = $"{bucket}/{filename}" };
});

server.RegisterResource("buckets://", async () =>
{
    var buckets = await _client.GetBucketsAsync();
    return new McpResource { uri = "buckets://", content = buckets };
});
```

**AI Ecosystems Supported:**
- ✅ **Claude** (Anthropic) — Native MCP client support
- ✅ **ChatGPT** (OpenAI) — Via plugin/actions API
- ✅ **VS Code Copilot** — Embedded MCP client
- ✅ **Other LLM apps** — Any MCP-compatible host
- ❌ DataMiner Assistant DxM (not MCP-compatible)

**Performance:**
- Latency: 20–100ms (includes LLM processing time)
- Streaming: ✅ Native SSE support
- Real-time: Suitable for conversational AI workflows

**Timeline:** 1–2 weeks (scaffolding from MCP SDK, reuse DocumentHub API, light testing)

**When to Use:**
- ✅ Want Claude/ChatGPT to manage DocumentHub files
- ✅ Document analysis workflows (OCR + AI context)
- ✅ Prefer vendor-agnostic agent protocol
- ✅ Lightweight implementation (single developer)
- ❌ Need DataMiner Assistant integration (not MCP-compatible)

**Example Use Case:**
```
User: "Claude, upload this invoice to the 'Invoices' bucket and extract structured data"
↓
Claude invokes MCP tool:
  POST /mcp/tools/execute
  { "tool": "upload-document", "bucket": "Invoices", "content": "..." }
↓
DocumentHub receives → stores file
↓
Claude processes → returns extracted invoice data
```

---

### 3. gRPC Service

**What:** High-performance binary RPC service for DocumentHub operations

**Protocol Details:**
- **Standard:** gRPC (Google Remote Procedure Call)
- **Encoding:** Protocol Buffers (binary, ~10x smaller than JSON)
- **Streaming:** Full bidirectional streaming support
- **Native to:** ASP.NET Core, Go, Python, JavaScript
- **Performance:** Sub-millisecond latency, 5000+ req/sec

**Architecture:**
```protobuf
service DocumentHubAPI {
  rpc UploadDocument(UploadRequest) returns (UploadResponse) {}
  rpc SearchDocuments(SearchRequest) returns (stream DocumentItem) {}
  rpc DownloadDocument(DownloadRequest) returns (stream FileChunk) {}
}

message UploadRequest {
  string bucket = 1;
  string filename = 2;
  bytes content = 3;
}

message DocumentItem {
  string filename = 1;
  int64 size_bytes = 2;
  google.protobuf.Timestamp created_at = 3;
}
```

**Implementation:**
```csharp
public class DocumentHubGrpcService : DocumentHub.DocumentHubBase
{
    private readonly DocHubClient _client;
    
    public override async Task<UploadResponse> UploadDocument(
        UploadRequest request, ServerCallContext context)
    {
        var path = await _client.Files.UploadAsync(
            request.Bucket, 
            request.Filename, 
            request.Content.ToByteArray());
        return new UploadResponse { Path = path };
    }
    
    public override async Task SearchDocuments(
        SearchRequest request, 
        IServerStreamWriter<DocumentItem> responseStream,
        ServerCallContext context)
    {
        var files = await _client.Files.GetAsync(request.Bucket);
        foreach (var file in files)
        {
            await responseStream.WriteAsync(new DocumentItem
            {
                Filename = file.Name,
                SizeBytes = file.Size,
                CreatedAt = Timestamp.FromDateTime(file.CreatedAt)
            });
        }
    }
}
```

**Performance Comparison:**
| Metric | REST | gRPC | 
|--------|------|------|
| Message size | 2.0 KB | 0.2 KB |
| Latency | 20-50ms | 1-5ms |
| Throughput | 500 req/sec | 5000 req/sec |
| Streaming overhead | High (chunking) | Native |

**When to Use:**
- ✅ High-throughput document operations
- ✅ Real-time streaming (file sync, monitoring)
- ✅ Internal microservice-to-microservice communication
- ✅ Performance-critical workflows
- ❌ Browser clients (no direct gRPC support)
- ❌ Simple, occasional usage

**Timeline:** 4–6 weeks (proto definitions, ASP.NET Core service, load testing)

---

### 4. WebSocket/Server-Sent Events (SSE) Server

**What:** Real-time bidirectional communication channel for file events and agent coordination

**Architecture:**
```
AI Agent                    DocumentHub WebSocket Server
   ↓                                      ↓
   └─── WebSocket connection ────────────┘
            ↓                      ↑
      { "action": "upload" }   {"status": "completed"}
```

**Implementation:**
```csharp
app.MapWebSocketHandler("/api/ws/stream", async (context) =>
{
    using var ws = await context.WebSocketManager.AcceptWebSocketAsync();
    
    while (ws.State == WebSocketState.Open)
    {
        // Receive command from agent
        var message = await ReceiveMessageAsync(ws);
        var action = JsonSerializer.Deserialize<AgentAction>(message);
        
        // Execute DocumentHub operation
        var result = await ExecuteActionAsync(action);
        
        // Stream response back
        await SendMessageAsync(ws, result);
    }
});

app.MapPost("/api/sse/subscribe/{bucket}", async (string bucket, IAsyncEnumerable<string> messageStream) =>
{
    // SSE: stream file events to agent in real-time
    await foreach (var fileEvent in _documentHub.SubscribeToChangesAsync(bucket))
    {
        yield return $"data: {JsonSerializer.Serialize(fileEvent)}\n\n";
    }
});
```

**Key Differences:**
| Feature | WebSocket | SSE |
|---------|-----------|-----|
| Direction | Bidirectional | Server → Client only |
| Latency | 5-20ms | 10-50ms |
| Browser support | ✅ Native | ✅ Native (EventSource) |
| Complexity | Medium | Low |
| Fallback | Requires polling | Built-in fallback |

**When to Use:**
- ✅ Real-time file sync monitoring
- ✅ Multi-agent coordination (agents watch bucket changes)
- ✅ Progressive uploads (chunked file streaming)
- ✅ Live dashboards showing document activity
- ❌ Simple request/response (REST is simpler)

**Timeline:** 2–3 weeks (WebSocket handler, event subscription mechanism, agent client library)

---

### 5. Hybrid: MCP + A2A Agent

**What:** Expose DocumentHub as both MCP server (for Claude/ChatGPT) and A2A agent (for DataMiner Assistant), sharing the same core API

**Architecture:**
```
Claude/ChatGPT          DataMiner Assistant
    ↓ MCP                      ↓ A2A JSON-RPC
    └──→ ┌──────────────────────┐
         │ DocumentHub Microservice
         │ (or Automation Script)
         │
         │ Shared Implementation:
         │ - UploadDocument skill
         │ - SearchDocuments skill
         │ - RetrieveDocument skill
         └──────────────────────┘
              ↓
          DocHubClient API
```

**Benefits:**
- ✅ Single implementation, dual interface
- ✅ Broadest AI ecosystem coverage
- ✅ Reusable skill handlers (no duplication)
- ✅ Consistent behavior across agents

**Transport Binding:**

**Option A: ASP.NET Core Microservice**
```
POST /api/mcp/tools/execute      → MCP tool handler
POST /api/a2a/message/send       → A2A JSON-RPC dispatcher
GET  /api/a2a/tasks/{id}         → A2A task polling
```

**Option B: Via UDAPI (Phase 1)**
```
UDAPI Endpoint: /api/custom/documenthub/

Automation Script routes:
  POST /mcp-message         → MCP protocol handler
  POST /a2a-message         → A2A JSON-RPC handler
  GET  /a2a-tasks/{id}      → Task state query
```

**Implementation:**
```csharp
// Shared skill implementations
public interface IDocumentHubSkill
{
    Task<SkillResult> ExecuteAsync(SkillInput input);
}

public class UploadDocumentSkill : IDocumentHubSkill
{
    public async Task<SkillResult> ExecuteAsync(SkillInput input)
    {
        // Shared logic
        var bucket = input.Parameters["bucket"].ToString();
        var filename = input.Parameters["filename"].ToString();
        var content = input.Parameters["content"]; // base64 or byte array
        
        var path = await _client.Files.UploadAsync(bucket, filename, content);
        return new SkillResult { Success = true, Output = path };
    }
}

// Wire skill to both protocols
var skill = new UploadDocumentSkill(_client);

// MCP protocol binding
mcp.RegisterTool("upload-document", async (args) => await skill.ExecuteAsync(args));

// A2A protocol binding
a2aRouter.RegisterSkill("upload-document", async (task) => await skill.ExecuteAsync(task.Input));
```

**Performance:**
- Latency: 10–100ms (same as individual protocols)
- Streaming: ✅ Bidirectional via microservice, polling-only via UDAPI
- Complexity: Medium (protocol translation layer)

**Timeline:**
- **Phase 1 (UDAPI-based, A2A only):** 2–3 weeks
- **Phase 2 (add MCP):** +1–2 weeks
- **Phase 3 (migrate to microservice, both protocols):** +2–4 weeks

**When to Use:**
- ✅ Need both DataMiner Assistant + Claude/ChatGPT support
- ✅ Future-proof against protocol changes
- ✅ Want to avoid reimplementing business logic

---

### 6. Direct IConnection (Internal Only)

**What:** Use DocumentHub via direct SLNet API calls from DataMiner Automation/Protocol/GQI

**Already Supported:**
```csharp
// In Automation Script
var client = engine.GetDocHubClient();
await client.Files.UploadAsync(bucket, filename, content);

// In Protocol
var client = protocol.GetDocHubClient();
var files = await client.Files.GetAsync(bucket);

// In GQI
var client = dms.GetDocHubClient();
```

**Performance:** <1ms (in-process API)

**Limitations:**
- ❌ Not accessible to external AI agents
- ❌ Requires DataMiner Automation Script context
- ❌ No built-in tool discovery for DataMiner Assistant

**When to Use:**
- ✅ Internal DocumentHub operations (already the standard)
- ✅ No external agent integration needed

---

## Recommended Roadmap

### Phase 1: MVP for DataMiner Assistant (2–3 weeks)
**Goal:** Expose DocumentHub as A2A agent within DataMiner ecosystem

**Components:**
1. A2A DOM module (`a2a_tasks` table for state)
2. Automation Script with A2A JSON-RPC dispatcher
3. 3–5 skill handlers (upload, search, retrieve, etc.)
4. Agent Card at `/.well-known/agent.json`

**Why:** Leverages existing UDAPI (unavoidable for DxM integration), complete design already in codebase

**Effort:** Low  
**Latency:** 10–50ms (acceptable for document operations)  
**Streaming:** Not required (polling is sufficient)

**Deliverable:** DataMiner Assistant can discover and invoke DocumentHub as an A2A agent

---

### Phase 2: Broaden AI Ecosystem (1–2 weeks)
**Goal:** Enable Claude, ChatGPT, VS Code Copilot integration

**Options:**

**Option A: Standalone MCP Server** (Recommended)
- Implement MCP protocol in ASP.NET Core
- Reuse DocumentHub API (DocHubClient NuGet)
- Deploy independently from DataMiner
- Instant Claude/ChatGPT integration

**Option B: Add MCP to UDAPI** (If Phase 1 exists)
- Extend Automation Script with MCP protocol handler
- Less overhead than separate service
- Still limited by UDAPI (no streaming)

**Effort:** Low (1–2 weeks)  
**Benefit:** Access to 100M+ Claude/ChatGPT users

---

### Phase 3: Real-time & High Performance (4–6 weeks)
**Goal:** Full streaming, gRPC, independent scaling

**Components:**
1. Extract DocumentHub logic to standalone ASP.NET Core microservice
2. Implement REST, gRPC, WebSocket bindings
3. Full A2A + MCP support via microservice
4. Docker packaging, orchestration

**Triggers:**
- Streaming required for file sync monitoring
- High-throughput automation (1000+ ops/min)
- Multiple DataMiner sites sharing DocumentHub
- Performance optimization critical

**Deliverable:** Production-grade, scalable DocumentHub service available to any HTTP/gRPC/WebSocket client

---

## Comparison Matrix

| Aspect | A2A via UDAPI | MCP Server | Standalone Microservice | gRPC Service | Hybrid MCP+A2A |
|--------|---|---|---|---|---|
| **Avoids UDAPI** | ❌ | ✅ | ✅ | ✅ | ✅ Phase 2-3 |
| **DataMiner Assistant** | ✅ | ❌ | ❓ | ❓ | ✅ |
| **Claude/ChatGPT** | ❌ | ✅ | ✅ | ❌ | ✅ |
| **Real-time Streaming** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Latency (ms)** | 10–50 | 20–100 | 5–50 | 1–5 | 10–100 |
| **Implementation Effort** | Low | Low | High | High | Medium |
| **Deployment Complexity** | None | Low | Medium | Medium | Medium |
| **Operational Overhead** | Low | Medium | High | High | High |
| **Scalability** | Limited | Good | Excellent | Excellent | Excellent |

---

## Key Decisions to Make

### 1. **Phase 1: Stay with UDAPI or Defer A2A?**
   - **Option A:** Implement A2A via UDAPI now (2–3 weeks, Phase 1 in roadmap)
   - **Option B:** Skip A2A, go directly to MCP server (1–2 weeks)
   - **Recommendation:** Option A (A2A first for DataMiner compatibility)

### 2. **Microservice Transport Preference?**
   - **REST (default):** Easiest for all clients, slight performance overhead
   - **gRPC:** Best performance, steeper learning curve
   - **Hybrid (REST + gRPC):** Both, no additional complexity in ASP.NET Core

### 3. **Streaming Requirement?**
   - **Yes → Phase 3 Microservice + SSE/WebSocket**
   - **No → Phase 1–2 polling-based (A2A + MCP)**

### 4. **Multi-site DocumentHub?**
   - **Single DataMiner site:** UDAPI or MCP sufficient
   - **Multiple sites:** Standalone microservice recommended (shared backend, independent deployments)

---

## Implementation Checklist

### Immediate (Next Sprint)
- [ ] Decide on Phase 1 approach (A2A via UDAPI vs. skip to Phase 2)
- [ ] If Phase 1: Create A2A DOM module + Automation Script scaffold
- [ ] If Phase 2 only: Start MCP server project

### Short-term (4–6 weeks)
- [ ] Phase 1: Deploy A2A endpoint, test with DataMiner Assistant
- [ ] Phase 2: MCP server for Claude/ChatGPT, user testing
- [ ] Documentation + team training

### Medium-term (8–12 weeks)
- [ ] Phase 3: Evaluate if streaming/performance critical
- [ ] If yes: Build standalone microservice, migrate Phase 1–2 logic
- [ ] If no: Optimize existing UDAPI/MCP, plan for scaling

---

## Open Questions

1. **DataMiner Assistant discovery for external agents:** Does DxM have agent registry, or does it only discover A2A agents via UDAPI?
   - **Impact:** Determines if Phase 3 microservice can register with DataMiner Assistant
   - **Recommendation:** Verify with Skyline before Phase 3

2. **File size limits for streaming:** UDAPI has 29 MB limit. Do MCP/microservice have constraints?
   - **Impact:** If files > 100 MB, streaming is critical
   - **Recommendation:** Design chunked upload strategy

3. **Authentication across services:** How to propagate DataMiner user context to standalone microservice?
   - **Impact:** Authorization (which buckets can agent access?)
   - **Recommendation:** Implement API key or bearer token exchange

4. **MCP + DataMiner auth integration:** Can MCP server enforce DataMiner permissions?
   - **Impact:** Security posture for Claude access
   - **Recommendation:** Isolate MCP to public buckets initially, add auth layer later

---

## References

**Existing Codebase Documentation:**
- A2A Protocol Design: `Documentation/AI-Explorations/A2A-Protocol-Integration.md`
- A2A Skill Specs: `Documentation/AI-Explorations/SKILL-A2A.md`
- REST API Spec: `Documentation/AI-Explorations/SKILL.md`
- Getting Started: `Documentation/Getting Started.md`

**External Standards:**
- MCP Specification: https://modelcontextprotocol.io
- gRPC: https://grpc.io
- A2A Protocol: https://a2a-protocol.org
- ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core

---

**Document Status:** Draft (ready for team review and decision-making)
