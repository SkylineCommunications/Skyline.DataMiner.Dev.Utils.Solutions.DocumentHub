namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal sealed class NativeSharePointHandler : IStorageHandler, ISearchableStorageHandler
	{
		private readonly IConnection connection;
		private readonly DocumentBucket bucket;
		private readonly Guid sourceId;

		internal NativeSharePointHandler(IConnection connection, DocumentBucket bucket)
		{
			this.connection = connection;
			this.bucket = bucket;
			sourceId = SharePointCredentialReference.Parse(bucket.SharePointConfiguration.Identifier);
		}

		public void DownloadFile(IDocHubFile file, string destinationPath)
		{
			if (!(file is SharePointFileInfo info))
				throw new ArgumentException("The file must have been read from this SharePoint source.", nameof(file));

			Execute(new SharePointScriptRequest { Operation = "Download", FileId = info.Id, LocalPath = destinationPath });
		}

		public bool FileExists(FileExistsData data)
		{
			if (!(data is WebFileExistsData web))
				throw new ArgumentException("SharePoint requires web file data.", nameof(data));

			return Execute(new SharePointScriptRequest { Operation = "Exists", Name = web.Name, UploadPath = web.Directory }).Value == "True";
		}

		public string UploadFile(UploadData data)
		{
			return Execute(new SharePointScriptRequest
			{
				Operation = "Upload",
				Name = data.Name,
				LocalPath = data.FilePath,
				UploadPath = data.Bucket.UploadPath,
			}).Value;
		}

		public List<IDocHubFile> ReadFiles(ReadData data)
		{
			return Read(data.Context, data.Filter, null);
		}

		public Task<List<IDocHubFile>> ReadFilesAsync(ReadData data)
		{
			return Task.FromResult(ReadFiles(data));
		}

		public List<IDocHubFile> SearchFiles(SearchData data)
		{
			return Read(data.Context, data.Query, data.Region, true);
		}

		// A script that declares the source's credential reads SharePoint itself. Any other caller, such as a GQI
		// data source, has its operations run by the DocumentHub script that declares it.
		internal static IStorageHandler Create(IConnection connection, DocumentBucket bucket, Func<string, string> scriptCredentials)
		{
			var proxy = new NativeSharePointHandler(connection, bucket);
			var configuration = scriptCredentials == null ? null : new DocumentHubApiHelper(connection).SharePointConfigurations
				.Read(SharePointConfigurationExposers.Identifier.Equal(proxy.sourceId.ToString("D"))).SingleOrDefault();
			var secret = configuration == null ? null : scriptCredentials(new SharePointCredentialReference(configuration).Name);
			if (string.IsNullOrEmpty(secret))
				return proxy;

			return new SharePointHandler(configuration, () => secret);
		}

		private List<IDocHubFile> Read(DocHubPageData context, string filter, string region, bool search = false)
		{
			var output = new List<IDocHubFile>();
			var offset = context?.NativeOffset ?? 0;
			do
			{
				var result = Execute(new SharePointScriptRequest
				{
					Operation = search ? "Search" : "Read",
					Filter = filter,
					Region = region ?? "EMEA",
					Offset = offset,
					PageSize = Math.Min(context?.PageSize ?? 200, 200),
				});
				output.AddRange(result.Files);
				offset = result.NextOffset;
				if (context != null)
				{
					context.NativeOffset = offset;
					context.NativeHasMore = result.HasMore;
				}

				if (context != null || !result.HasMore)
					return output;
			}
			while (true);
		}

		private SharePointScriptResult Execute(SharePointScriptRequest request)
		{
			request.SourceId = sourceId.ToString("D");
			request.BucketId = bucket.Identifier;
			return SharePointScriptTransport.Execute(connection, request);
		}
	}
}
