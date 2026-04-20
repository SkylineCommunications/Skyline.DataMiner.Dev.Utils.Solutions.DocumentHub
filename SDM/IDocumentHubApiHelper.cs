namespace Skyline.DataMiner.Solutions.DocumentHub.SDM
{
    using System;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

    internal interface IDocumentHubApiHelper : IDisposable
    {
        IConnection Connection { get; }

        IBulkRepository<DocumentCategory> DocumentCategories { get; }

        IRepository<DomSource> DomSources { get; }

        IRepository<SharePointConfiguration> SharePointConfigurations { get; }
    }
}