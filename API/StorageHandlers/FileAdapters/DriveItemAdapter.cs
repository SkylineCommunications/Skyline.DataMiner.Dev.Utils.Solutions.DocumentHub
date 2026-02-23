namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters
{
	using System;
	using System.IO;
	using API.DataHelpers;
	using Microsoft.Graph;

	public interface IDocHubWebFile : IDocHubFile
	{
	}

	internal class DriveItemAdapter : IDocHubWebFile
	{
		internal DriveItemAdapter()
		{
		}

		internal DriveItem DriveItem { get; set; }

		public DateTime GetCreatedAt()
		{
			return DriveItem.CreatedDateTime?.UtcDateTime ?? DateTime.MinValue;
		}

		public string GetCreatedBy()
		{
			return DriveItem.CreatedBy?.User?.DisplayName;
		}

		public string GetDirectory()
		{
			var path = DriveItem.ParentReference?.Path;
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			var rootIndex = path.IndexOf("root:/", StringComparison.OrdinalIgnoreCase);
			if (rootIndex < 0)
				return string.Empty;

			var relative = path.Substring(rootIndex + "root:/".Length).TrimStart('/');

			return relative;
		}

		public string GetExtension()
		{
			return Path.GetExtension(DriveItem?.Name).TrimStart('.');
		}

		public string GetFile()
		{
			return DriveItem?.Name;
		}

		public string GetFilePath()
		{
			return DriveItem?.WebUrl;
		}

		public Guid GetInstanceId()
		{
			return Guid.Empty;
		}

		public string GetInstanceName()
		{
			return string.Empty;
		}

		public string GetModule()
		{
			return string.Empty;
		}

		public string GetName()
		{
			return Path.GetFileNameWithoutExtension(DriveItem?.Name);
		}

		public string GetSize()
		{
			long? bytes = DriveItem?.Size;

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
}
