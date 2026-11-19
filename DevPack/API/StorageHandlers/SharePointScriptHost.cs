namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Text;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	/// <summary>Executes SharePoint operations inside a script that declares the source's library credential.</summary>
	public static class SharePointScriptHost
	{
		/// <summary>Serves callers that cannot declare a credential themselves; never returns a credential.</summary>
		/// <param name="connection">The executing user's connection, not a local-loop connection.</param>
		/// <param name="getSecret">
		/// Retrieves a credential that the running script declares, by name, for example
		/// <c>name => engine.GetCredential(name)?.GetToken()</c>.
		/// </param>
		/// <param name="request">Base64-encoded non-secret operation data.</param>
		/// <returns>Non-secret operation results.</returns>
		public static string Execute(IConnection connection, Func<string, string> getSecret, string request)
		{
			if (getSecret == null)
				throw new ArgumentNullException(nameof(getSecret));

			var input = SecureNewtonsoftDeserialization.DeserializeObject<SharePointScriptRequest>(Encoding.UTF8.GetString(Convert.FromBase64String(request)))
				?? throw new ArgumentException("An operation is required.");
			var source = SharePointCredentialReference.Parse(input.SourceId);
			var api = new DocumentHubApiHelper(connection);
			var configuration = api.SharePointConfigurations
				.Read(SharePointConfigurationExposers.Identifier.Equal(source.ToString("D"))).SingleOrDefault()
				?? throw new UnauthorizedAccessException("The SharePoint source is unavailable to this caller.");
			Func<string> getToken = () => getSecret(new SharePointCredentialReference(configuration).Name)
				?? throw new InvalidOperationException("This script declares no credential for the source.");

			if (input.Operation == "Check")
			{
				var provider = new SharePointCredentialProvider(getToken);
				return JsonConvert.SerializeObject(new SharePointScriptResult
				{
					Value = SharePointConnectionTester.IsConnected(configuration, provider.CreateCredential(configuration.TenantID, configuration.ClientID)).ToString(),
				});
			}

			if (input.Operation == "Has")
				return JsonConvert.SerializeObject(new SharePointScriptResult { Value = (!string.IsNullOrEmpty(getToken())).ToString() });

			var bucketId = SharePointCredentialReference.Parse(input.BucketId);
			var bucket = api.DocumentBuckets.Read(DocumentBucketExposers.Identifier.Equal(bucketId.ToString("D"))).SingleOrDefault();
			if (bucket?.StorageType != StorageType.SharePoint
				|| !Guid.TryParse(bucket.SharePointConfiguration.Identifier, out var linkedSource) || linkedSource != source)
			{
				throw new UnauthorizedAccessException("The bucket is unavailable to this source and caller.");
			}

			ValidateRequest(connection, bucket, input);
			var handler = new SharePointHandler(configuration, getToken);
			return JsonConvert.SerializeObject(ExecuteOperation(connection, handler, bucket, input));
		}

		internal static SharePointScriptResult ReadPage(IStorageHandler handler, DocumentBucket bucket, SharePointScriptRequest input)
		{
			// No caller-controlled Graph continuation URLs or cached DriveItems cross the script boundary.
			// Replaying to an offset costs additional Graph calls but preserves the source authorization boundary.
			var search = input.Operation == "Search" || !string.IsNullOrWhiteSpace(input.Filter);
			var searchable = handler as ISearchableStorageHandler;
			if (search && searchable == null)
				throw new ArgumentException("The SharePoint handler does not support search.", nameof(handler));
			DocHubPageData context = search
				? (DocHubPageData)new SharePointSearchPageData { PageSize = input.PageSize }
				: new SharePointPageData { PageSize = input.PageSize };
			var seen = 0;
			var result = new SharePointScriptResult();
			do
			{
				var files = search
					? searchable.SearchFiles(new WebFileSearchData { Bucket = bucket, Query = input.Filter, Region = input.Region, Context = context })
					: handler.ReadFiles(new WebFileReadData { Bucket = bucket, Filter = input.Filter, Context = context });
				foreach (var file in files.OfType<DriveItemAdapter>())
				{
					if (seen++ >= input.Offset)
						result.Files.Add(SharePointFileInfo.From(file));
				}
			}
			while (seen <= input.Offset && context.HasNextPage());

			result.NextOffset = seen;
			result.HasMore = context.HasNextPage();
			return result;
		}

		internal static void ValidateUploadPath(string root, string candidate)
		{
			var normalizedRoot = Uri.UnescapeDataString(root ?? string.Empty).Replace('\\', '/').Trim('/');
			var normalized = Uri.UnescapeDataString(candidate ?? string.Empty).Replace('\\', '/').Trim('/');
			if (normalized.Split('/').Any(s => s == "." || s == "..")
				|| (normalizedRoot.Length > 0 && normalized != normalizedRoot
					&& !normalized.StartsWith(normalizedRoot + "/", StringComparison.Ordinal)))
			{
				throw new UnauthorizedAccessException("The upload directory is outside this bucket.");
			}
		}

		internal static void ValidateLocalPath(IConnection connection, string path, bool upload)
		{
			var full = Path.GetFullPath(path);
			var root = upload ? @"C:\Skyline DataMiner\TempDocuments\" : @"C:\Skyline DataMiner\Documents\DocumentHub\Temp\";
			if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				// Existing C# and UDAPI callers may use arbitrary agent-local files. That capability
				// must not become available to ordinary users through the new credential consumer.
				new Secrets(connection).RequireManagementPermission();
			}
			else if (!upload && File.Exists(full))
			{
				throw new UnauthorizedAccessException("Credential-consuming downloads require a new destination file.");
			}

			for (var directory = new DirectoryInfo(Path.GetDirectoryName(full)); directory != null; directory = directory.Parent)
			{
				if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
					throw new UnauthorizedAccessException("Reparse-point paths are not supported for credential-consuming file operations.");
			}

			if (File.Exists(full) && (File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
				throw new UnauthorizedAccessException("Reparse-point files are not supported for credential-consuming file operations.");
		}

		private static void ValidateRequest(IConnection connection, DocumentBucket bucket, SharePointScriptRequest input)
		{
			switch (input.Operation)
			{
				case "Read":
				case "Search":
					if (input.Offset < 0 || input.PageSize < 1 || input.PageSize > 200)
						throw new ArgumentException("Invalid SharePoint page bounds.");
					break;
				case "Exists":
					ValidateUploadPath(bucket.UploadPath, input.UploadPath);
					ValidateUploadPath(bucket.UploadPath, input.UploadPath + "/" + input.Name);
					break;
				case "Upload":
					ValidateLocalPath(connection, input.LocalPath, true);
					ValidateUploadPath(bucket.UploadPath, input.UploadPath);
					break;
				case "Download":
					ValidateLocalPath(connection, input.LocalPath, false);
					break;
				default:
					throw new ArgumentException("The SharePoint operation is not supported.");
			}
		}

		private static SharePointScriptResult ExecuteOperation(IConnection connection, SharePointHandler handler, DocumentBucket bucket, SharePointScriptRequest input)
		{
			switch (input.Operation)
			{
				case "Read":
				case "Search":
					return ReadPage(handler, bucket, input);
				case "Exists":
					return new SharePointScriptResult { Value = handler.FileExists(new WebFileExistsData { Directory = input.UploadPath, Name = input.Name }).ToString() };
				case "Upload":
					var upload = new FileValidator(connection).ValidateAndSanitizeFile(bucket, input.LocalPath, input.Name);
					upload.Bucket.UploadPath = input.UploadPath;
					if (handler.FileExists(new WebFileExistsData { Directory = input.UploadPath, Name = upload.Name }))
						throw new InvalidOperationException("The file already exists.");
					return new SharePointScriptResult { Value = handler.UploadFile(new WebFileUploadData(upload)) };
				case "Download":
					// Resolve the item through the authorized bucket, not through an arbitrary caller-supplied Graph ID.
					var page = new SharePointPageData { PageSize = 200 };
					do
					{
						var file = handler.ReadFiles(new WebFileReadData { Bucket = bucket, Context = page })
							.OfType<DriveItemAdapter>().FirstOrDefault(f => f.DriveItem.Id == input.FileId);
						if (file != null)
						{
							handler.DownloadFile(file, input.LocalPath);
							return new SharePointScriptResult();
						}
					}
					while (page.HasNextPage());
					throw new UnauthorizedAccessException("The file is unavailable in this bucket.");
				default:
					throw new ArgumentException("The SharePoint operation is not supported.");
			}
		}
	}
}
