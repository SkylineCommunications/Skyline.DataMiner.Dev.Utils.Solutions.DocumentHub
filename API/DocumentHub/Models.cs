using System;
using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
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
            /// Unique identifier of the document category.
            /// </summary>
            public Guid ID { get; set; }

            /// <summary>
            /// Human readable name of the category.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Optional description explaining the purpose of the category.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Base upload path used by the storage handler.
            /// This path is interpreted relative to the storage backend.
            /// </summary>
            public string UploadPath { get; set; }

            /// <summary>
            /// Storage backend used by this category.
            /// </summary>
            public Storagetype StorageType { get; set; }

            /// <summary>
            /// Allowed file extensions for this category.
            /// Typically provided as a comma separated list without dots.
            /// Example. pdf,docx,xlsx
            /// </summary>
            public string Extensions { get; set; }

            /// <summary>
            /// Indicates whether this category is the default category.
            /// </summary>
            public bool IsDefault { get; set; }
        }

        /// <summary>
        /// Configuration required to connect to a SharePoint document library.
        /// </summary>
        public class SharePointConfiguration
        {
            /// <summary>
            /// Unique identifier of the SharePoint configuration.
            /// </summary>
            public Guid ID { get; set; }

            /// <summary>
            /// Azure Active Directory tenant identifier.
            /// </summary>
            public string TenantID { get; set; }

            /// <summary>
            /// Client application identifier registered in Azure AD.
            /// </summary>
            public string ClientID { get; set; }

            /// <summary>
            /// Client secret used for authentication against Microsoft Graph.
            /// </summary>
            public string ClientSecret { get; set; }

            /// <summary>
            /// Base URL of the SharePoint site.
            /// Example. https://contoso.sharepoint.com/sites/MySite
            /// </summary>
            public string SiteURL { get; set; }

            /// <summary>
            /// Name of the document library within the SharePoint site.
            /// Example. Shared Documents
            /// </summary>
            public string DocumentLibraryName { get; set; }
        }
    }
}
