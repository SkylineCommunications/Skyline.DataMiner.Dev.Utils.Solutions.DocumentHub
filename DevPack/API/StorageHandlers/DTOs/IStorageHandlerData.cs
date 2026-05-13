namespace Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal interface IStorageHandlerData
	{
	}

	#region Upload
	internal class UploadData : IStorageHandlerData
	{
		public UploadData()
		{
		}

		internal UploadData(UploadData data)
		{
			Bucket = data.Bucket;
			FilePath = data.FilePath;
			Name = data.Name;
		}

		public DocumentBucket Bucket { get; set; }

		public string FilePath { get; set; }

		public string Name { get; set; }
	}

	internal class WebFileUploadData : UploadData
	{
	}

	internal class DomFileUploadData : UploadData
	{
		internal DomFileUploadData(UploadData data)
			: base(data)
		{
		}

		public Guid DomInstanceId { get; set; }

	}
	#endregion

	#region Read
	internal class ReadData : IStorageHandlerData
	{
		public DocumentBucket Bucket { get; set; }

		public string Filter { get; set; }

		public DocHubPageData Context { get; set; }
	}

	internal class WebFileReadData : ReadData
	{
	}

	internal class DomFileReadData : ReadData
	{
		public string Module { get; set; }

		public List<Guid> DomInstanceIds { get; set; }
	}
	#endregion

	#region File Exists
	internal class FileExistsData : IStorageHandlerData
	{
		public string Name { get; set; }
	}

	internal class WebFileExistsData : FileExistsData
	{
		public string Directory { get; set; }
	}

	internal class DomFileExistsData : FileExistsData
	{
		public DocumentBucket Bucket { get; set; }

		public Guid DomInstanceId { get; set; }
	}
	#endregion

}