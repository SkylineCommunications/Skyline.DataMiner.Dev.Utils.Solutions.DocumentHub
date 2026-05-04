# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

## About

A .NET class library providing API access to the **DocumentHub** solution for DataMiner. It enables automation scripts and connectors to manage document categories, upload/read files, and interact with storage backends such as local storage and SharePoint.

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

// Upload a file to a document category
client.Files.UploadFile(category, @"C:\Documents\report.pdf");

// Read files from a document category
var files = client.Files.ReadFiles(category);
```

## Features

| Area                    | Description                                                                                          |
|-----------------------  |------------------------------------------------------------------------------------------------------|
| **File Operations**     | Upload and read files across configured storage backends                                             |
| **Storage Backends**    | Supports local DataMiner storage and SharePoint integration                                          |
| **Document Categories** | Organize documents by category with configurable storage types                                       |
| **DOM Repositories**    | Typed CRUD and filter operations for SharePoint configurations, DOM sources, and document categories |

## Requirements

- DataMiner System with DOM module enabled
- .NET Framework 4.8