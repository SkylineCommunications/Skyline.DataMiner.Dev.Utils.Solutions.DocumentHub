namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
    using Azure.Identity;
	using Microsoft.Graph;
	using Microsoft.Graph.Models;
    using Microsoft.Graph.Models.ODataErrors;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
    using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using File = System.IO.File;

	/// <summary>
	/// SharePoint storage handler implementation using Microsoft Graph.
	/// </summary>
	/// <remarks>
	/// This class provides file enumeration, existence checks, folder creation, and uploads
	/// to a SharePoint document library.  
	/// <para>
	/// ⚠ Paging is implemented manually because Microsoft Graph paginates per-folder, not recursively.
	/// This means logical paging must aggregate multiple Graph pages and maintain internal buffers.
	/// </para>
	/// </remarks>
	/// <seealso cref="IStorageHandler"/>
	/// <seealso cref="GraphServiceClient"/>
	/// <seealso cref="SharePointPageData"/>
	/// <example>
	/// Typical usage:
	/// <code>
	/// var handler = new SharePointHandler(helpers, connection);
	/// var files = handler.ReadFiles(new WebFileReadData { Bucket = bucket });
	/// </code>
	/// </example>
	internal class SharePointHandler : IStorageHandler
	{
		#region Globals

		/// <summary>
		/// SharePoint DOM repository used to retrieve configurations from DataMiner Object Model.
		/// </summary>
		private readonly IRepository<SharePointConfiguration> _sharePointRepository;

		/// <summary>
		/// SharePoint configuration retrieved from the Document Hub configuration model.
		/// </summary>
		private readonly SharePointConfiguration _sharePoint;

		/// <summary>
		/// Microsoft Graph client used for SharePoint API operations.
		/// </summary>
		private readonly GraphServiceClient _graphClient;

		/// <summary>
		/// SharePoint site resolved from the configured Site URL.
		/// </summary>
		private readonly Site _site;

		/// <summary>
		/// Document library (drive) inside the SharePoint site.
		/// </summary>
		private readonly Drive _drive;
		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="SharePointHandler"/> class.
		/// </summary>
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
		/// <param name="bucket">
		/// The <see cref="DocumentBucket"/> containing the storage type and related configuration for which this handler is being created.
		/// </param>
		/// <exception cref="NullReferenceException">
		/// Thrown when the configured SharePoint document library cannot be found.
		/// </exception>
		internal SharePointHandler(IConnection connection, DocumentBucket bucket)
		{
			_sharePointRepository = new SharePointConfigurationDomRepository(connection);

			// Defensive check to avoid reinitialization
			if (_graphClient != null && _drive != null)
				return;

			if (!bucket.SharePointConfiguration.IsValidReference(out Guid identifierGuid))
			{
				throw new ArgumentException($"The provided {nameof(bucket)} does not have a SharePoint reference.");
			}

			// Retrieve SharePoint configuration from DOM
			var sharePointFilter = SharePointConfigurationExposers.Identifier.Equal(identifierGuid.ToString());
			_sharePoint = _sharePointRepository.Read(sharePointFilter).FirstOrDefault()
				?? throw new ArgumentException("No SharePoint configuration tied to the provided bucket could be found.", nameof(bucket));

			// Retrieve client secret
			var clientSecret = RetrieveClientSecret();

			// Authenticate using Azure AD client credentials flow
			var credential = new Azure.Identity.ClientSecretCredential(
				_sharePoint.TenantID,
				_sharePoint.ClientID,
				clientSecret);

			// Initialize Graph client
			_graphClient = new GraphServiceClient(credential);

			// Build site URI components
			var uri = new UriBuilder("https://" + _sharePoint.SiteURL).Uri;
			string hostname = uri.Host;
			string path = uri.AbsolutePath;

			// Resolve SharePoint site
			_site = _graphClient.Sites[$"{hostname}:{path}"]
				.GetAsync()
				.GetAwaiter()
				.GetResult();

			// Retrieve document libraries (drives)
			var drives = _graphClient.Sites[_site.Id]
				.Drives
				.GetAsync()
				.GetAwaiter()
				.GetResult();

			// Select configured document library
			_drive = drives.Value.FirstOrDefault(d => d.Name.Equals(_sharePoint.DocumentLibraryName, StringComparison.OrdinalIgnoreCase));
			if (_drive == null)
				throw new NullReferenceException($"Library '{_sharePoint.DocumentLibraryName}' not found.");
		}

		#endregion

		#region Public

		/// <summary>
		/// Reads all files using recursive traversal with manual paging.
		/// </summary>
		/// <remarks>
		/// ⚠ Microsoft Graph paginates per folder, so this method aggregates multiple folder pages
		/// into a single logical page using <see cref="SharePointPageData"/>.
		/// </remarks>
		public List<IDocHubFile> ReadFiles(ReadData data)
		{
			return ReadFilesAsync(data)
				.GetAwaiter()
				.GetResult();
		}

        /// <summary>
        /// Reads all files using recursive traversal with manual paging.
        /// </summary>
        /// <remarks>
        /// ⚠ Microsoft Graph paginates per folder, so this method aggregates multiple folder pages
        /// into a single logical page using <see cref="SharePointPageData"/>.
        /// </remarks>
        public async Task<List<IDocHubFile>> ReadFilesAsync(ReadData data)
        {
            if (!(data is WebFileReadData args))
                throw new ArgumentException("SharePointHandler requires WebFileReadData.", nameof(data));

            // If paging context exists, return next page only
            if (args.Context != null)
                return await ReadPage(args);

            // Initialize paging context
            var context = new SharePointPageData();
            args.Context = context;

            var files = new List<IDocHubFile>();

            // Iterate until no more data is available
            while (context.HasNextPage())
            {
                var page = await ReadPage(args);

                // Safety break if no results and no further pages
                if (page.Count == 0 && !context.HasNextPage())
                {
                    break;
                }

				files.AddRange(page);
			}

            return files;
        }

        /// <summary>
        /// Checks whether a file exists in SharePoint.
        /// </summary>
        public bool FileExists(FileExistsData data)
		{
			if (!(data is WebFileExistsData args))
				throw new ArgumentException("SharePointHandler requires WebFileExistsData.", nameof(data));

			var directory = args.Directory;
			var name = args.Name;

			return FileExistsAsync(directory, name)
				.GetAwaiter()
				.GetResult();
		}

		/// <summary>
		/// Uploads a file from disk to SharePoint.
		/// </summary>
		public string UploadFile(UploadData data)
		{
			if (!(data is WebFileUploadData args))
				throw new ArgumentException("SharePointHandler requires WebFileUploadData.", nameof(data));

			var bucket = args.Bucket;
			var filePath = args.FilePath;
			var name = args.Name;

			return UploadFileAsync(bucket.UploadPath, filePath, name)
				.GetAwaiter()
				.GetResult();
		}

		/// <summary>
		/// Uploads an image to SharePoint as a JPEG file.
		/// </summary>
		public void UploadImage(Bitmap image, string directory, string name)
		{
			UploadImageAsync(image, directory, name)
				.GetAwaiter()
				.GetResult();
		}

        #endregion

        #region Private

        /// <summary>
        /// Enqueues subfolders discovered in the current Graph page.
        /// </summary>
        private static void EnqueueSubFolders(SharePointPageData context, DriveItemCollectionResponse page)
        {
            if (page?.Value == null) return;

            foreach (var folder in page.Value)
            {
                if (folder.Folder != null)
                    context.FolderQueue.Enqueue(folder.Id);
            }
        }

        /// <summary>
        /// Reads a single logical page of SharePoint files.
        /// </summary>
        private async Task<List<IDocHubFile>> ReadPage(ReadData data)
		{
			// Validate and cast input
			var args = data as WebFileReadData;
			if (args == null)
				throw new ArgumentException("SharePointHandler requires WebFileReadData.", nameof(data));

			var spContext = args.Context as SharePointPageData;
			if (spContext == null)
				throw new ArgumentException("SharePointHandler requires SharePointPageContext.", nameof(args.Context));

			var bucket = args.Bucket;
			var filter = args.Filter;

			// Initialize bucket root exactly once (replace initial "root" sentinel)
			if (bucket != null
				&& spContext.FolderQueue.Count == 1
				&& spContext.FolderQueue.Peek() == "root"
				&& spContext.NextPageLink == null)
			{
				var trimmedPath = (bucket.UploadPath ?? string.Empty).Trim('/', '\\');

				DriveItem folder;
				if (string.IsNullOrEmpty(trimmedPath))
				{
					folder = (await _graphClient
						.Sites[_site.Id]
						.Drives[_drive.Id]
						.GetAsync())
						.Root;
				}
				else
				{
					folder = await _graphClient
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(trimmedPath)
						.GetAsync();
				}

				if (folder == null || folder.Folder == null)
					throw new InvalidOperationException("Could not find folder with path /" + bucket.UploadPath);

				// Do NOT replace the queue instance (other code may hold references)
				spContext.FolderQueue.Clear();
				spContext.FolderQueue.Enqueue(folder.Id);
			}

			// End-of-traversal: no folders, no Graph pages, no buffered items
			if (spContext.FolderQueue.Count == 0
				&& spContext.NextPageLink == null
				&& spContext.PageRemainderBuffer.Count == 0)
			{
				return new List<IDocHubFile>();
			}

			// Parse allowed extensions from bucket
			var allowedExtensions = bucket != null && !string.IsNullOrEmpty(bucket.Extensions)
										? new HashSet<string>(bucket.Extensions.Split(','), StringComparer.OrdinalIgnoreCase)
										: null;

			// Fetch next logical page (Graph paging + remainder buffer)
			var driveItems = await FetchNextPageInternal(filter, allowedExtensions, spContext);

			// Wrap DriveItems in adapter objects
			var result = new List<IDocHubFile>(driveItems.Count);
			foreach (var item in driveItems)
			{
				result.Add(new DriveItemAdapter
				{
					DriveItem = item,
				});
			}

			return result;
		}

        /// <summary>
        /// Executes the next Graph page request and updates the continuation link.
        /// </summary>
        private async Task<DriveItemCollectionResponse> ExecuteGraphPageRequest(SharePointPageData context)
        {
            DriveItemCollectionResponse response;

            if (context.NextPageLink == null)
            {
                // First page for this folder
                context.CurrentFolderId = context.FolderQueue.Dequeue();

				response = await _graphClient
					.Drives[_drive.Id]
					.Items[context.CurrentFolderId]
					.Children
					.GetAsync(cfg => cfg.QueryParameters.Top = context.PageSize);
            }
            else
            {
				// Resume paging via @odata.nextLink
				response = await _graphClient
					.Drives[_drive.Id]
					.Items[context.CurrentFolderId]
					.Children
					.WithUrl(context.NextPageLink)
					.GetAsync();
            }

            context.NextPageLink = response?.OdataNextLink;
            return response;
        }

        /// <summary>
        /// Retrieves the next logical page of <see cref="DriveItem"/> objects by recursively
        /// traversing the folder queue, applying name and extension filters, and merging
        /// multiple Graph API responses into a single page of the configured size.
        /// </summary>
        /// <remarks>
        /// Microsoft Graph paginates children per folder, not across the entire drive.
        /// This method bridges that gap by consuming the <see cref="SharePointPageData.FolderQueue"/>,
        /// issuing per-folder requests, and collecting results until
        /// <see cref="DocHubPageData.PageSize"/> items are gathered.
        /// Any surplus items are stored in <see cref="SharePointPageData.PageRemainderBuffer"/>
        /// so they are returned on the next call rather than being lost.
        /// </remarks>
        /// <param name="filter">
        /// Optional substring filter applied to <see cref="DriveItem.Name"/> (case-insensitive).
        /// Pass <c>null</c> or empty to skip name filtering.
        /// </param>
        /// <param name="allowedExtensions">
        /// Optional set of file extensions (without leading dot) to include.
        /// Pass <c>null</c> to accept all extensions.
        /// </param>
        /// <param name="context">
        /// Paging state that tracks the folder queue, the current Graph continuation token,
        /// and any buffered overflow items from previous calls.
        /// </param>
        /// <returns>
        /// A list of <see cref="DriveItem"/> objects representing the next logical page of files.
        /// The list size is at most <see cref="DocHubPageData.PageSize"/>.
        /// </returns>
        private async Task<IList<DriveItem>> FetchNextPageInternal(string filter, HashSet<string> allowedExtensions, SharePointPageData context)
		{
			var collected = new List<DriveItem>(context.PageSize);

			// Drain buffered items from previous partial Graph pages
			DrainRemainderBuffer(context, collected);

			// Continue folder traversal until logical page is full or no data remains
			while (ShouldContinuePaging(context, collected))
			{
				EnsureNextPageRequest(context);

				var page = await ExecuteGraphPageRequest(context);

				EnqueueSubFolders(context, page);
				CollectFiles(filter, allowedExtensions, context, collected, page);
			}

			return collected;
		}

		/// <summary>
		/// Drains leftover items from the remainder buffer into the current page.
		/// </summary>
		private static void DrainRemainderBuffer(SharePointPageData context, ICollection<DriveItem> collected)
		{
			while (collected.Count < context.PageSize && context.PageRemainderBuffer.Count > 0)
			{
				collected.Add(context.PageRemainderBuffer.Dequeue());
			}
		}

        /// <summary>
        /// Determines whether paging should continue.
        /// </summary>
        private static bool ShouldContinuePaging(SharePointPageData context, ICollection<DriveItem> collected)
        {
            return collected.Count < context.PageSize &&
                   (context.FolderQueue.Count > 0 || context.NextPageLink != null);
        }

        /// <summary>
        /// Ensures a Graph paging request exists for the current folder.
        /// </summary>
        private void EnsureNextPageRequest(SharePointPageData context)
        {
            if (context.NextPageLink != null)
                return;

			var folderId = context.FolderQueue.Dequeue();

			context.NextPageLink = _graphClient
				.Drives[_drive.Id]
				.Items[folderId]
				.Children
				.GetAsync(conf => { conf.QueryParameters.Top = context.PageSize; })
				.Result
				.OdataNextLink;
        }

		/// <summary>
		/// Collects file items into the logical page and buffers overflow items.
		/// </summary>
		private static void CollectFiles(
			string filter, 
			HashSet<string> allowedExtensions, 
			SharePointPageData context, 
			ICollection<DriveItem> collected, 
			DriveItemCollectionResponse page)
		{
			var files = page.Value
				.Where(i => i.File != null &&
							(string.IsNullOrEmpty(filter) ||
							 i.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) &&
							(allowedExtensions == null ||
							 allowedExtensions.Contains(Path.GetExtension(i.Name).TrimStart('.'))))
				.ToList();

			foreach (var file in files)
			{
				if (collected.Count < context.PageSize)
				{
					collected.Add(file);
				}
				else
				{
					// Buffer overflow files for the next logical page
					context.PageRemainderBuffer.Enqueue(file);
				}
			}
		}

		/// <summary>
		/// Checks if a file exists asynchronously.
		/// </summary>
		private async Task<bool> FileExistsAsync(string directory, string name)
		{
			try
			{
				var rootDrive = await _graphClient
					.Drives[_drive.Id]
					.Root
					.GetAsync();

				var searchResponse = await _graphClient
					.Drives[_drive.Id]
					.Items[rootDrive.Id]
					.ItemWithPath($"{directory}/{name}")
					.GetAsync();

				return searchResponse != null && searchResponse.File != null && searchResponse.Name.Contains(name);
            }
			catch (ODataError ex)
			{
				if (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.NotFound)
					return false;

				throw;
			}
		}

		/// <summary>
		/// Uploads a file to SharePoint and returns the resulting Web URL.
		/// </summary>
		private async Task<string> UploadFileAsync(string directory, string filepath, string name)
		{
			try
			{
				// Normalize and validate directory path
				var normalizedDirectory = NormalizeDirectoryPath(directory);

				// Ensure folder structure exists (skip for root)
				if (!string.IsNullOrEmpty(normalizedDirectory))
				{
					await EnsureFolderPathExistsAsync(normalizedDirectory);
				}

				using (var stream = File.OpenRead(filepath))
				{
					DriveItem item;

					if (string.IsNullOrEmpty(normalizedDirectory))
					{
						// Upload to root directory
						item = await _graphClient
							//.Sites[_site.Id]
							.Drives[_drive.Id]
							.Root
							.ItemWithPath(name)
							.Content
							.PutAsync(stream);
					}
					else
					{
						// Upload to subdirectory
						var path = $"{normalizedDirectory}/{name}";
						item = await _graphClient
							.Drives[_drive.Id]
							.Root
							.ItemWithPath(path)
							.Content
							.PutAsync(stream);
					}

					return item?.WebUrl;
				}
			}
			catch (Exception e)
			{
				throw new IOException(ExtractMessage(e));
			}
		}

		/// <summary>
		/// Uploads a bitmap image as JPEG to SharePoint.
		/// </summary>
		private async Task UploadImageAsync(Bitmap image, string directory, string name)
		{
			try
			{
				// Normalize directory path
				var normalizedDirectory = NormalizeDirectoryPath(directory);
				var filename = $"{name}.jpeg";

				// Ensure folder structure exists (skip for root)
				if (!string.IsNullOrEmpty(normalizedDirectory))
				{
					await EnsureFolderPathExistsAsync(normalizedDirectory);
				}

				MemoryStream jpegStream = new MemoryStream();

				// Serialize image to memory
				image.Save(jpegStream, ImageFormat.Jpeg);
				jpegStream.Position = 0;

				string path;
				if (string.IsNullOrEmpty(normalizedDirectory))
				{
					// Upload to root directory
					path = filename;
				}
				else
				{
					// Upload to subdirectory
					path = $"{normalizedDirectory}/{filename}";
				}

				var item = await _graphClient
					.Drives[_drive.Id]
					.Root
					.ItemWithPath(path)
					.Content
					.PutAsync(jpegStream);
			}
			catch (Exception e)
			{
				throw new IOException(ExtractMessage(e));
			}
		}

		/// <summary>
		/// Ensures that a folder hierarchy exists in SharePoint, creating missing folders as needed.
		/// </summary>
		private async Task EnsureFolderPathExistsAsync(string directory)
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(directory))
				return;

			// Split and filter out empty/invalid segments
			var segments = directory.Trim('/', '\\', ' ', '\t')
				.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(s => !string.IsNullOrWhiteSpace(s) && s != "." && s != "..")
				.ToArray();

			// Nothing to create if no valid segments
			if (segments.Length == 0)
				return;

			string currentPath = string.Empty;
			foreach (var segment in segments)
			{
				currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

				try
				{
					// Check if folder exists
					await _graphClient
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(currentPath)
						.GetAsync();
				}
				catch (ODataError ex) when (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.NotFound)
				{
					// Folder missing -> create it
					var parentPath = Path.GetDirectoryName(currentPath).Replace("\\", "/");
					var folderName = Path.GetFileName(currentPath);

					var folder = new DriveItem
					{
						Name = folderName,
						Folder = new Folder(),
					};

					if (string.IsNullOrEmpty(parentPath) || parentPath == ".")
					{
						await _graphClient
							.Drives[_drive.Id]
							.Items["root"]
							.Children
							.PostAsync(folder);
					}
					else
					{
						await _graphClient
							.Drives[_drive.Id]
                            .Root
							.ItemWithPath(parentPath)
							.Children
							.PostAsync(folder);
					}
				}
				catch (Exception e)
				{
					throw new IOException(e.ToString());
				}
			}
		}

		/// <summary>
		/// Normalizes a directory path for use with SharePoint Graph API.
		/// </summary>
		/// <param name="directory">The raw directory path.</param>
		/// <returns>A normalized path string, or empty string for root directory.</returns>
		private string NormalizeDirectoryPath(string directory)
		{
			// Handle null or whitespace
			if (string.IsNullOrWhiteSpace(directory))
				return string.Empty;

			// Trim leading/trailing slashes, backslashes, and whitespace
			var normalized = directory.Trim('/', '\\', ' ', '\t');

			// Return empty for special cases
			if (string.IsNullOrWhiteSpace(normalized) || normalized == "." || normalized == "..")
				return string.Empty;

			// Remove any remaining relative path components and clean up
			var segments = normalized
				.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(s => !string.IsNullOrWhiteSpace(s) && s != "." && s != "..")
				.ToArray();

			if (segments.Length == 0)
				return string.Empty;

			// Reconstruct path with forward slashes
			return string.Join("/", segments);
		}

		private string RetrieveClientSecret()
		{
			string path = @"C:\Skyline DataMiner\Security\DocumentHub\graphsecret.dat";

			if (!File.Exists(path))
				throw new FileNotFoundException("Secret file not found", path);

			string encrypted = File.ReadAllText(path);
			string clientSecret = DPAPIHelper.Decrypt(encrypted);

			return clientSecret;
		}

		/// <summary>
		/// Extracts the most meaningful message from nested exceptions.
		/// </summary>
		private string ExtractMessage(Exception ex)
		{
			Exception current = ex;
			string lastMessage = ex.Message;

			while (current != null)
			{
				lastMessage = current.Message;

				if (current is Microsoft.Identity.Client.MsalServiceException)
				{
					return current.Message;
				}

				current = current.InnerException;
			}

			return lastMessage;
		}

        #endregion
    }
}
