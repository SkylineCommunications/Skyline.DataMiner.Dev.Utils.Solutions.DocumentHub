namespace Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters
{
	using System;
	using System.IO;
	using System.Security.AccessControl;
	using System.Security.Principal;

	internal class FileInfoAdapter : IDocHubFile
	{
		internal FileInfo fileInfo;

		public static string GetRelativePath(string fullPath, string partToRemove)
		{
			var rootPath = partToRemove;
			var directoryPath = fullPath;

			if (string.IsNullOrEmpty(directoryPath))
				return string.Empty;

			var rootFullPath = Path.GetFullPath(rootPath)
								   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			var dirFullPath = Path.GetFullPath(directoryPath)
								  .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// The paths are equal: nothing remains relative to the root.
			if (dirFullPath.Equals(rootFullPath, StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}

			// Ensure the full path is actually *inside* the root directory and not
			// merely sharing a prefix (e.g. root "C:\Web" must not match "C:\Web2").
			// The character following the root must be a directory separator.
			if (!dirFullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase) ||
				(dirFullPath[rootFullPath.Length] != Path.DirectorySeparatorChar &&
				 dirFullPath[rootFullPath.Length] != Path.AltDirectorySeparatorChar))
			{
				return string.Empty;
			}

			var relativePath = dirFullPath.Substring(rootFullPath.Length)
										  .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// Normalize to forward slashes so web paths are consistent
			// (Windows filesystem paths use '\', but web/URL paths use '/').
			return relativePath.Replace(Path.DirectorySeparatorChar, '/')
							   .Replace(Path.AltDirectorySeparatorChar, '/');
		}

		public string GetFilePath()
		{
			return fileInfo.FullName;
		}

		public string GetWebPath()
		{
			return '/' + GetRelativePath(fileInfo.FullName, @"C:\Skyline DataMiner\Webpages");
		}

		public string GetFile()
		{
			return fileInfo.Name;
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