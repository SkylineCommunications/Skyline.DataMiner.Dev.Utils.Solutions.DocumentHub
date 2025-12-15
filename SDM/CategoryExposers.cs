using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
using System;

namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
    internal static class CategoryExposers
    {
        internal static readonly Exposer<Models.DocumentCategory, Guid> Id = new Exposer<Models.DocumentCategory, Guid>((obj) => obj.ID, String.Join(".", nameof(CategoryExposers), nameof(Id)));
    }

    internal static class SharePointExposers
    {
        internal static readonly Exposer<Models.SharePointConfiguration, Guid> Id = new Exposer<Models.SharePointConfiguration, Guid>((obj) => obj.ID, String.Join(".", nameof(SharePointExposers), nameof(Id)));
    }
}
