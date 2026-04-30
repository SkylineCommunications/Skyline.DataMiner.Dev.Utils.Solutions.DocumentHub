# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

## About

NuGet Class Library API to interact with DocumentHub functionality. It provides repositories and helpers for managing document categories, SharePoint configurations, and DOM sources.

## Solution Structure

| Project						| Description																				  |
|-------------------------------|---------------------------------------------------------------------------------------------|
| `Utils.DocumentHub.Common`    | Core library containing models, repositories, exposers, and the `DocumentHubApiHelper`.     |
| `Utils.DocumentHub.Installer` | DOM installer that provisions module settings, section definitions, and DOM definitions.    |
| `Utils.DocumentHub.Tests`     | Unit tests covering CRUD operations and filter queries for all repositories.                |

## Getting Started

Use the `DocumentHubApiHelper` to access repositories:

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

The `Utils.DocumentHub.Tests` project contains integration-style tests using an in-memory DOM mock (`DomSLNetMessageHandler`). Tests are organized per repository:

- **SharePoint** — `SharepointDomRepository_CRUD_Tests`, `SharepointDomRepository_FilterTests_Tests`
- **DomSource** — `DomSourceDomRepository_CRUD_Tests`, `DomSourceDomRepository_FilterTests_Tests`
- **DocumentCategory** — `DocumentCategoryDomRepository_CRUD_Tests`, `DocumentCategoryDomRepository_FilterTests_Tests`

Each test class covers:
- **CRUD**: Create, Update, ReadPaged, DeleteSingle, DeleteBulk
- **Filters**: Equal, Contains, AND, OR, TRUE

## About DataMiner

DataMiner is a transformational platform that provides vendor-independent control and monitoring of devices and services. Out of the box and by design, it addresses key challenges such as security, complexity, multi-cloud, and much more. It has a pronounced open architecture and powerful capabilities enabling users to evolve easily and continuously.

The foundation of DataMiner is its powerful and versatile data acquisition and control layer. With DataMiner, there are no restrictions to what data users can access. Data sources may reside on premises, in the cloud, or in a hybrid setup.

A unique catalog of 7000+ connectors already exists. In addition, you can leverage DataMiner Development Packages to build your own connectors (also known as "protocols" or "drivers").

> **Note**
> See also: [About DataMiner](https://aka.dataminer.services/about-dataminer).

### About Skyline Communications

At Skyline Communications, we deal in world-class solutions that are deployed by leading companies around the globe. Check out [our proven track record](https://aka.dataminer.services/about-skyline) and see how we make our customers' lives easier by empowering them to take their operations to the next level.