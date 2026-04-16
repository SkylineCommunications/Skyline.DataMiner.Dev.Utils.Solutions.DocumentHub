namespace Skyline.DataMiner.DocumentHub.SDM.Models
{
    using System;
    using Skyline.DataMiner.SDM;
    using static Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers.SlcDocumenthubIds.Enums;

    /// <summary>
    /// Represents a document category that defines how and where files are stored.
    /// </summary>
    //[GenerateExposers]
    //[SdmDomStorage("(slc)documenthub")]
    public class DocumentCategory : SdmObject<DocumentCategory>
    {
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
        public SdmObjectReference<DomSource> DOMSource { get; set; }

        /// <summary>
        /// Gets or sets a string value indicating the definition associated with this category.
        /// </summary>
        public string Definition { get; set; }
    }
}
