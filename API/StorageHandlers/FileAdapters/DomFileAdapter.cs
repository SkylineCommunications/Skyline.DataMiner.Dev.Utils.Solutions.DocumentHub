namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters
{
	using System;
	using System.IO;
	using System.Linq;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.History;

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
