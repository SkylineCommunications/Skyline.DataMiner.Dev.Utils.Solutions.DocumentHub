namespace Skyline.DataMiner.Solutions.DocumentHub.SDM
{
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
    using System;

    internal interface IDocumentHubApiHelper
    {
        IConnection Connection { get; }

        IBulkRepository<DocumentCategory> DocumentCategories { get; }

        IRepository<DomSource> DomSources { get; }

        IRepository<SharePointConfiguration> SharePointConfigurations { get; }
    }
}