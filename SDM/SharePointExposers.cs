namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
	using System;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    internal static class SharePointExposers
	{
		internal static readonly Exposer<Models.Sources.SharePointConfiguration, Guid> Id = new Exposer<Models.Sources.SharePointConfiguration, Guid>((obj) => obj.ID, String.Join(".", nameof(SharePointExposers), nameof(Id)));
	}
}
