# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

## About

NuGet Class Library API to interact with DocumentHub functionality. It provides repositories and helpers for managing document categories, SharePoint configurations, and DOM sources, as well as a high-level API for file upload and read operations across multiple storage backends.

## Solution Structure

| Project | Description |
|---------|-------------|
| `DevPack` | Core library containing models, repositories, exposers, `DocumentHubApiHelper`, and the `DocHubClient` API. |
| `DevPack.Installer` | DOM installer that provisions module settings, section definitions, and DOM definitions. |
| `DevPack.Tests` | Unit tests covering CRUD operations, filter queries, and API validation for all components. |

## Getting Started

### Using the DocHubClient API

The `DocHubClient` provides a simplified interface for file operations:

```csharp
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

var client = new DocHubClient(connection);

// Upload a file to a document category
client.Files.UploadFile(category, @"C:\Documents\report.pdf");

// Upload with custom name
client.Files.UploadFile(category, filePath, name: "CustomName");

// Upload to DOM instance
client.Files.UploadFile(category, filePath, domInstanceId);

// Read files from a category
var files = client.Files.ReadFiles(category);

// Read files with filter
var filtered = client.Files.ReadFiles(category, filter: "invoice");
```

### Using the DocumentHubApiHelper

For direct repository access and DOM operations:

```csharp
var helper = new DocumentHubApiHelper(connection);

// Create a SharePoint configuration
helper.SharePointConfigurations.Create(new SharePointConfiguration
{
    TenantID = "your-tenant-id",
    ClientID = "your-client-id",
    ClientSecret = "your-client-secret",
    SiteURL = "https://contoso.sharepoint.com/sites/MySite",
    DocumentLibraryName = "Shared Documents",
});

// Read with filters
var results = helper.DocumentCategories.Read(
    DocumentCategoryExposers.Name.Equal("Technical Documentation"));
```

## Unit Tests

The `DevPack.Tests` project contains unit tests using an in-memory DOM mock (`DomSLNetMessageHandler`).

### Test Coverage

| Area                       | Test Classes                                                                                  | Tests |
|----------------------------|-----------------------------------------------------------------------------------------------|-------|
| **API - DocHubClient**     | `DocHubClient_Tests`                                                                          | 3     |
| **API - Files Upload**     | `Files_UploadFile_Tests`                                                                      | 8     |
| **API - Files Read**       | `Files_ReadFiles_Tests`                                                                       | 5     |
| **SDM - SharePoint**       | `SharepointDomRepository_CRUD_Tests`, `SharepointDomRepository_FilterTests_Tests`             | 13    |
| **SDM - DomSource**        | `DomSourceDomRepository_CRUD_Tests`, `DomSourceDomRepository_FilterTests_Tests`               | 12    |
| **SDM - DocumentCategory** | `DocumentCategoryDomRepository_CRUD_Tests`, `DocumentCategoryDomRepository_FilterTests_Tests` | 12    |

**Total: 53 tests**

### SDM Repository Tests

Each repository test class covers:
- **CRUD**: Create, Update, ReadPaged, DeleteSingle, DeleteBulk
- **Filters**: Equal, Contains, AND, OR, TRUE

### API Tests

API tests validate parameter handling:
- Null category, filePath, and domInstanceId validation
- Storage type restrictions (e.g., DOM storage with qualifier overload)
- DomSource module validation

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