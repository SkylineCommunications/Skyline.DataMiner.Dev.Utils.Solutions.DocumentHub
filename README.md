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

## Documentation

| Document                                              | Description                                                                  |
| ----------------------------------------------------- | ---------------------------------------------------------------------------- |
| [Getting Started](Documentation/Getting%20Started.md) | Installation, prerequisites, and basic usage                                 |
| [Quick Reference](Documentation/Quick%20Reference.md) | Common code snippets for file operations, repositories, and storage backends |

External resources:

- [DataMiner Docs](https://docs.dataminer.services/) – Official DataMiner documentation

## Unit Tests

The `DevPack.Tests` project contains unit tests using an in-memory DOM mock (`DomSLNetMessageHandler`).

### Test Coverage

| Area                       | Test Classes                                                                                  | Tests |
|----------------------------|-----------------------------------------------------------------------------------------------|-------|
| **API - DocHubClient**     | `DocHubClient_Tests`                                                                          | 3     |
| **API - Files Upload**     | `Files_UploadFile_Tests`                                                                      | 8     |
| **API - Files Read**       | `Files_ReadFiles_Tests`                                                                       | 5     |
| **API - Files Delete**     | `Files_DeleteFile_Tests`                                                                      | 6     |
| **API - Files Search**     | `Files_SearchFiles_Tests`                                                                     | 5     |
| **API - File Validator**   | `FileValidator_ValidateAndSanitizeFile_Tests`                                                 | 19    |
| **API - File Adapters**    | `FileInfoAdapter_GetRelativePath_Tests`                                                       | 11    |
| **SDM - SharePoint**       | `SharepointDomRepository_CRUD_Tests`, `SharepointDomRepository_FilterTests_Tests`             | 13    |
| **SDM - DomSource**        | `DomSourceDomRepository_CRUD_Tests`, `DomSourceDomRepository_FilterTests_Tests`               | 13    |
| **SDM - DocumentBucket**   | `DocumentBucketDomRepository_CRUD_Tests`, `DocumentBucketDomRepository_FilterTests_Tests`     | 26    |

**Total: 109 tests** (includes 3 parameterized test methods)

### SDM Repository Tests

Each repository test class covers:
- **CRUD**: Create, Update, CreateOrUpdate, ReadPaged, DeleteSingle, DeleteBulk, Count
- **Filters**: Equal, Contains, AND, OR, TRUE
- **Middleware**: Path sanitization (DocumentBucket)

### API Tests

API tests validate parameter handling:
- Null category, filePath, and domInstanceId validation
- Storage type restrictions (e.g., DOM storage with qualifier overload)
- DomSource module validation

### File Adapter Tests

`FileInfoAdapter_GetRelativePath_Tests` covers local web-path resolution:
- Relative path extraction for files and directories inside the web root
- Forward-slash normalization of returned paths
- Directory-boundary safety (e.g., `C:\Web` does not match `C:\Web2`)
- Path-equals-root and trailing-separator handling
- Case-insensitive matching and null/empty input handling

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
