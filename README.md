# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

This documentation describes how to use the public API exposed by `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub`. The API is intended to be used when developing custom solutions based on the DocumentHub solution.

## Installation

Add the NuGet package to your solution:

```
dotnet add package Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub
```

Depending on your project type, one of the following additional packages is also required:

- Automation scripts: `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub.Automation`
- Protocols: `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub.Protocol`
- GQI Ad-hoc data sources and custom operators: `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub.GQI`

> **Note**
> This library targets `.NET Framework 4.8`.

Version 1.3 requires DataMiner **10.6.10 / 10.7.0** for native Automation
credentials. Source management is administrator-only. SharePoint secrets use
source-scoped Credentials Library entries declared on the DocumentHub scripts that
call `engine.GetCredential`; DOM shares use native username/password credentials. There are no normal
DPAPI writes or fallbacks. Existing installations require the explicit, per-agent
[credential migration](Documentation/CredentialStorage.md) before resuming use.

### Authentication dependency compatibility

SharePoint uses MSAL (`Microsoft.Identity.Client`) client-credentials authentication through
Kiota's Task-based bearer-token provider. Saved-secret checks, candidate-secret checks, and
file operations share this implementation. Each provider reuses its MSAL app token cache,
requests `https://graph.microsoft.com/.default`, and sends bearer tokens only to the HTTPS
Graph host. Claims challenges and cancellation are forwarded to MSAL.

The executing authentication path does not use Azure.Identity or Azure.Core's
`TokenCredential.GetTokenAsync`/`ValueTask<T>` contract, avoiding its assembly-identity
conflict with DataMiner's authentication dependencies. The focused client uses Kiota's
HTTP and JSON runtime packages, not the full Microsoft.Graph SDK, Graph.Core, or the
Kiota Azure authentication package. MSAL and both authentication providers are unchanged.
Shared runtime versions previously supplied transitively by Graph are retained explicitly
to avoid dependency downgrades when restoring the smaller package.
No changes to DataMiner-owned DLLs or executable configuration are required.
`SharePointDependency_Tests` covers token acquisition with mocked OAuth responses, caching,
claims, cancellation, trusted-host restrictions, and the absence of direct Azure references.

After rebuilding an unpublished package without changing its version, remove only that
package version from the global NuGet cache and the consuming packaging projects' local
`packages` folders, then restore and rebuild. Deploying a new package does not unload
assemblies already loaded by the Agent; an Agent still running an earlier DevPack may
need a process restart to pick up the replacement.
Do not delete shared DLL folders indiscriminately, as other solutions may depend on them.

### Version 2 migration and DLL size

Version 2 replaces the full Graph SDK with an internal Kiota-generated client for the
existing SharePoint site/library lookups, file/folder operations, recursive paging,
KQL search, and batched file-URL resolution. Local and DOM storage are unchanged.
SharePoint deletion and resumable uploads have not been added.

Upgrade the core, Automation, Protocol, and GQI NuGet libraries together and rebuild
consumers. `SharePointPageData.PageRemainderBuffer` and
`SharePointSearchPageData.PageRemainderBuffer` are now internal. Consumers must use
the public paging-context APIs instead of reading or changing these buffers.
The high-level file APIs remain unchanged, but existing binaries that depend on
Graph model types or these removed public members are not compatible.

Build and pack targets reject each shipping DLL larger than **20,971,520 bytes
(20 MiB)** and reject reintroduction of the full Graph SDK. This includes resolved
runtime dependencies, not only the DocumentHub DLL or the compressed NuGet size.
MSAL remains a supported dependency and is checked against the same size limit.

### Regenerating the Graph client

The small OpenAPI definition and generated C# sources are checked in under
`DevPack\API\StorageHandlers\Graph`. Normal builds do not run Kiota or download a schema.
After updating the API definition, run this command from the repository root:

```powershell
.\DevPack\API\StorageHandlers\Graph\Generate-Client.ps1
```

The script restores the pinned local Kiota tool and regenerates only the `Generated`
directory. Do not hand-edit its C# files or `kiota-lock.json`.
The handwritten partial client supplies Graph's site/file path aliases and batches
metadata GETs in groups of at most 20. Inner batch failures are surfaced, even when
the outer HTTP status is 200; the handler retains the pending search page for a retry.
OAuth remains implemented by MSAL, not by this generated client.

## Documentation

| Document                                              | Description                                                                  |
| ----------------------------------------------------- | ---------------------------------------------------------------------------- |
| [Getting Started](Documentation/Getting%20Started.md) | Installation, prerequisites, and basic usage                                 |
| [Quick Reference](Documentation/Quick%20Reference.md) | Common code snippets for file operations, repositories, and storage backends |
| [Credential storage](Documentation/CredentialStorage.md) | Verified APIs, permissions, deterministic references, migration and integration prerequisites |

External resources:

- [DocumentHub](https://docs.dataminer.services/solutions/standard_solutions/DocumentHub/DocumentHub.html) – Official DocumentHub solution documentation

## Unit Tests

The `DevPack.Tests` project contains an in-memory DOM mock (`DomSLNetMessageHandler`), temporary local files,
and mocked HTTP responses for MSAL and Kiota; no live DataMiner or SharePoint connection is required.

### Test Coverage

| Area | Test class | Test cases |
| --- | --- | ---: |
| API - DocHubClient | `DocHubClient_Tests` | 3 |
| API - Files Upload | `Files_UploadFile_Tests` | 16 |
| API - Files Read | `Files_ReadFiles_Tests` | 5 |
| API - Files Delete | `Files_DeleteFile_Tests` | 6 |
| API - Local Public Storage | `LocalHandler_Tests` | 19 |
| API - Files Search | `Files_SearchFiles_Tests` | 5 |
| API - File Download | `Files_DownloadFile_Tests` | 9 |
| API - File Validation | `FileValidator_ValidateAndSanitizeFile_Tests` | 19 |
| API - File Name Validation | `FileValidator_FileName_Tests` | 19 |
| API - Local File Adapter | `FileInfoAdapter_GetRelativePath_Tests` | 11 |
| API - DOM File Adapter | `DomFileAdapter_Tests` | 6 |
| API - DOM Attachment Paths | `DomAttachmentsHandler_Tests` | 7 |
| API - Secret Keys | `SecretKey_Tests` | 22 |
| API - Encrypted Secret Storage | `DpapiFileSecretStore_Tests` | 7 |
| API - Secrets | `Secrets_Tests` | 14 |
| API - Secret Management Script | `ManageSharePointSecretScript_Tests` | 6 |
| SharePoint Authentication | `SharePointDependency_Tests` | 12 |
| SharePoint Kiota Client | `KiotaClient_Tests` | 7 |
| Credentials | `NativeCredentialMessaging_Tests` | 34 |
| SDM - SharePoint CRUD | `SharepointDomRepository_CRUD_Tests` | 6 |
| SDM - SharePoint Filters | `SharepointDomRepository_FilterTests_Tests` | 7 |
| SDM - DomSource CRUD | `DomSourceDomRepository_CRUD_Tests` | 6 |
| SDM - DomSource Filters | `DomSourceDomRepository_FilterTests_Tests` | 7 |
| SDM - DocumentBucket CRUD and Middleware | `DocumentBucketDomRepository_CRUD_Tests` | 16 |
| SDM - DocumentBucket Filters | `DocumentBucketDomRepository_FilterTests_Tests` | 10 |
| **Total** | **25 classes** | **279** |

### Kiota Client Tests

The focused Kiota tests use mocked HTTP responses with the real generated client
and JSON serialization. They cover encoded root/nested file operations, upload
bytes and folder request bodies, search-result metadata, 20+1 metadata batching
with out-of-order responses, search-page recovery after inner 403/429 failures,
and binary downloads through Kiota's redirect middleware without forwarding the
Graph bearer token. They require no live DataMiner or SharePoint connection.
Existing MSAL authentication tests are unchanged.

### SDM Repository Tests

The **SDM module cases** are split across **SharePoint**,
**DomSource**, and **DocumentBucket**. Each module has separate CRUD
and filter test classes:

- **CRUD**: Create, Update, ReadPaged, DeleteSingle, DeleteBulk, and Count for all three modules; CreateOrUpdate for DocumentBucket.
- **Filters**: Equality, substring matching, AND/OR combinations, and unfiltered TRUE queries for module-specific fields.
- **Middleware**: DocumentBucket upload-path sanitization on both create and update, with parameterized inputs.

### API Tests

API tests validate parameter handling:
- Null category, filePath, query, and domInstanceId validation
- Storage type restrictions (e.g., DOM storage with qualifier overload)
- DomSource module validation
- Bucket type restrictions for the search functionality

### File Name Validation Tests

`FileValidator_FileName_Tests` covers the user-facing checks applied to the name a file is stored under (the uploaded file's own name, or the optional new name supplied by the caller):
- Characters the storage backends reject (`< > : " | ? *` and control characters) are reported with a message that lists the offending characters, instead of the raw .NET "Illegal characters in path."
- The message subject follows the source of the name: "The new name …" for a rename, "The file name …" for the original file name
- Directory separators are tolerated (directory parts are stripped), but a name consisting only of a directory is rejected
- Names ending in a dot are rejected; surrounding whitespace is trimmed
- Null, empty, whitespace-only, spaced and non-ASCII names are accepted

### File Adapter Tests

`FileInfoAdapter_GetRelativePath_Tests` covers local web-path resolution:
- Relative path extraction for files and directories inside the web root
- Forward-slash normalization of returned paths
- Directory-boundary safety (e.g., `C:\Web` does not match `C:\Web2`)
- Path-equals-root and trailing-separator handling
- Case-insensitive matching and null/empty input handling

`DomFileAdapter_Tests` covers DOM attachment metadata using local files:
- Size, UTC creation time, and Windows owner parity with local-storage metadata
- Unchanged DOM identity and empty directory/path values
- Missing-file size errors and null metadata input validation

### Secret Store Tests

`SecretKey_Tests`, `DpapiFileSecretStore_Tests` and `Secrets_Tests` cover the storage of secrets (e.g., the SharePoint client secret):
- Secret key path building (`<source>\[<scope>\...]<name>.dat`, e.g. `sharepoint\<tenantId>\<clientId>.dat`) and segment validation (no path traversal)
- Save/read round trip, with the secret never stored as plain text
- Atomic replace of an existing secret without leftover temporary files
- Missing secret handling, `Exists` and `Delete` behavior, and null argument validation
- Isolation between scopes, and a secret file copied to another key failing to decrypt
- Tenant/client ID validation for SharePoint client secrets
- The public `DocHubClient.Secrets` API being write-only (no member returns a secret, no secret store types are exposed)
- The in-memory store used as a test double

`ManageSharePointSecretScript_Tests` adds five cases for saving and deleting secrets,
defaulting an empty action to Save, rejecting invalid input, and matching the
script action constants to the DevPack contract.

### Running Tests

```bash
dotnet test DevPack.Tests
```

## About DataMiner

DataMiner is a transformational platform that provides vendor-independent control and monitoring of devices and services. Out of the box and by design, it addresses key challenges such as security, complexity, multi-cloud, and much more. It has a pronounced open architecture and powerful capabilities enabling users to evolve easily and continuously.

The foundation of DataMiner is its powerful and versatile data acquisition and control layer. With DataMiner, there are no restrictions to what data users can access. Data sources may reside on premises, in the cloud, or in a hybrid setup.

A unique catalog of 7000+ connectors already exists. In addition, you can leverage DataMiner Development Packages to build your own connectors (also known as "protocols" or "drivers").

> **Note**
> See also: [About DataMiner](https://aka.dataminer.services/about-dataminer).

### About Skyline Communications

At Skyline Communications, we deal in world-class solutions that are deployed by leading companies around the globe. Check out [our proven track record](https://aka.dataminer.services/about-skyline) and see how we make our customers' lives easier by empowering them to take their operations to the next level.
