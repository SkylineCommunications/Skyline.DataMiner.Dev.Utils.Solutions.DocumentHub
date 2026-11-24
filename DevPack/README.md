# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

## About

NuGet Class Library API to interact with DocumentHub functionality.

## Version 2 compatibility

The full Microsoft.Graph SDK has been replaced with a focused internal Kiota client.
Existing SharePoint file operations, recursive paging, KQL search, and metadata
batching are retained. Authentication still uses Microsoft.Identity.Client (MSAL)
and the existing credential/token providers without changes.

Upgrade the core, Automation, Protocol, and GQI packages together and rebuild consumers.
The `PageRemainderBuffer` properties on `SharePointPageData` and
`SharePointSearchPageData` are now internal. Use the public paging-context APIs
instead of manipulating Graph model buffers. Other high-level file APIs, local
storage, and DOM storage are unchanged.

Repository build/pack targets check the shipping DLLs and resolved runtime
dependencies against a **20,971,520-byte (20 MiB)** per-DLL limit. They also reject
the full Graph SDK and its Azure authentication dependency chain. MSAL is retained
and is subject to the same size limit.

## Graph client maintenance

The source repository includes a small OpenAPI definition in
`DevPack\API\StorageHandlers\Graph\documenthub.openapi.yaml` and checked-in generated
sources. From the repository root, regenerate with:

```powershell
.\DevPack\API\StorageHandlers\Graph\Generate-Client.ps1
```

This restores the pinned Kiota tool and replaces only the generated directory.
Ordinary builds use the checked-in sources. Keep handwritten path/batch helpers
outside that directory and update the definition before adding Graph operations.
ID-based request builders are reused for Graph's documented site-by-path and
`root:/path:` aliases, with path segments escaped individually.

### About DataMiner

DataMiner is a transformational platform that provides vendor-independent control and monitoring of devices and services. Out of the box and by design, it addresses key challenges such as security, complexity, multi-cloud, and much more. It has a pronounced open architecture and powerful capabilities enabling users to evolve easily and continuously.

The foundation of DataMiner is its powerful and versatile data acquisition and control layer. With DataMiner, there are no restrictions to what data users can access. Data sources may reside on premises, in the cloud, or in a hybrid setup.

A unique catalog of 7000+ connectors already exists. In addition, you can leverage DataMiner Development Packages to build your own connectors (also known as "protocols" or "drivers").

> **Note**
> See also: [About DataMiner](https://aka.dataminer.services/about-dataminer).

### About Skyline Communications

At Skyline Communications, we deal in world-class solutions that are deployed by leading companies around the globe. Check out [our proven track record](https://aka.dataminer.services/about-skyline) and see how we make our customers' lives easier by empowering them to take their operations to the next level.

<!-- Uncomment below and add more info to provide more information about how to use this package. -->
<!-- ## Getting Started -->
