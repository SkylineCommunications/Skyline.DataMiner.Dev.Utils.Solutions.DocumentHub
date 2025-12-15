using Microsoft.Graph;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
    public interface IDocHubFile
    {
        string GetFilePath();
        string GetFile();
        string GetType();
        string GetExtension();
        string GetName();
        string GetSize();
        DateTime GetCreatedAt();
        string GetCreatedBy();
        string GetDirectory();
    }

    public class DriveItemAdapter : IDocHubFile
    {
        public DriveItem driveItem;

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
            if (driveItem?.ParentReference?.Path == null)
                return string.Empty;

            var path = driveItem.ParentReference.Path;

            // Remove Graph prefix
            const string prefix = "/drive/root:";
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(prefix.Length);
            }

            path = path.Trim('/');

            // Split on slash
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // segments[0] is the document library name
            if (segments.Length <= 1)
                return string.Empty;

            // Recombine everything after the library
            return string.Join("/", segments, 1, segments.Length - 1);
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

        string IDocHubFile.GetType()
        {
            return string.Empty;
        }
    }

    public class FileInfoAdapter : IDocHubFile
    {
        public FileInfo fileInfo;

        public string GetFilePath()
        {
            return "\\" + fileInfo.FullName.Replace(@"c:\Skyline DataMiner\Webpages\", string.Empty);
        }

        public string GetFile()
        {
            return fileInfo.Name;
        }

        public string GetType()
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
            // Define root portion to strip from path.
            string partToRemove = @"c:\Skyline DataMiner\Webpages\Public\WebFileManager";

            // Return relative path (removing root and trimming leading backslashes).
            return fileInfo.DirectoryName.Replace(partToRemove, string.Empty).TrimStart('\\');
        }
    }
}
