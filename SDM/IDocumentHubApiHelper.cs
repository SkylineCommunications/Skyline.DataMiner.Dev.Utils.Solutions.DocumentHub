using Skyline.DataMiner.DocumentHub.SDM.Models;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.SDM;
using Skyline.DataMiner.Utils.DocumentHub.SDM.Models;
using System;

namespace Skyline.DataMiner.DocumentHub.SDM
{
    internal interface IDocumentHubApiHelper : IDisposable
    {
        IConnection Connection { get; }

        IBulkRepository<DocumentCategory> DocumentCategories { get; }

        IRepository<DomSource> DomSources { get; }

        IRepository<SharePointConfiguration> SharePointConfigurations { get; }
    }
}