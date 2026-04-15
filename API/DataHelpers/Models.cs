namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
    using System;

    /// <summary>
    /// Public data models used by the DocumentHub API.
    /// </summary>
    public static partial class Models
	{
		/// <summary>
		/// Contains configuration details for supported storage backends.
		/// </summary>
		public static partial class Sources
		{
			/// <summary>
			/// Marker interface for different document storage source configurations.
			/// </summary>
			public interface IDocHubSource
			{
			}

			/// <summary>
			/// Configuration required to connect to a SharePoint document library.
			/// </summary>
			public class SharePointConfiguration : IDocHubSource
			{
				/// <summary>
				/// Gets or sets unique identifier of the SharePoint configuration.
				/// </summary>
				public Guid ID { get; set; }

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
			}
		}
	}
}
