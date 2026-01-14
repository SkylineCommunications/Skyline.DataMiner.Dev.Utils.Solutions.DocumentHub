namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
	using System;
	using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

	/// <summary>
	/// Public data models used by the DocumentHub API.
	/// </summary>
	public static class Models
    {
        /// <summary>
        /// Represents a document category that defines how and where files are stored.
        /// </summary>
        public class DocumentCategory
        {
			/// <summary>
			/// Gets or sets unique identifier of the document category.
			/// </summary>
			public Guid ID { get; set; }

			/// <summary>
			/// Gets or sets human readable name of the category.
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// Gets or sets optional description explaining the purpose of the category.
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// Gets or sets base upload path used by the storage handler.
			/// This path is interpreted relative to the storage backend.
			/// </summary>
			public string UploadPath { get; set; }

			/// <summary>
			/// Gets or sets storage backend used by this category.
			/// </summary>
			public Storagetype StorageType { get; set; }

			/// <summary>
			/// Gets or sets allowed file extensions for this category.
			/// Typically provided as a comma separated list without dots.
			/// Example. pdf,docx,xlsx
			/// </summary>
			public string Extensions { get; set; }

			/// <summary>
			/// Gets or sets a value indicating whether indicates whether this category is the default category.
			/// </summary>
			public bool IsDefault { get; set; }
        }

        /// <summary>
        /// Configuration required to connect to a SharePoint document library.
        /// </summary>
        public class SharePointConfiguration
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
			/// Example. https://contoso.sharepoint.com/sites/MySite
			/// </summary>
			public string SiteURL { get; set; }

			/// <summary>
			/// Gets or sets name of the document library within the SharePoint site.
			/// Example. Shared Documents
			/// </summary>
			public string DocumentLibraryName { get; set; }
        }
    }
}
