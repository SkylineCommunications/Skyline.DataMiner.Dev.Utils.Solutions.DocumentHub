namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
	using System;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    public static class SharePointExposers
	{
		public static readonly Exposer<Models.Sources.SharePointConfiguration, Guid> Id = new Exposer<Models.Sources.SharePointConfiguration, Guid>((obj) => obj.ID, String.Join(".", nameof(SharePointExposers), nameof(Id)));
	}
}
