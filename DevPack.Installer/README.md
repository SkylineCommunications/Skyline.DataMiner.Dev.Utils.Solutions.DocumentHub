# Skyline.DataMiner.Dev.Utils.Solutions.DocumentHub

## About

NuGet Class Library API to interact with DocumentHub functionality.

The 2.0.0 installer requires DataMiner `10.6.10.0 - 17716` and deploys the native
credential-capable DevPack and `SLC-DH-AS-ManageSharePointSecret` migration script.
Install matching backend/App content, quiesce legacy writers and invoke
`Action` (parameter ID 2) = `MigrateLocal` on each agent. Global legacy files
require explicit `MigrateLocalWithGlobal` consent and an unambiguous app
registration. Nothing migrates automatically during installation; retain
unverified legacy data. See `Documentation/CredentialStorage.md` in the repository
for permissions, verification, recovery and deployment ordering.

The focused Kiota client replaces the full Microsoft.Graph SDK. Upgrade all four
DocumentHub NuGet libraries to 2.0.0 and rebuild consumers before installation.
Restore source credential assignments after the matching script packages are
installed; package installation does not migrate secrets or verify Graph access.

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
