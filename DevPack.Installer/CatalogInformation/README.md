# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

## About

A .NET class library providing API access to the **DocumentHub** solution for DataMiner. It enables automation scripts and connectors to manage document buckets, upload/read files, and interact with storage backends such as local storage and SharePoint.

## Installation

Install via NuGet:

```
dotnet add package Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub
```

Or search for `Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub` in the Visual Studio NuGet Package Manager.

## Getting Started

```csharp

using Skyline.DataMiner.Net;
using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

// Create the client using an active DataMiner connection
var client = new DocHubClient(connection);

// Upload a file to a document bucket
client.Files.UploadFile(bucket, @"C:\Documents\report.pdf");

// Read files from a document bucket
var files = client.Files.ReadFiles(bucket);
```

## Features

| Area                    | Description                                                                                          |
|-----------------------  |------------------------------------------------------------------------------------------------------|
| **File Operations**     | Upload and read files across configured storage backends                                             |
| **Storage Backends**    | Supports local DataMiner storage and SharePoint integration                                          |
| **Document Buckets**    | Organize documents by bucket with configurable storage types                                         |
| **DOM Repositories**    | Typed CRUD and filter operations for SharePoint configurations, DOM sources, and document buckets    |

## Requirements

- DataMiner System with DOM module enabled
- .NET Framework 4.8