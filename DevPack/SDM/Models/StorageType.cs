namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Models
{
    /// <summary>
    /// Enumeration representing the different types of storage backends supported by the Document Hub.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// Representing local file system storage.
        /// </summary>
        Local = 0,

        /// <summary>
        /// Represents a SharePoint data source type.
        /// </summary>
        SharePoint = 1,

        /// <summary>
        /// Represents the DOM Attachment storage type.
        /// </summary>
        DOM = 2,
    }
}