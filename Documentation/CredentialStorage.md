# Native credential storage (DocumentHub 2.0.0)

DocumentHub no longer writes or distributes its own DPAPI secret files. Normal
operations have no DPAPI fallback. Upgrade the core DevPack, backend scripts,
Assistant content and App together, then explicitly migrate legacy data before
resuming source operations.

## Assessment and ownership

The previous SharePoint path sent client secrets in Automation parameters,
distributed them to agents, wrote per-tenant/client DPAPI files, and decrypted
those files from Graph consumers. That made partial cluster distribution hard to
detect and conflated independent sources using the same app registration.
DOM attachment credentials already used the Credentials Library, but used
name-based discovery, random identifiers and unnecessarily broad group grants.

| Owner / consumer | Native representation | Persisted reference / access |
|---|---|---|
| SharePoint source; the installed DocumentHub scripts that reach SharePoint | SLNet `PasswordCredential`; Automation `Token` | Deterministic library GUID; one declaration per source in each of those scripts, named like the library credential |
| DOM network attachments; native DOM attachment service | `UserNameAndPasswordCredential` | GUID in the source/module attachment settings; no Automation assignment |
| A connector's own device/API credentials | Password parameters in its protocol | Parameter ID; authorized `SLProtocol.GetParameter(int)` access |

The SharePoint Token contains the **client secret**, not a cached Graph bearer
token. MSAL obtains short-lived bearer tokens inside the consuming script.
SNMP credential types are not appropriate for either DocumentHub source.
No owned connector `protocol.xml` exists in these repositories; the Protocol
DevPack is an adapter, not a connector definition.

## Configuration and dispatch

1. The App generates a source GUID once per new-source form and reuses it on
   retries. Password values are submitted through native IAS password controls,
   never Automation launch parameters. Candidate connection tests are transient.
   The form requires an explicit saved-source confirmation before reporting success.
   These dialogs wait for the user's response (`UIBuilder.RequireResponse`).
   As for every interactive Automation script, SLAutomation writes submitted
   dialog values to its own log **only at debug log level 5**; keep that level
   lower on systems where source credentials are entered.
2. SaveConfiguration and the SharePoint HTTP UDAPI call the shared
   `SharePointSources.Save` lifecycle using `engine.GetUserConnection()`.
   Authenticated direct/gRPC clients use the same lifecycle with their own
   `IConnection`. Do not substitute `Engine.SLNetRaw` or a local-loop connection.
3. SLNet authorizes each mutation on the caller's connection: library
   credential add/edit permissions, credential group membership and Automation
   create/edit permission. No DocumentHub pre-check duplicates that. DocumentHub
   itself requires an unrestricted DataMiner administrator only where SLNet
   cannot enforce the invariant: agent-local legacy migration and file access
   outside the staging folders on behalf of another caller. It sends supported
   `GetLibraryCredentialsRequestMessage` / `UpdateLibraryCredentialMessage`
   messages; it never instantiates the internal `CredentialsLibraryManager`.
4. The credential is upserted by GUID. Its declaration is then added to every
   installed DocumentHub script that reaches SharePoint and read back.
   Only then is non-secret source metadata created/updated in DOM.
5. A script that declares the source's credential reads SharePoint itself.
   Callers that cannot declare one (GQI data sources, connectors, scripts of other
   solutions) send non-secret operations to `SLC-DH-AS-ManageSharePointSecret`
   (Action `Execute`). It reloads caller-accessible source/bucket metadata and
   checks the bucket's source before retrieving the credential. Graph objects,
   pre-authenticated download URLs and secrets do not cross this boundary.

Engine-mediated messages carry the executing user's impersonation wrapper.
gRPC serializes the same SLNet messages through API Gateway and the legacy named
pipe. The synchronous pipe path validates SLNet connection ID/token/sequence
state in `Remoting.VerifyClientToken`; it does not rely on the subscription JWT
path. Both reach the same Facade dispatch and CredentialsLibraryManager checks.
gRPC can rethrow a sole `ExceptionMessage`, while other connection paths can
return it: either shape is treated as failure. A successful native credential
update returns **no messages**, not a success response object.
Default `ExecuteScriptMessage` targeting is local to the connected agent:
the target is `-1`, which `DataMiner.IsLocalTarget(int)` explicitly accepts.
End-to-end staged-file locality still needs live verification.

The manager delegates persistence to the platform's credential CRUD/encrypted
storage implementation. Native XML storage, when selected, lives under
`C:\Skyline DataMiner\Security\Credentials`; other backends are platform-managed.
These files and the platform's encryption/backup key files are **not**
DocumentHub's obsolete DPAPI files and must not be deleted. Platform-managed
key custody may itself use DPAPI; DocumentHub does not manage those keys.

## Identification contract

`SharePointConfiguration` has no separate organization property:
`organizationId` here means its Entra **TenantID**.

```text
organizationId_clientId_sourceId
```

- Every component must parse as a nonempty GUID. Persisted spelling is lowercase
  GUID `D` format, including hyphens; `_` separates components. The name is
  exactly **110 ASCII characters**.
- Tenant/client is not unique: independent source records can use the same app
  registration and independently supplied/rotated secrets. Source ID is therefore
  necessary. A further credential-type suffix is unnecessary: a SharePoint
  source currently owns exactly one client secret. DOM credentials use a
  separate purpose/name namespace.
- Native names are compared case-sensitively by the inspected manager.
  DocumentHub applies stricter ordinal case-insensitive collision checks and
  rejects another GUID with the same canonical name.
- No explicit credential-name length check was found in the inspected manager;
  do not infer a different platform/UI maximum.

Library identity is **not the display name**. For SharePoint, hash ASCII:

```text
DocumentHub/SharePointClientSecret/v1/{sourceId in lowercase D format}
```

Take the first 16 SHA-256 bytes and construct `.NET new Guid(bytes)`, including
.NET's GUID byte ordering when formatting it. This is a versioned derivation
contract, **not RFC UUID-v5**. Only immutable source ID is hashed, so replacing
tenant/client changes the name but preserves the library reference. Such a change
requires a replacement secret. Do not change this algorithm without migration.
The script declaration carries both the name and the GUID, which is the supported
name-to-GUID mapping: a script selects the credential by name among those it
declares; no library-wide lookup by name is used.

New DOM credential IDs use the same construction with purpose `DomNetworkShare`
and name `DocHub_{sourceId:D}` (43 characters). A persisted legacy DOM credential
GUID is retained only after source-name/type ownership validation. Both the dialog
and HTTP handler reject a module already assigned to another source, pass only the
verified GUID to its attachment settings, and require module reinitialization to succeed.

## Permissions, assignment and versions

- Minimum runtime: **DataMiner 10.6.10 / 10.7.0**; package floor
  `10.6.10.0 - 17716`, compiled with Automation/Common SDK `10.6.10.1`.
  Cube's credential-declaration editor requires **10.6.11 / 10.7.0**.
  Programmatic declarations do not require that newer Cube UI.
- DocumentHub stores source and bucket metadata in DOM. On a self-managed DMA,
  Cassandra alone is insufficient: DataMiner also needs an active
  [indexing database](https://docs.dataminer.services/dataminer/Administrator_guide/Databases/Indexing_database/Indexing_Database.html),
  preferably OpenSearch. A missing `ModuleSettings` manager blocks the Storage
  Installer before it can install the DOM definitions.
- Native creation/editing requires `AddLibraryCredential` /
  `EditLibraryCredential`; deletion requires `DeleteLibraryCredential`.
  Native edit/delete also check credential group access.
- Saving a declaration requires Automation create/edit permission and access to
  the referenced matching credential. Missing/inaccessible/type-mismatched
  references can be cleared to `Guid.Empty` despite a successful script save;
  DocumentHub reads every declaration back and fails the save when one was cleared.
- A new credential gets the DataMiner user groups of the user who saves the
  source; an existing credential keeps its groups on every update. Native rules
  then apply: members of those groups who hold the library and Automation
  permissions can see, edit and bind the credential; SLNet refuses everyone
  else, and DocumentHub reports that refusal. The built-in Administrator account
  bypasses the group check and normally belongs to no group, so credentials it
  creates (including migrated ones) have no groups and stay manageable by that
  account only. On its add path SLNet removes a credential of another type that
  has the same GUID without a group check; DocumentHub does not pre-empt that.
  The source model has no organization-to-DataMiner-group ACL
  mapping, so a submitted tenant ID is not treated as authorization. Native null
  groups also mean no nonadministrator group access.
- The source credential is declared on the installed DocumentHub scripts that
  reach SharePoint: `SLC-DH-AS-ManageSharePointSecret`,
  `SLC-DH-AS-CheckConnection`, `SLC-DH-AS-SaveConfiguration`,
  `SLC-DH-IAS-UploadFile`, `SLC-DH-UDAPI-DownloadFile`, `SLC-DH-UDAPI-ManageFiles`
  and `SLC-DH-UDAPI-ManageSharepointSources`. Each gets one `Credential` entry per
  source: the lowest free ID, `Name` equal to the library credential name, the
  library GUID and `Type` `Token`. The name is what selects the right entry when a
  script declares several; a changed tenant or client keeps the ID and GUID and
  updates the name. Other declarations and the script itself are left as they
  are, apart from the author, which SLNet sets to the saving user.
- Saving a script makes SLNet unlink, without failing, every declared credential
  that the saving user cannot access. DocumentHub therefore refuses to save a
  script that declares a credential hidden from the caller: users who manage
  sources must share a group with all source credentials.
- A package upgrade replaces these scripts and with them the declarations. Run
  `SLC-DH-AS-ManageSharePointSecret` with Action `RestoreAssignments` after
  upgrading either package; saving a source also restores its own declarations.
  A script whose declaration is missing still works, through
  `SLC-DH-AS-ManageSharePointSecret`, as long as that script declares it.
- Configuration and migration code receives no credential values. Native
  execution permission and caller DOM read permissions govern consumption.
  Source/bucket metadata write access must remain restricted to trusted source
  administrators.
- Runtime retrieval resolves the **current script's** declaration ID/name,
  library GUID and type; it does not recheck group membership on every token
  retrieval. Save-time access validation and protecting script editing matter.
- The inspected manager caps the whole Credentials Library at **1,000**
  credentials and checks capacity before upsert, potentially blocking updates
  at the cap. Account for other platform users and one credential per source.

Entra application permissions and site/library grants are separate and unchanged
by storing the secret natively. Saving a source verifies the stored declarations,
not retrieval or Graph authorization. HTTP and silent Automation saves check the persisted source;
the App checks it by ID after save confirmation. A negative verdict is Disconnected;
an incomplete check is reported separately. Migration requires a successful check
before deletion.

## Automation retrieval example

For synthetic organization `aaaaaaaa-0000-0000-0000-000000000001`, client
`bbbbbbbb-0000-0000-0000-000000000002`, source
`cccccccc-0000-0000-0000-000000000003`, the derived library GUID is
`9b863ce5-4b70-e0e0-bfa3-4b24c82098c4`. Saving the source adds this entry to
each consuming script, beside any entries of other sources:

```xml
<Credentials>
  <Credential id="1">
    <Name>aaaaaaaa-0000-0000-0000-000000000001_bbbbbbbb-0000-0000-0000-000000000002_cccccccc-0000-0000-0000-000000000003</Name>
    <CredentialId>9b863ce5-4b70-e0e0-bfa3-4b24c82098c4</CredentialId>
    <Type>Token</Type>
  </Credential>
</Credentials>
```

```csharp
using System;
using Skyline.DataMiner.Automation;

public class Script
{
    public void Run(IEngine engine)
    {
        // organizationId_clientId_sourceId of the source being accessed selects its entry.
        var credential = engine.GetCredential(
            "aaaaaaaa-0000-0000-0000-000000000001_bbbbbbbb-0000-0000-0000-000000000002_cccccccc-0000-0000-0000-000000000003");
        if (credential == null)
        {
            engine.ExitFail("The source credential is not assigned to this script.");
            return;
        }

        string clientSecret = credential.GetToken();
        if (String.IsNullOrEmpty(clientSecret))
        {
            engine.ExitFail("The assigned source credential is empty.");
            return;
        }

        // Consume clientSecret only for authentication inside this script.
    }
}
```

`engine.GetCredential(name)` matches the declaration's name case-insensitively
among the entries **this script declares**; `engine.GetCredential(id)` uses the
entry's ID, which differs per script. Neither overload retrieves a credential the
script does not declare. Never log, output, persist or pass the retrieved value
as a script parameter. DocumentHub scripts do not call this themselves:
`engine.GetDocHubClient()` passes the lookup to the DevPack, which builds the
name from the source and falls back to `SLC-DH-AS-ManageSharePointSecret` when
the running script has no entry for it.

## Migration and deployment

1. Inventory agents, sources, old writers and credential capacity. Quiesce old
   source saves, rotations and file consumers across the DMS. Disable obsolete
   secret-distribution scripts, including any installed `SaveClientSecret`
   artifacts. Do not delete their data yet.
2. Install matching 2.0.0 core/Automation/GQI/Protocol dependencies, migration
   script, backend/App and updated Assistant registrations. Ensure long-lived
   Automation/GQI hosts load the new DLLs; an approved process restart may be
   necessary. Refresh the browser after the App update. No live upgrade/restart
   is performed by these repository changes.
   Once both script packages are installed, execute
   `SLC-DH-AS-ManageSharePointSecret` with **Action (ID 2)** =
   `RestoreAssignments` to restore declarations for existing native sources.
   Migration also restores declarations for the sources it verifies.
3. On **each DMA holding legacy files**, execute
   `SLC-DH-AS-ManageSharePointSecret` as an administrator with parameter
   **Action (ID 2)** = `MigrateLocal`. There is no Secret parameter; **Request
   (ID 3)** is ignored for this action and may hold any value.
   `MigrateLocalWithGlobal` explicitly permits `graphsecret.dat`, and only when
   the configured sources have one unambiguous tenant/client group.
4. Migration groups sources by legacy tenant/client file. It provisions a
   native credential and its script declarations for **every** source, verifies
   the assignments and then saved-source Graph/library access, which retrieves
   the credential through `engine.GetCredential`.
   An existing native value is authoritative; migration does not intentionally
   replace it with a stale legacy value.
5. Only after every referencing source verifies does migration remove the exact
   encrypted file it read. It checks for unfinished `.tmp` writes and reparse
   points, snapshots ciphertext, quarantines by atomic move and rechecks bytes.
   A changed file or deletion failure fails explicitly and preserves/restores
   recoverable ciphertext, possibly in the reported directory's `.migrated`
   quarantine. Do not run competing writers during migration.
6. Successful output is `MigratedLocalFiles`. A failed batch may have migrated
   earlier files, but unverified data is retained. Resolve permissions,
   assignments, unavailable agents or Graph access and rerun with the same source
   IDs. Do not resume old writers as a fallback.
7. Inventory orphaned files not referenced by current configuration and agents
   that were offline; the migration cannot infer their ownership and does not
   blanket-delete the legacy directory. Do not declare the DMS migrated until
   those agents/data have been reviewed.

Legacy paths are
`C:\Skyline DataMiner\Security\DocumentHub\sharepoint\{tenant:D}\{client:D}.dat`
and the explicitly selected unscoped `graphsecret.dat` in that root. Decryption
is migration-only. New saves, reads and file operations never read/write them.

## Failure, concurrency and removal semantics

Deterministic GUIDs and credential names prevent duplicate identities on repeated or
concurrent saves **of the same source ID**. A metadata-only edit skips the native
credential write when the name and groups are already canonical; the declarations
are still checked and repaired. A rotation updates only submitted
secret fields, never redacted values. Different sources retain distinct credentials.

The available messages have **no compare-and-swap/create-only token or transaction**
covering credential, script and DOM changes. Saving a script replaces all of its
declarations, so DocumentHub serializes its own script saves within one process;
saves from different processes or agents can still overwrite each other's entry.
Concurrent conflicting rotations are
last-writer-wins or may fail and require retry. Migration therefore requires
quiescence; identical-creation tests are not proof of atomic multi-object saves.
Failure is reported, not converted to successful configuration. A credential or
declaration may already exist after a later-stage failure; retry the same source ID
with the intended value. Never silently roll back another writer's rotation.

Source removal refuses linked buckets, removes only that source's declaration
from the DocumentHub scripts, then removes metadata. It **retains the native
credential and unrelated declarations**, since no transactional complete
reverse-reference query exists.
DOM removal likewise retains credentials/module references. Manual reclamation
requires reviewing all native consumers, not just DocumentHub source records.

Classic Assistant `MessageContent` cannot contain passwords/client secrets.
Secret-collecting source-creation tools were removed; use Settings/native password
dialogs or authenticated HTTP request bodies. HTTP bodies must be valid JSON:
malformed backslashes are rejected, not repaired in ways that change passwords.
Configure platform/proxy request logging not to record secret-bearing bodies.

For the callers it serves, `SLC-DH-AS-ManageSharePointSecret` bounds pages to 200,
replays offsets without accepting Graph
continuation URLs, validates source/bucket and upload scope, and resolves
downloads through bucket enumeration. Search scoping is unchanged from 1.2
(site-scoped query plus the bucket-folder filter). Its local-file operations are
restricted to existing upload staging (`C:\Skyline DataMiner\TempDocuments`) and
new downloads under `C:\Skyline DataMiner\Documents\DocumentHub\Temp`, with
reparse points rejected; other local paths require an administrator. This retains
the platform/filesystem trust assumptions for staged files, not a new per-user
file-ownership service. Large paged reads replay Graph calls and can be costly.
A script that declares the credential and reads SharePoint itself works as in 1.2.

## Connector-owned secrets

An actual connector must use its protocol's native password option, for example:

```xml
<Measurement>
  <Type options="password">string</Type>
</Measurement>
```

`<Type>password</Type>` and ordinary string parameters are not equivalents.
QActions access the runtime value through the supported
`object SLProtocol.GetParameter(int iID)` API. Native element/parameter permissions
and password handling apply; never copy the result into ordinary parameters,
logs or DocumentHub configuration. No connector definition was changed or tested
because none is owned by this workspace.

## Verification and prerequisites

Local checks exercise the core lifecycle/message adapter, permission denial,
creation/repeat/rotation/collisions, organization/source separation, declarations
for several sources, request scope, source removal, migration failures/global-file
consent and real Windows DPAPI decryption of **synthetic** fixtures.
Backend tests cover shared/classic-message guards; frontend tests
mock the native dialog and verify secret-free launch arguments and confirmation.

Run `dotnet test DevPack.Tests -c Release` for the core's offline checks.
Regenerate the Graph client with the pinned tooling and require no generated
diff before release. The runtime size gate validates resolved DLLs against
20,971,520 bytes and rejects the full Graph SDK; verify the installed package's
dependency closure as well as the NuGet artifacts.

The Storage Installer uses Skyline SDK 2.6.2. Its script-reference harvesting
resolves the DevPack's Debug output even when the outer build is Release.
Build the solution in Debug before building the Release installer, and verify
the harvested DLL against that Debug build rather than assuming a Release DLL
was selected. NuGet consumers should use the Release packages.

Coordinated backend package creation may require the SDK's configured
`skyline:sdk:dataminertoken` secret or `skyline__sdk__dataminertoken` environment
variable for Catalog dependencies; do not put that value in source or chat.
Validate consumers against the exact local 2.0.0 packages and source commit
before any publication. The API itself has no minimum web version; a consuming
App or headless IAS can have a separate, stricter web prerequisite. A successful
build or package manifest does not prove live runtime compatibility.

These are **not live DataMiner/gRPC/Graph integration tests**. Before production,
use an authenticated 10.6.10+ test DMA, administrator and denied-user identities,
an Automation license, the matching installed DLLs, a test Entra app/site/library
and per-agent legacy fixtures. Verify real script save/readback/retrieval, engine
and gRPC parity, native encrypted persistence/backup recovery, concurrency,
source permissions, staged-file ownership/locality,
upload/download and offline-agent recovery. No live endpoint or Graph test
identity was used for this implementation.

## Verified references

Paths below are in the supplied source checkout, not additional client APIs:

| Evidence | Source |
|---|---|
| Credential DTOs, dirty-property updates, supported messages | `C:\devcore3\server\SLNet\SLNetTypes\CredentialsLibrary\Credentials\`; `CredentialsLibrary\Messages\UpdateLibraryCredentialMessage.cs` |
| CRUD, permission/group filtering/redaction, native persistence and capacity | `C:\devcore3\server\SLNet\SLNet\CredentialsLibrary\CredentialsLibraryManager.cs`, especially `GetCredentialsByTypeInternal`, `ApplyCredentialSecurityFiltering`, `CreateOrUpdateCredential` |
| Shared dispatch | `C:\devcore3\server\SLNet\SLNet\Logic\Facade.cs:3621-3630` |
| User impersonation and declaration-local retrieval | `C:\devcore3\server\SLManagedAutomation\SLManagedAutomation\Engine.cpp:213-280,1370-1410` |
| Script-save validation and cleared references | `C:\devcore3\server\SLNet\SLNet\Automation\AutomationScriptManager.cs:1217-1270`; `Logic\MessageHandler.cs:876-951` |
| Runtime declaration/GUID/type resolution | `C:\devcore3\server\SLAutomation\ActionHandler.cpp:659-683,879-1039` |
| gRPC transport and token validation | `SLNetTypes\Connection\GRPCConnection\GRPCConnection.cs:997+`; `SLNet\Transport\LegacyPipeConnection.cs:75-105`; `SLNet\Transport\Remoting.cs:135-154` under the SLNet root |
| Gateway forwarding | `C:\devcore3\apigateway\APIGateway\APIGateway\Legacy\Messaging\LegacyProtoMessageHandler.cs:40-80` |
| Default local targeting | `C:\devcore3\server\SLNet\SLNet\Logic\Dataminer.cs:4987-4989`; `Facade.SetHostingAgentIdForRouting` |
| Native password definition/loading | `C:\devcore3\server\SLProtocol\Parameter.cpp:1996-2003,3480-3525`; `NotifyProtocolHelper.cpp:1690-1750` |
| Native storage architecture | `C:\Git\internal-docs\DevDocs\SLNet_Subscriptions_And_Caching\Encryption_SLNetIntegration.md`; `DevDocs\StorageAPI\05_EncryptionAtRest.md`; `DevDocs\Encryption_At_Rest_For_Credentials\Encryption_At_Rest_For_Credentials.md` |

Public documentation:

- [Using credentials in an Automation script](https://docs.dataminer.services/develop/devguide/Automation/Howto/Using_credentials_in_an_automation_script.html)
- [Engine.GetCredential](https://docs.dataminer.services/develop/api/types/Skyline.DataMiner.Automation.Engine.GetCredential.html)
- [Protocol measurement options](https://docs.dataminer.services/develop/schemadoc/Protocol/Protocol.Params.Param.Measurement.Type-options.html)
- [SLProtocol.GetParameter](https://docs.dataminer.services/develop/api/types/Skyline.DataMiner.Scripting.SLProtocol.GetParameter.html)
