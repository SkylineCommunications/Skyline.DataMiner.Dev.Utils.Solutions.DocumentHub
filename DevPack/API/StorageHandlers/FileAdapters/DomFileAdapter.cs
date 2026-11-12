namespace Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters
{
	using System;
	using System.IO;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	/// <summary>
	/// Represents a file that provides access to a DocHub DOM (DataMiner Object Model) structure.
	/// </summary>
	/// <remarks>
	/// This interface extends <see cref="IDocHubFile"/> to indicate that the file supports DOM-based
	/// operations.
	/// Implementations may provide additional methods or properties for interacting with the document
	/// structure.
	/// <para>
	/// Files returned by DocumentHub require on-Agent filesystem access for size, creation time, and
	/// Windows file owner metadata. The owner is not necessarily the uploader, and the creation time
	/// belongs to the stored attachment file rather than the DOM instance or original document.
	/// </para>
	/// </remarks>
	public interface IDocHubDomFile : IDocHubFile
	{
	}

	internal class DomFileAdapter : IDocHubDomFile
	{
		private readonly FileInfoAdapter metadata;

		internal DomFileAdapter(FileInfo fileInfo)
		{
			metadata = new FileInfoAdapter
			{
				fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo)),
			};
		}

		internal DomInstance Instance { get; set; }

		internal string Module { get; set; }

		internal string Filename { get; set; }

		public DateTime GetCreatedAt()
		{
			return metadata.GetCreatedAt();
		}

		public string GetCreatedBy()
		{
			return metadata.GetCreatedBy();
		}

		public string GetDirectory()
		{
			return string.Empty;
		}

		public string GetExtension()
		{
			return Path.GetExtension(Filename).TrimStart('.');
		}

		public string GetFile()
		{
			return Filename;
		}

		public string GetFilePath()
		{
			return string.Empty;
		}

		public string GetWebPath()
		{
			return string.Empty;
		}

		public Guid GetInstanceId()
		{
			return Instance.ID.Id;
		}

		public string GetInstanceName()
		{
			return Instance.Name;
		}

		public string GetModule()
		{
			return Module;
		}

		public string GetName()
		{
			return Path.GetFileNameWithoutExtension(Filename);
		}

		public string GetSize()
		{
			return metadata.GetSize();
		}
	}
}