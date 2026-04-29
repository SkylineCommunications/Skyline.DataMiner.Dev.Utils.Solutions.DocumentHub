namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.IO;
	using System.Security.AccessControl;
	using System.Security.Principal;

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

		// test candidate
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

		public string GetModule()
		{
			return string.Empty;
		}

		public string GetInstanceName()
		{
			return string.Empty;
		}

		public Guid GetInstanceId()
		{
			return Guid.Empty;
		}
	}
}