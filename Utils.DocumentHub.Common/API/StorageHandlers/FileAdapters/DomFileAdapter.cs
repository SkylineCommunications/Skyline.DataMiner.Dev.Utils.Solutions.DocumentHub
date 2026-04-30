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
    /// </remarks>
    public interface IDocHubDomFile : IDocHubFile
	{
	}

	internal class DomFileAdapter : IDocHubDomFile
	{
		internal DomInstance Instance { get; set; }

		internal string Module { get; set; }

		internal string Filename { get; set; }

		public DateTime GetCreatedAt()
		{
			return DateTime.MinValue.ToUniversalTime();
		}

		public string GetCreatedBy()
		{
			return string.Empty;
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
			return string.Empty;
		}

		string IDocHubFile.GetType()
		{
			return string.Empty;
		}
	}
}