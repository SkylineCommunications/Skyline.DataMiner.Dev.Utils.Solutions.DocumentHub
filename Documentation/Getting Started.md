# Getting Started

This documentation describes how to use the public API exposed by `Skyline.DataMiner.Solutions.DocumentHub`.

## Installation

Add the NuGet package to your solution:

```shell
dotnet add package Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub
```

Depending on your project type, one of the following additional packages is also required:

- Automation scripts: `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub.Automation`
- Protocols: `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub.Protocol`
- GQI Ad-hoc Data Sources and custom operators: `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub.GQI`

> **Note**
> This library targets `.NET Framework 4.8`.

## Entry Point

The `DocHubClient` class is the main entry point to the DocumentHub API.

It exposes:

- File operations for uploading and reading files across configured storage backends
- Support for multiple storage types (Local, SharePoint, DOM)
- Pagination for large file collections

The `IDocumentHubApiHelper` interface provides access to typed repositories for managing:

- Document Buckets
- DOM Sources
- SharePoint Configurations

### Obtaining an API Instance

To obtain an instance of the `DocHubClient` class, use the `GetDocHubClient` extension method. This extension method is available for automation scripts, connectors, GQI ad-hoc data sources, and custom operators.

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

// Automation scripts
var client = engine.GetDocHubClient();

// Protocols
var client = protocol.GetDocHubClient();

// GQI ad-hoc data sources and custom operators
var client = dms.GetDocHubClient();
```

On other places the instance can also be created starting from an `IConnection` object:

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

IConnection connection;
var client = new DocHubClient(connection);
```

To obtain an instance of the `IDocumentHubApiHelper`, use the `GetDocumentHubApiHelper` extension method:

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

// Automation scripts
var helper = engine.GetDocumentHubApiHelper();

// Protocols
var helper = protocol.GetDocumentHubApiHelper();

// GQI ad-hoc data sources and custom operators
var helper = dms.GetDocumentHubApiHelper();
```

Or starting from an `IConnection` object:

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Extensions;

IConnection connection;
var helper = connection.GetDocumentHubApiHelper();
```

## Core Concepts

### Document Buckets

Document Buckets define how and where files are stored. Each bucket specifies a storage type, an upload path, allowed file extensions, and an optional size limit.

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

var helper = engine.GetDocumentHubApiHelper();

// Read all document buckets
var buckets = helper.DocumentBuckets.ReadAll();

// Create a new document bucket
var bucket = helper.DocumentBuckets.Create(new DocumentBucket
{
    Name = "Reports",
    Description = "Monthly reports",
    UploadPath = "/reports",
    StorageType = StorageType.Local,
    Extensions = "pdf,docx,xlsx",
    SizeLimit = 10485760, // 10 MB
});
```

### Storage Types

The DocumentHub solution supports three storage backends:

- `StorageType.Local` – files stored on the DataMiner Agent's file system.
- `StorageType.SharePoint` – integration with SharePoint Online via Microsoft Graph. Multiple SharePoint configurations are supported, allowing buckets to connect to different sites or document libraries.
- `StorageType.DOM` – files attached directly to DOM instances.

### SharePoint Configurations

SharePoint configurations hold the Azure AD credentials and site information needed to connect to SharePoint Online document libraries. Multiple configurations are supported, allowing different document buckets to connect to different SharePoint sites or libraries.

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

var helper = engine.GetDocumentHubApiHelper();

// Create a configuration for one SharePoint site
var marketingConfig = helper.SharePointConfigurations.Create(new SharePointConfiguration
{
    Name = "Marketing Site",
    TenantID = "your-tenant-id",
    ClientID = "your-client-id",
    SiteURL = "https://contoso.sharepoint.com/sites/Marketing",
    DocumentLibraryName = "Shared Documents",
});

// Create a configuration for another SharePoint site
var engineeringConfig = helper.SharePointConfigurations.Create(new SharePointConfiguration
{
    Name = "Engineering Site",
    TenantID = "your-tenant-id",
    ClientID = "your-client-id",
    SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
    DocumentLibraryName = "Technical Docs",
});
```

Each SharePoint document bucket references a specific configuration via its `SharePointConfiguration` property:

```csharp
var bucket = helper.DocumentBuckets.Create(new DocumentBucket
{
    Name = "Marketing Reports",
    StorageType = StorageType.SharePoint,
    UploadPath = "/reports",
    Extensions = "pdf,docx,xlsx",
    SharePointConfiguration = new SdmObjectReference<SharePointConfiguration>(marketingConfig),
});
```

### DOM Sources

DOM Sources configure DOM-based storage by specifying the module where file attachments are stored.

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

var helper = engine.GetDocumentHubApiHelper();

var source = helper.DomSources.Create(new DomSource
{
    Name = "My DOM Source",
    Module = "my_dom_module",
});
```

### File Operations

The `DocHubClient.Files` property provides access to all file operations.

#### Uploading Files

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

var client = engine.GetDocHubClient();

// Upload a file to a document bucket
client.Files.UploadFile(bucket, @"C:\Documents\report.pdf");

// Upload with a custom file name
client.Files.UploadFile(bucket, @"C:\Documents\report.pdf", name: "monthly_report");

// Upload and link to a specific DOM instance
client.Files.UploadFile(bucket, @"C:\Documents\report.pdf", domInstanceId);

// Upload with a path qualifier (adds a subfolder — not supported for DOM storage)
client.Files.UploadFile(bucket, @"C:\Documents\report.pdf", "2024/January");
```

#### Reading Files

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

var client = engine.GetDocHubClient();

// Read all files from a bucket
var files = client.Files.ReadFiles(bucket);

// Read files with a name filter
var files = client.Files.ReadFiles(bucket, new ReadFilesConfiguration { Filter = "report" });

// Read DOM files for specific DOM instances
var files = client.Files.ReadFiles(domBucket, new ReadFilesConfiguration
{
    DomInstanceIds = new[] { instanceId1, instanceId2 },
});

// Read with pagination
var config = new ReadFilesConfiguration { Context = pageContext };
var files = client.Files.ReadFiles(bucket, config);
```

#### Deleting Files

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

var client = engine.GetDocHubClient();

// Delete a file from a local storage bucket
client.Files.DeleteFile(bucket, "report.pdf");
```

> **Note**: Delete is only supported for `StorageType.Local` buckets.

#### Retrieving File Bytes

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

var client = engine.GetDocHubClient();

// Get bytes from a DOM file
var domFiles = client.Files.ReadFiles(domBucket);
var domFile = domFiles.OfType<IDocHubDomFile>().First();
byte[] bytes = client.Files.GetBytes(domFile);

// Get bytes by module, instance ID, and filename
byte[] bytes = client.Files.GetBytes("my_module", instanceId, "report.pdf");
```

#### Searching Files

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Configurations;

var client = engine.GetDocHubClient();

// Basic KQL search against a SharePoint bucket
var results = client.Files.SearchFiles(spBucket, "filetype:pdf title:\"design doc\"");

// Search with pagination
var config = new SearchFilesConfiguration
{
    Context = new DocHubPageData { PageSize = 50 },
};
var page = client.Files.SearchFiles(spBucket, "quarterly report", config);
```

> **Note**: Search is only supported for `StorageType.SharePoint` buckets. The query is passed straight through to Microsoft Graph as a KQL query.

## Basic Usage

Once you have instances of the `DocHubClient` and `IDocumentHubApiHelper`, you can start using their features.

### Creating Objects

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

// Create a document bucket for local storage
var localBucket = helper.DocumentBuckets.Create(new DocumentBucket
{
    Name = "Local Reports",
    StorageType = StorageType.Local,
    UploadPath = "/reports/local",
    Extensions = "pdf,docx",
});

// Create a document bucket for SharePoint storage
var spBucket = helper.DocumentBuckets.Create(new DocumentBucket
{
    Name = "SharePoint Documents",
    StorageType = StorageType.SharePoint,
    UploadPath = "/shared",
    Extensions = "pdf,docx,xlsx,pptx",
});

// Upload a file to the local bucket
client.Files.UploadFile(localBucket, @"C:\Reports\Q1.pdf");
```

### Reading Objects

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

// Read all document buckets
var buckets = helper.DocumentBuckets.ReadAll();

// Read all SharePoint configurations
var configs = helper.SharePointConfigurations.ReadAll();

// Read all DOM sources
var sources = helper.DomSources.ReadAll();

// Read files from a specific bucket
var files = client.Files.ReadFiles(buckets.First());
```

### Updating Objects

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

// Update a document bucket
bucket.Name = "Updated Reports";
bucket.Extensions = "pdf,docx,xlsx,csv";
helper.DocumentBuckets.Update(bucket);
```

### Deleting Objects

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

// Delete a document bucket
helper.DocumentBuckets.Delete(bucket);

// Delete a file from a local storage bucket
client.Files.DeleteFile(bucket, "old_report.pdf");
```

## Next Steps

- [Quick Reference](Quick%20Reference.md) – Common snippets for file operations, repositories, and filtering
