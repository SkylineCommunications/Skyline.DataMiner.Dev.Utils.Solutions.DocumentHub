# Quick Reference

Common code snippets for the `Skyline.DataMiner.Solutions.DocumentHub` API.

## Obtaining API Instances

### DocHubClient

```csharp
// Automation scripts
using Skyline.DataMiner.Solutions.DocumentHub.Automation;
var client = engine.GetDocHubClient();

// Protocols
using Skyline.DataMiner.Solutions.DocumentHub.Protocol;
var client = protocol.GetDocHubClient();

// GQI
using Skyline.DataMiner.Solutions.DocumentHub.GQI;
var client = dms.GetDocHubClient();

// From IConnection
using Skyline.DataMiner.Solutions.DocumentHub.API.Extensions;
var client = connection.GetDocHubClient();
```

### IDocumentHubApiHelper

```csharp
// Automation scripts
using Skyline.DataMiner.Solutions.DocumentHub.Automation;
var helper = engine.GetDocumentHubApiHelper();

// Protocols
using Skyline.DataMiner.Solutions.DocumentHub.Protocol;
var helper = protocol.GetDocumentHubApiHelper();

// GQI
using Skyline.DataMiner.Solutions.DocumentHub.GQI;
var helper = dms.GetDocumentHubApiHelper();

// From IConnection
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Extensions;
var helper = connection.GetDocumentHubApiHelper();
```

## Repositories

### Document Buckets

```csharp
// Create
var bucket = helper.DocumentBuckets.Create(new DocumentBucket
{
    Name = "Invoices",
    StorageType = StorageType.Local,
    UploadPath = "/invoices",
    Extensions = "pdf",
    SizeLimit = 5242880,
});

// Read all
var buckets = helper.DocumentBuckets.ReadAll();

// Read with filter
var filtered = helper.DocumentBuckets.Read(
    DocumentBucketExposers.StorageType.Equal(StorageType.SharePoint));

// Update
bucket.Name = "Updated Invoices";
helper.DocumentBuckets.Update(bucket);

// Delete
helper.DocumentBuckets.Delete(bucket);

// Bulk create
var created = helper.DocumentBuckets.Create(new[] { bucket1, bucket2 });

// Bulk update
var updated = helper.DocumentBuckets.Update(new[] { bucket1, bucket2 });

// Create or update
var result = helper.DocumentBuckets.CreateOrUpdate(new[] { bucket1, bucket2 });
```

### SharePoint Configurations

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

var sources = new SharePointSources(engine.GetUserConnection());
// IDs are supplied by the administrator's form and reused on retries.
var config1 = new SharePointConfiguration
{
    Identifier = sourceId,
    Name = "Marketing Site",
    TenantID = organizationId,
    ClientID = clientId,
    SiteURL = "https://contoso.sharepoint.com/sites/Marketing",
    DocumentLibraryName = "Shared Documents",
};
sources.Save(config1, clientSecretFromNativePasswordControl);

// Read all
var configs = helper.SharePointConfigurations.ReadAll();

// Update
config1.DocumentLibraryName = "Archive";
sources.Save(config1, null); // Preserve the existing native secret.

// Remove a different, unlinked source; retain its native credential.
sources.Delete(unlinkedSourceId);

// Link a SharePoint bucket to a specific configuration
var bucket = helper.DocumentBuckets.Create(new DocumentBucket
{
    Name = "Marketing Docs",
    StorageType = StorageType.SharePoint,
    UploadPath = "/docs",
    Extensions = "pdf,docx",
    SharePointConfiguration = new SdmObjectReference<SharePointConfiguration>(config1),
});
```

### SharePoint Client Secrets

Stored in a source-scoped native Credentials Library entry, never ordinary DOM
data or secret files. The DocumentHub scripts that reach SharePoint declare it
under its library name and use `engine.GetCredential(name).GetToken()`.
Management requires a DataMiner administrator
and 10.6.10 / 10.7.0. See [Credential storage](CredentialStorage.md) for verified
references, exact assignments and explicit legacy migration.

```csharp
var client = engine.GetDocHubClient();

bool valid = client.Secrets.IsSharePointConnected(config1, clientSecretFromNativePasswordControl); // transient candidate
client.Secrets.SaveSharePointClientSecret(config1, clientSecretFromNativePasswordControl);
bool exists = client.Secrets.HasSharePointClientSecret(config1);
bool connected = client.Secrets.IsSharePointConnected(config1); // test the saved secret
```

### DOM Sources

```csharp
// Create
var source = helper.DomSources.Create(new DomSource
{
    Name = "My Source",
    Module = "my_module",
});

// Read all
var sources = helper.DomSources.ReadAll();

// Update
source.Name = "Renamed Source";
helper.DomSources.Update(source);

// Delete
helper.DomSources.Delete(source);
```

## File Operations

### Upload

```csharp
// Basic upload
client.Files.UploadFile(bucket, @"C:\file.pdf");

// Upload with custom name
client.Files.UploadFile(bucket, @"C:\file.pdf", name: "custom_name");

// Upload linked to a DOM instance
client.Files.UploadFile(bucket, @"C:\file.pdf", domInstanceId);

// Upload linked to a DOM instance with custom name
client.Files.UploadFile(bucket, @"C:\file.pdf", domInstanceId, name: "custom_name");

// Upload with path qualifier (subfolder — not supported for DOM storage)
client.Files.UploadFile(bucket, @"C:\file.pdf", "2024/Q1");
```

### Read

```csharp
// Read from a bucket
var files = client.Files.ReadFiles(bucket);

// Read from a bucket with filter
var files = client.Files.ReadFiles(bucket, new ReadFilesConfiguration { Filter = "invoice" });

// Read DOM files for specific instances
var files = client.Files.ReadFiles(domBucket, new ReadFilesConfiguration
{
    DomInstanceIds = new[] { instanceId1, instanceId2 },
});

// Read DOM files for specific instances with filter
var files = client.Files.ReadFiles(domBucket, new ReadFilesConfiguration
{
    DomInstanceIds = new[] { instanceId },
    Filter = "invoice",
});
```

### Pagination

```csharp
// Read with pagination (reuse the same context instance between calls)
var pageContext = new DocHubPageData { PageSize = 50 };
var config = new ReadFilesConfiguration { Context = pageContext };

do
{
    var files = client.Files.ReadFiles(bucket, config);
    // Process files...
}
while (pageContext.HasNextPage());
```

### Get File Bytes (DOM)

```
// From an IDocHubDomFile instance
var domFiles = client.Files.ReadFiles(StorageType.DOM);
var domFile = domFiles.OfType<IDocHubDomFile>().First();
byte[] bytes = client.Files.GetBytes(domFile);

// From module, instance ID, and filename
byte[] bytes = client.Files.GetBytes("my_module", instanceId, "report.pdf");
```

### Search

```csharp
// Basic KQL search (SharePoint buckets only)
var results = client.Files.SearchFiles(spBucket, "filetype:pdf title:\"design doc\"");

// Search with pagination
var pageContext = new DocHubPageData { PageSize = 50 };
var config = new SearchFilesConfiguration { Context = pageContext };

do
{
    var results = client.Files.SearchFiles(spBucket, "invoice", config);
    // Process results...
}
while (pageContext.HasNextPage());
```

## IDocHubFile Properties

```csharp
IDocHubFile file = files.First();

string filePath   = file.GetFilePath();    // Full absolute path (local) or web URL (SharePoint); empty for DOM
string webPath    = file.GetWebPath();     // Relative web path (local) or web URL (SharePoint); empty for DOM
string fileName   = file.GetFile();        // File name with extension (e.g. "report.pdf")
string name       = file.GetName();        // File name without extension (e.g. "report")
string extension  = file.GetExtension();   // Extension without dot (e.g. "pdf")
string size       = file.GetSize();        // Human-readable size (e.g. "1.5 MB")
string directory  = file.GetDirectory();   // Relative directory path from storage root; empty for DOM
DateTime created  = file.GetCreatedAt();   // Creation timestamp (UTC)
string createdBy  = file.GetCreatedBy();   // Creator name or identifier
string module     = file.GetModule();      // DOM module name (DOM files only; empty otherwise)
string instance   = file.GetInstanceName();// DOM instance name (DOM files only; empty otherwise)
Guid instanceId   = file.GetInstanceId();  // DOM instance ID (DOM files only; Guid.Empty otherwise)
```

## Filtering with Exposers

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;

// Filter by name
var buckets = helper.DocumentBuckets.Read(
    DocumentBucketExposers.Name.Equal("Reports"));

// Filter by storage type
var buckets = helper.DocumentBuckets.Read(
    DocumentBucketExposers.StorageType.Equal(StorageType.SharePoint));

// Filter by default flag
var buckets = helper.DocumentBuckets.Read(
    DocumentBucketExposers.IsDefault.Equal(true));

// Combine filters with OR
var buckets = helper.DocumentBuckets.Read(
    new ORFilterElement<DocumentBucket>(
        DocumentBucketExposers.StorageType.Equal(StorageType.Local),
        DocumentBucketExposers.StorageType.Equal(StorageType.SharePoint)));
```

## Storage Types

| Value                    | Description                                          |
| ------------------------ | ---------------------------------------------------- |
| `StorageType.Local`      | Files stored on the DataMiner Agent's file system    |
| `StorageType.SharePoint` | Files stored in a SharePoint Online document library |
| `StorageType.DOM`        | Files attached to DOM instances                      |
