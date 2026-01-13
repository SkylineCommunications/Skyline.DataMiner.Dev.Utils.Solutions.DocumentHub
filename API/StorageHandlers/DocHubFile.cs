using Microsoft.Graph;
using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
    /// <summary>
	/// Represents a file returned by the DocumentHub API.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface abstract the underlying storage system,
	/// such as local file storage or SharePoint document libraries.
	/// </remarks>
	public interface IDocHubFile
    {
        /// <summary>
        /// Gets the full path to the file within the storage backend.
        /// </summary>
        /// <returns>
        /// A storage specific path identifying the file location.
        /// </returns>
        string GetFilePath();

        /// <summary>
        /// Gets the full file reference used to access or download the file.
        /// </summary>
        /// <returns>
        /// A string representing the file reference.
        /// </returns>
        string GetFile();

        /// <summary>
        /// Gets the type of the item.
        /// </summary>
        /// <returns>
        /// A string describing the file type.
        /// Example. File
        /// </returns>
        string GetType();

        /// <summary>
        /// Gets the file extension.
        /// </summary>
        /// <returns>
        /// The file extension including the leading dot.
        /// Example. .pdf
        /// </returns>
        string GetExtension();

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns>
        /// The file name including extension.
        /// </returns>
        string GetName();

        /// <summary>
        /// Gets the size of the file.
        /// </summary>
        /// <returns>
        /// File size represented as a string.
        /// </returns>
        string GetSize();

        /// <summary>
        /// Gets the creation date and time of the file.
        /// </summary>
        /// <returns>
        /// The creation timestamp in UTC.
        /// </returns>
        DateTime GetCreatedAt();

        /// <summary>
        /// Gets the identifier of the user or system that created the file.
        /// </summary>
        /// <returns>
        /// Creator name or identifier when available.
        /// </returns>
        string GetCreatedBy();

        /// <summary>
        /// Gets the directory containing the file.
        /// </summary>
        /// <returns>
        /// A path relative to the storage root.
        /// </returns>
        string GetDirectory();
    }

    internal class DriveItemAdapter : IDocHubFile
    {
        internal DriveItem driveItem;
        internal Models.SharePointConfiguration sharePoint;

        internal DriveItemAdapter(Models.SharePointConfiguration sharePoint)
        {
            this.sharePoint = sharePoint;
        }

        public DateTime GetCreatedAt()
        {
            return driveItem.CreatedDateTime?.UtcDateTime ?? DateTime.MinValue;
        }

        public string GetCreatedBy()
        {
            return driveItem.CreatedBy?.User?.DisplayName;
        }

        public string GetDirectory()
        {
            // Ensure the site URL starts with a valid protocol.
            var siteUrl = sharePoint.SiteURL;
            if (!siteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !siteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                siteUrl = "https://" + siteUrl;
            }

            // Attempt to create valid URI objects.
            if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var siteUri) ||
                !Uri.TryCreate(driveItem.WebUrl, UriKind.Absolute, out var fileUri))
            {
                return string.Empty;
            }

            // Decode and extract path components.
            var sitePath = Uri.UnescapeDataString(siteUri.AbsolutePath);
            var filePath = Uri.UnescapeDataString(fileUri.AbsolutePath);

            // Compute relative path of file to site root.
            var relativePath = filePath.StartsWith(sitePath)
                ? filePath.Substring(sitePath.Length).TrimStart('/')
                : filePath.TrimStart('/');

            // Return directory portion (everything before last slash).
            var lastSlashIndex = relativePath.LastIndexOf('/');
            return lastSlashIndex >= 0
                ? relativePath.Substring(0, lastSlashIndex)
                : string.Empty;
        }

        public string GetExtension()
        {
            return Path.GetExtension(driveItem?.Name).TrimStart('.');
        }

        public string GetFile()
        {
            return driveItem?.Name;
        }

        public string GetFilePath()
        {
            return driveItem?.WebUrl;
        }

        public string GetName()
        {
            return Path.GetFileNameWithoutExtension(driveItem?.Name);
        }

        public string GetSize()
        {
            long? bytes = driveItem?.Size;

            // Handle missing value.
            if (bytes == null)
                return "0 B";

            // Supported size suffixes.
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes.Value;
            int order = 0;

            // Keep dividing by 1024 until size is in readable range.
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            // Return formatted size string.
            return $"{len:0.##} {sizes[order]}";
        }

        public new string GetType()
        {
            return string.Empty;
        }
    }

    internal class FileInfoAdapter : IDocHubFile
    {
        internal FileInfo fileInfo;

        public string GetFilePath()
        {
            return '/' + GetRelativePath(fileInfo.FullName, @"C:\Skyline DataMiner\Webpages");
        }

        public string GetFile()
        {
            return fileInfo.Name;
        }

        public new string GetType()
        {
            return Path.GetFileName(fileInfo.DirectoryName);
        }

        public string GetExtension()
        {
            return fileInfo.Extension.TrimStart('.');
        }

        public string GetName()
        {
            return Path.GetFileNameWithoutExtension(fileInfo.FullName);
        }

        public string GetSize()
        {
            // Get raw file size in bytes.
            long bytes = fileInfo.Length;

            // Supported size suffixes.
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            // Keep dividing by 1024 until size is in readable range.
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            // Format to 2 decimal places.
            return $"{len:0.##} {sizes[order]}";
        }

        public DateTime GetCreatedAt()
        {
            return fileInfo.CreationTimeUtc.ToUniversalTime();
        }

        public string GetCreatedBy()
        {
            // Retrieve file's security descriptor.
            FileSecurity fileSecurity = fileInfo.GetAccessControl();

            // Extract the owner identity.
            IdentityReference owner = fileSecurity.GetOwner(typeof(NTAccount));

            // Simplify domain\user format to just "user".
            return owner.Value.Contains("\\")
                ? owner.Value.Split('\\')[1]
                : owner.Value;
        }

        public string GetDirectory()
        {
            return GetRelativePath(fileInfo.DirectoryName, @"C:\Skyline DataMiner\Webpages\Public\WebFileManager");
        }

        public static string GetRelativePath(string fullPath, string partToRemove)
        {
            var rootPath = partToRemove;
            var directoryPath = fullPath;

            if (string.IsNullOrEmpty(directoryPath))
                return string.Empty;

            var rootFullPath = Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar);

            var dirFullPath = Path.GetFullPath(directoryPath)
                .TrimEnd(Path.DirectorySeparatorChar);

            if (!dirFullPath.StartsWith(
                    rootFullPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var relativePath = dirFullPath.Substring(rootFullPath.Length)
                .TrimStart(Path.DirectorySeparatorChar);

            return relativePath;
        }
    }
}
