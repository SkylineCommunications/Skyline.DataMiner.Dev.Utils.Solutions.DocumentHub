namespace Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters
{
	using System;

	// Deliberately project metadata, never Graph AdditionalData (which can contain pre-authenticated download URLs).
	internal sealed class SharePointFileInfo : IDocHubWebFile
	{
		public string Id { get; set; }

		public string Name { get; set; }

		public string Path { get; set; }

		public string Directory { get; set; }

		public string Size { get; set; }

		public DateTime CreatedAt { get; set; }

		public string CreatedBy { get; set; }

		public string GetFilePath() => Path;

		public string GetWebPath() => Path;

		public string GetFile() => Name;

		public string GetExtension() => System.IO.Path.GetExtension(Name).TrimStart('.');

		public string GetName() => System.IO.Path.GetFileNameWithoutExtension(Name);

		public string GetSize() => Size;

		public DateTime GetCreatedAt() => CreatedAt;

		public string GetCreatedBy() => CreatedBy;

		public string GetDirectory() => Directory;

		public string GetModule() => string.Empty;

		public string GetInstanceName() => string.Empty;

		public Guid GetInstanceId() => Guid.Empty;

		internal static SharePointFileInfo From(DriveItemAdapter file)
		{
			return new SharePointFileInfo
			{
				Id = file.DriveItem.Id,
				Name = file.GetFile(),
				Path = file.GetWebPath(),
				Directory = file.GetDirectory(),
				Size = file.GetSize(),
				CreatedAt = file.GetCreatedAt(),
				CreatedBy = file.GetCreatedBy(),
			};
		}
	}
}
