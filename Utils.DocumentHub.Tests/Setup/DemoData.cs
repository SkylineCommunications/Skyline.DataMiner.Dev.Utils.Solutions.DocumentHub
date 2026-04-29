namespace Skyline.DataMiner.Utils.DocumentHub.Tests.Setup
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	public static class DemoData
	{
		public static List<SharePointConfiguration> SharePointConfigurations => new List<SharePointConfiguration>
		{
			new SharePointConfiguration
			{
				TenantID = "tenant-aaa-111",
				ClientID = "client-aaa-111",
				ClientSecret = "secret-aaa-111",
				SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
				DocumentLibraryName = "Shared Documents",
				Status = SharePointConfigurationStatus.Connected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-bbb-222",
				ClientID = "client-bbb-222",
				ClientSecret = "secret-bbb-222",
				SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
				DocumentLibraryName = "Technical Manuals",
				Status = SharePointConfigurationStatus.Connected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-ccc-333",
				ClientID = "client-ccc-333",
				ClientSecret = "secret-ccc-333",
				SiteURL = "https://fabrikam.sharepoint.com/sites/Operations",
				DocumentLibraryName = "Shared Documents",
				Status = SharePointConfigurationStatus.Disconnected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-ddd-444",
				ClientID = "client-aaa-111",
				ClientSecret = "secret-ddd-444",
				SiteURL = "https://fabrikam.sharepoint.com/sites/Sales",
				DocumentLibraryName = "Proposals",
				Status = SharePointConfigurationStatus.Connected,
			},
			new SharePointConfiguration
			{
				TenantID = "tenant-eee-555",
				ClientID = "client-eee-555",
				ClientSecret = "secret-eee-555",
				SiteURL = "https://contoso.sharepoint.com/sites/Engineering",
				DocumentLibraryName = "Design Documents",
				Status = SharePointConfigurationStatus.Disconnected,
			},
		};
	}
}