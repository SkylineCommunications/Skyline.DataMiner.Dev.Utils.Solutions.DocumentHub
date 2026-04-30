namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Models
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Configuration required to connect to a SharePoint document library.
	/// </summary>
	// [GenerateExposers]
	// [SdmDomStorage("(slc)documenthub")]
	public class SharePointConfiguration : SdmObject<SharePointConfiguration>
	{
		/// <summary>
		/// Gets or sets azure Active Directory tenant identifier.
		/// </summary>
		public string TenantID { get; set; }

		/// <summary>
		/// Gets or sets client application identifier registered in Azure AD.
		/// </summary>
		public string ClientID { get; set; }

		/// <summary>
		/// Gets or sets client secret used for authentication against Microsoft Graph.
		/// </summary>
		public string ClientSecret { get; set; }

		/// <summary>
		/// Gets or sets base URL of the SharePoint site.
		/// Example. https://contoso.sharepoint.com/sites/MySite.
		/// </summary>
		public string SiteURL { get; set; }

		/// <summary>
		/// Gets or sets name of the document library within the SharePoint site.
		/// Example. Shared Documents.
		/// </summary>
		public string DocumentLibraryName { get; set; }

		/// <summary>
		/// Gets or sets the current status of the SharePoint configuration.
		/// </summary>
		public SharePointConfigurationStatus Status { get; set; }
	}
}