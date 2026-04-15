using Skyline.DataMiner.DocumentHub.SDM.Models;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.SDM;
using System;

namespace Skyline.DataMiner.DocumentHub.SDM
{
    internal interface IDocumentHubApiHelper : IDisposable
    {
        IConnection Connection { get; }

        IBulkRepository<DocumentCategory> DocumentCategories { get; }

        IBulkRepository<DomSource> DomSources { get; }
    }
}