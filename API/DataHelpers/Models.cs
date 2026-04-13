namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using System;
	using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

	/// <summary>
	/// Public data models used by the DocumentHub API.
	/// </summary>
	public static class Models
	{
		/// <summary>
		/// Contains configuration details for supported storage backends.
		/// </summary>
		public static class Sources
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

			/// <summary>
			/// Configuration required to connect to a DOM Attachment file storage.
			/// </summary>
			public class DomSource : IDocHubSource
			{
				/// <summary>
				/// Gets or sets the unique identifier for the entity.
				/// </summary>
				public Guid ID { get; set; }

				/// <summary>
				/// Gets or sets unique identifier of the DOM Attachment source configuration.
				/// </summary>
				public string Name { get; set; }

				/// <summary>
				/// Gets or sets the name of the DOM module where the attachments are stored.
				/// </summary>
				public string Module { get; set; }

				/// <summary>
				/// Gets or sets the name of the DOM definition where the attachments are stored.
				/// </summary>
				public string NetworkSharePath { get; set; }

				/// <summary>
				/// Gets or sets the name of the DOM field used to store the file attachments.
				/// </summary>
				public string Credential { get; set; }
			}
		}

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

			/// <summary>
			/// Gets or sets a reference to the associated DOM Source with this category.
			/// </summary>
			public Models.Sources.DomSource DOMSource { get; set; }

			/// <summary>
			/// Gets or sets a string value indicating the definition associated with this category.
			/// </summary>
			public string Definition { get; set; }
        }
	}
}
