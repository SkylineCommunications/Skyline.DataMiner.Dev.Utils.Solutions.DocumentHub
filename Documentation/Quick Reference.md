# Quick Reference

Common code snippets for the `Skyline.DataMiner.Solutions.DocumentHub` API.

## Obtaining API Instances

### DocHubClient

```
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

```
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

```
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

```
// Create
var config = helper.SharePointConfigurations.Create(new SharePointConfiguration
{
    TenantID = "tenant-id",
    ClientID = "client-id",
    ClientSecret = "client-secret",
    SiteURL = "https://contoso.sharepoint.com/sites/MySite",
    DocumentLibraryName = "Shared Documents",
});

// Read all
var configs = helper.SharePointConfigurations.ReadAll();

// Update
config.DocumentLibraryName = "Archive";
helper.SharePointConfigurations.Update(config);

// Delete
helper.SharePointConfigurations.Delete(config);
```

### DOM Sources

```
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

```
// Basic upload
client.Files.UploadFile(bucket, @"C:\file.pdf");

// Upload with custom name
client.Files.UploadFile(bucket, @"C:\file.pdf", name: "custom_name");

// Upload linked to a DOM instance
client.Files.UploadFile(bucket, @"C:\file.pdf", domInstanceId);

// Upload linked to a DOM instance with custom name
client.Files.UploadFile(bucket, @"C:\file.pdf", domInstanceId, name: "custom_name");

// Upload with path qualifier (subfolder)
client.Files.UploadFile(bucket, @"C:\file.pdf", uploadPathQualifier: "2024/Q1");
```

### Read

```
// Read from a bucket
var files = client.Files.ReadFiles(bucket);

// Read from a bucket with filter
var files = client.Files.ReadFiles(bucket, filter: "invoice");

// Read by storage type
var files = client.Files.ReadFiles(StorageType.Local);
var files = client.Files.ReadFiles(StorageType.SharePoint);
var files = client.Files.ReadFiles(StorageType.DOM);

// Read by storage type with filter
var files = client.Files.ReadFiles(StorageType.DOM, filter: "report");

// Read DOM files for specific instances
var files = client.Files.ReadFiles(domSource, new[] { instanceId1, instanceId2 });

// Read DOM files for specific instances with filter
var files = client.Files.ReadFiles(domSource, new[] { instanceId }, filter: "invoice");
```

### Pagination

```
// Read with pagination (context is created automatically on first call)
DocHubPageData pageContext = null;
do
{
    var files = client.Files.ReadFiles(bucket, context: pageContext);
    // Process files...
}
while (pageContext != null && pageContext.HasNextPage());

// Read by storage type with pagination
DocHubPageData pageContext = null;
var files = client.Files.ReadFiles(StorageType.Local, context: pageContext);
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

## IDocHubFile Properties

```
IDocHubFile file = files.First();

string path      = file.GetFilePath();    // Full path in the storage backend
string name      = file.GetName();        // File name with extension
string extension = file.GetExtension();   // e.g. ".pdf"
string size      = file.GetSize();        // File size as string
string type      = file.GetType();        // Item type, e.g. "File"
string directory = file.GetDirectory();   // Parent directory
string reference = file.GetFile();        // Full file reference
DateTime created = file.GetCreatedAt();   // Creation timestamp (UTC)
string createdBy = file.GetCreatedBy();   // Creator name or identifier
string module    = file.GetModule();      // DOM module (DOM files only)
string instance  = file.GetInstanceName();// DOM instance name (DOM files only)
Guid instanceId  = file.GetInstanceId();  // DOM instance ID (DOM files only)
```

## Filtering with Exposers

```
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
