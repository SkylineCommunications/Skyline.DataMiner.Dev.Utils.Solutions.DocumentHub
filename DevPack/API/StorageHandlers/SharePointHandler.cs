namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using Microsoft.Graph;
	using Microsoft.Graph.Models;
	using Microsoft.Graph.Models.ODataErrors;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.SharePointConfiguration;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Validation;
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
	internal class SharePointHandler : IStorageHandler, ISearchableStorageHandler
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

		private readonly Func<DriveItem, Stream> _openDownloadStream;
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

			_openDownloadStream = OpenDownloadStream;
		}

		internal SharePointHandler(Func<DriveItem, Stream> openDownloadStream)
		{
			_openDownloadStream = openDownloadStream ?? throw new ArgumentNullException(nameof(openDownloadStream));
		}

		#endregion

		#region Public

		/// <summary>
		/// Downloads a SharePoint file to the specified local destination.
		/// </summary>
		/// <param name="file">The SharePoint file to download.</param>
		/// <param name="destinationPath">The full local destination path.</param>
		public void DownloadFile(IDocHubFile file, string destinationPath)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!(file is DriveItemAdapter sharePointFile))
				throw new ArgumentException("SharePointHandler requires a SharePoint DocumentHub file.", nameof(file));
			if (sharePointFile.DriveItem == null || string.IsNullOrWhiteSpace(sharePointFile.DriveItem.Id))
				throw new ArgumentException("The SharePoint file does not contain a drive item identifier.", nameof(file));

			using (var source = _openDownloadStream(sharePointFile.DriveItem))
			{
				if (source == null)
					throw new IOException($"SharePoint returned no content for file '{file.GetFile()}'.");

				AtomicFileDownloader.Write(
					destinationPath,
					temporaryPath =>
					{
						using (var destination = File.Create(temporaryPath))
						{
							source.CopyTo(destination);
						}
					});
			}
		}

		/// <summary>
		/// Reads all files using recursive traversal with manual paging.
		/// </summary>
		/// <remarks>
		/// ⚠ Microsoft Graph paginates per folder, so this method aggregates multiple folder pages
		/// into a single logical page using <see cref="SharePointPageData"/>.
		/// </remarks>
		/// <returns>A <see cref="List{IDocHubFile}"/> of file abstractions.</returns>
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
		/// <returns>A <see cref="Task"/> representing the asynchronous operation returning a <see cref="List{IDocHubFile}"/> of file abstractions.</returns>
		public async Task<List<IDocHubFile>> ReadFilesAsync(ReadData data)
		{
			if (!(data is WebFileReadData args))
				throw new ArgumentException("SharePointHandler requires WebFileReadData.", nameof(data));

			// If a filter/query is provided when ReadFiles is called, we reroute to SearchFiles.
			if (!string.IsNullOrWhiteSpace(data.Filter))
			{
				var searchData = new WebFileSearchData
				{
					Bucket = data.Bucket,
					Query = data.Filter,
					Context = data.Context,
					Region = "EMEA", // Region is required for search requests with application-wide permissions.
				};

				return await SearchFilesAsync(searchData);
			}

			// If paging context exists, return next page only
			if (args.Context != null)
				return await ReadPage(args);

			var context = new SharePointPageData();
			args.Context = context;

			var files = new List<IDocHubFile>();

			while (context.HasNextPage())
			{
				var page = await ReadPage(args);
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
		/// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
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
		/// <returns>The relative path of the uploaded file.</returns>
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

		/// <summary>
		/// Executes a KQL search against the configured SharePoint document library using the
		/// Microsoft Graph <c>POST /search/query</c> endpoint (Microsoft Search API).
		/// </summary>
		/// <remarks>
		/// The <see cref="SearchData.Query"/> value accepts full Keyword Query Language, e.g.
		/// <c>filetype:pdf title:"design doc"</c>. Results are scoped to the folder configured
		/// on <see cref="DocumentBucket.UploadPath"/> by appending a <c>path:</c> clause derived
		/// from that folder's Graph <c>webUrl</c>. Bucket-level <see cref="DocumentBucket.Extensions"/>
		/// are enforced client-side, mirroring <see cref="ReadFiles(ReadData)"/>.
		/// </remarks>
		public List<IDocHubFile> SearchFiles(SearchData data)
		{
			return SearchFilesAsync(data)
				.GetAwaiter()
				.GetResult();
		}

		/// <summary>
		/// Async counterpart to <see cref="SearchFiles(SearchData)"/>.
		/// </summary>
		public async Task<List<IDocHubFile>> SearchFilesAsync(SearchData data)
		{
			if (!(data is WebFileSearchData args))
				throw new ArgumentException("SharePointHandler requires WebFileSearchData.", nameof(data));

			if (string.IsNullOrWhiteSpace(args.Query))
				throw new ArgumentException("A non-empty search query is required.", nameof(data));

			// If paging context exists, return next page only
			if (args.Context != null)
				return await SearchPage(args);

			// Initialize paging context and drain until end
			var context = new SharePointSearchPageData();
			args.Context = context;

			var files = new List<IDocHubFile>();

			while (context.HasNextPage())
			{
				var page = await SearchPage(args);

				// Safety break if no results and no further pages
				if (page.Count == 0 && !context.HasNextPage())
				{
					break;
				}

				files.AddRange(page);
			}

			return files;
		}

		#endregion

		#region Private

		private Stream OpenDownloadStream(DriveItem item)
		{
			return _graphClient
				.Drives[_drive.Id]
				.Items[item.Id]
				.Content
				.GetAsync()
				.GetAwaiter()
				.GetResult();
		}

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
			if (!(data is WebFileReadData args))
				throw new ArgumentException("SharePointHandler requires WebFileReadData.", nameof(data));

			if (!(args.Context is SharePointPageData spContext))
				throw new ArgumentException("SharePointHandler requires SharePointPageContext.", nameof(args.Context));

			var bucket = args.Bucket;
			var filter = args.Filter;

			// Initialize bucket root exactly once (replace initial "root" sentinel)
			if (bucket != null
				&& spContext.FolderQueue.Count == 1
				&& (spContext.FolderQueue.TryPeek(out string first) && first == "root")
				&& spContext.NextPageLink == null)
			{
				var trimmedPath = (bucket.UploadPath ?? string.Empty).Trim('/', '\\');

				DriveItem folder;
				if (string.IsNullOrEmpty(trimmedPath))
				{
					folder = await _graphClient
						.Drives[_drive.Id]
						.Root
						.GetAsync();
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
				spContext.FolderQueue.TryDequeue(out _);
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
				if (!context.FolderQueue.TryDequeue(out var folderId))
					return null;

				context.CurrentFolderId = folderId;

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
		/// Optional substring filter applied to <see cref="BaseItem.Name"/> of the <see cref="DriveItem"/> object. (case-insensitive).
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
				var page = await ExecuteGraphPageRequest(context);
				if (page == null)
					break;

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
				if (context.PageRemainderBuffer.TryDequeue(out DriveItem dequeued))
				{
					collected.Add(dequeued);
				}
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
		/// Collects file items into the logical page and buffers overflow items.
		/// </summary>
		private static void CollectFiles(
			string filter,
			HashSet<string> allowedExtensions,
			SharePointPageData context,
			ICollection<DriveItem> collected,
			DriveItemCollectionResponse page)
		{
			if (page?.Value == null) return;

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
				var searchResponse = await _graphClient
					.Drives[_drive.Id]
					.Root
					.ItemWithPath($"{directory}/{name}")
					.GetAsync();

				return searchResponse != null && searchResponse.File != null;
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
		private async Task<string> UploadFileAsync(string directory, string filePath, string name)
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

				using (var stream = File.OpenRead(filePath))
				{
					DriveItem item;
					string pathPart;
					if (string.IsNullOrEmpty(normalizedDirectory))
					{
						// Upload to root directory
						pathPart = name;
					}
					else
					{
						// Upload to subdirectory
						pathPart = $"{normalizedDirectory}/{name}";
					}

					item = await _graphClient
							.Drives[_drive.Id]
							.Root
							.ItemWithPath(pathPart)
							.Content
							.PutAsync(stream);

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

				using (MemoryStream jpegStream = new MemoryStream())
				{
					// Serialize image to memory
					image.Save(jpegStream, ImageFormat.Jpeg);
					jpegStream.Position = 0;

					// what to do with the other image formats: jpg, png, bmp, gif, etc.?
					string fileName = $"{name}.jpeg";
					string path;
					if (string.IsNullOrEmpty(normalizedDirectory))
					{
						// Upload to root directory
						path = fileName;
					}
					else
					{
						// Ensure the target folder exists before uploading
						await EnsureFolderPathExistsAsync(normalizedDirectory);

						// Upload to subdirectory
						path = $"{normalizedDirectory}/{fileName}";
					}

					var item = await _graphClient
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(path)
						.Content
						.PutAsync(jpegStream);
				}
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

		/// <summary>
		/// Reads a single logical page of SharePoint search results.
		/// </summary>
		private async Task<List<IDocHubFile>> SearchPage(WebFileSearchData args)
		{
			if (!(args.Context is SharePointSearchPageData spContext))
				throw new ArgumentException("SharePointHandler search requires SharePointSearchPageData.", nameof(args.Context));

			var bucket = args.Bucket;

			// End-of-traversal
			if (spContext.SearchStarted
				&& !spContext.MoreResultsAvailable
				&& spContext.PageRemainderBuffer.Count == 0)
			{
				return new List<IDocHubFile>();
			}

			// Resolve the bucket's UploadPath to (a) a KQL scoping clause applied server-side
			// and (b) the set of allowed parent folder ids used to filter hits client-side to
			// the bucket-folder subtree.
			if (spContext.ScopingClause == null)
			{
				await ResolveBucketScopingAsync(spContext, bucket);
			}

			var allowedExtensions = bucket != null && !string.IsNullOrEmpty(bucket.Extensions)
				? new HashSet<string>(bucket.Extensions.Split(','), StringComparer.OrdinalIgnoreCase)
				: null;

			var collected = new List<DriveItem>(spContext.PageSize);

			// Drain any leftovers from the previous logical page first
			while (collected.Count < spContext.PageSize && spContext.PageRemainderBuffer.TryDequeue(out var buffered))
			{
				collected.Add(buffered);
			}

			// Keep pulling Microsoft Search pages until logical page is filled or the search is exhausted
			while (collected.Count < spContext.PageSize
				   && (!spContext.SearchStarted || spContext.MoreResultsAvailable))
			{
				var container = await ExecuteSearchPageRequest(spContext, args);
				if (container == null)
					break;

				CollectSearchFiles(allowedExtensions, spContext, collected, container);
			}

			var result = new List<IDocHubFile>(collected.Count);
			foreach (var item in collected)
			{
				result.Add(new DriveItemAdapter
				{
					DriveItem = item,
				});
			}

			return result;
		}

		/// <summary>
		/// Resolves the folder referenced by <see cref="DocumentBucket.UploadPath"/> into (1) a
		/// KQL <c>site:</c> scoping clause applied server-side and (2) the set of driveItem ids
		/// (the bucket folder plus every folder under it) used to filter hits client-side to the
		/// bucket-folder subtree.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The Microsoft Search <c>path:</c> refiner matches driveItem hits by their
		/// display-form URL (<c>.../Forms/DispForm.aspx?ID=...</c>), not their storage URL,
		/// so it cannot be used for folder-level scoping. The response's
		/// <c>parentReference.path</c> is not populated for driveItem hits either, but
		/// <c>parentReference.id</c> is - hence the client-side filter over a pre-computed
		/// set of folder ids.
		/// </para>
		/// </remarks>
		private async Task ResolveBucketScopingAsync(SharePointSearchPageData context, DocumentBucket bucket)
		{
			var trimmedPath = (bucket?.UploadPath ?? string.Empty).Trim('/', '\\');

			var siteUrl = "https://" + _sharePoint.SiteURL.TrimEnd('/');
			context.ScopingClause = "site:\"" + siteUrl + "\"";

			if (string.IsNullOrEmpty(trimmedPath))
			{
				context.AllowedParentIds = null;
				return;
			}

			var folder = await _graphClient
				.Drives[_drive.Id]
				.Root
				.ItemWithPath(trimmedPath)
				.GetAsync();

			if (folder == null || folder.Folder == null)
				throw new InvalidOperationException("Could not find folder with path /" + (bucket?.UploadPath ?? string.Empty));

			var allowedIds = new HashSet<string>(StringComparer.Ordinal) { folder.Id };
			await CollectDescendantFolderIdsAsync(folder.Id, allowedIds);
			context.AllowedParentIds = allowedIds;
		}

		/// <summary>
		/// Recursively enumerates every folder id under <paramref name="folderId"/> and adds
		/// them to <paramref name="ids"/>.
		/// </summary>
		private async Task CollectDescendantFolderIdsAsync(string folderId, HashSet<string> ids)
		{
			var page = await _graphClient
				.Drives[_drive.Id]
				.Items[folderId]
				.Children
				.GetAsync(cfg => cfg.QueryParameters.Select = new[] { "id", "folder" });

			while (page != null)
			{
				if (page.Value != null)
				{
					foreach (var child in page.Value)
					{
						if (child.Folder != null && !string.IsNullOrEmpty(child.Id) && ids.Add(child.Id))
						{
							await CollectDescendantFolderIdsAsync(child.Id, ids);
						}
					}
				}

				if (string.IsNullOrEmpty(page.OdataNextLink))
					break;

				page = await _graphClient
					.Drives[_drive.Id]
					.Items[folderId]
					.Children
					.WithUrl(page.OdataNextLink)
					.GetAsync();
			}
		}

		/// <summary>
		/// Executes one Microsoft Search page request and advances the paging cursor.
		/// </summary>
		private async Task<SearchHitsContainer> ExecuteSearchPageRequest(
			SharePointSearchPageData context,
			WebFileSearchData args)
		{
			// Combine site scoping with the user's KQL query; the bucket-folder subtree filter
			// is applied client-side in CollectSearchFiles because Microsoft Search cannot
			// express "everything under this folder" reliably for driveItem hits.
			var fullQuery = "(" + context.ScopingClause + ") AND (" + args.Query + ")";

			var body = new Microsoft.Graph.Search.Query.QueryPostRequestBody
			{
				Requests = new List<SearchRequest>
				{
					new SearchRequest
					{
						EntityTypes = new List<EntityType?> { EntityType.DriveItem },
						Query = new SearchQuery { QueryString = fullQuery },
						From = context.From,
						Size = context.PageSize,
						Region = args.Region,
					},
				},
			};

			DebugLog($"Request: from={context.From} size={context.PageSize} region={args.Region ?? "<null>"} query=\"{fullQuery}\"");

			var response = await _graphClient.Search.Query.PostAsQueryPostResponseAsync(body);
			var container = response?.Value?.FirstOrDefault()?.HitsContainers?.FirstOrDefault();

			DebugLog($"Response: total={container?.Total?.ToString() ?? "<null>"} hits={container?.Hits?.Count ?? 0} moreResultsAvailable={container?.MoreResultsAvailable}");

			context.SearchStarted = true;
			context.MoreResultsAvailable = container?.MoreResultsAvailable == true;
			context.From += context.PageSize;

			return container;
		}

		/// <summary>
		/// Collects file items from a Microsoft Search hits container into the logical page and
		/// buffers overflow.
		/// </summary>
		private static void CollectSearchFiles(
			HashSet<string> allowedExtensions,
			SharePointSearchPageData context,
			ICollection<DriveItem> collected,
			SearchHitsContainer container)
		{
			if (container?.Hits == null)
				return;

			DebugLog($"CollectSearchFiles: allowedExtensions={(allowedExtensions == null ? "<null>" : string.Join(",", allowedExtensions))} allowedParentIds={context.AllowedParentIds?.Count ?? 0} hitCount={container.Hits.Count}");

			int hitIndex = 0;
			foreach (var h in container.Hits)
			{
				var asDriveItem = h.Resource as DriveItem;
				var name = asDriveItem?.Name;
				var parentId = asDriveItem?.ParentReference?.Id;
				var inScope = context.AllowedParentIds == null
					|| (parentId != null && context.AllowedParentIds.Contains(parentId));

				DebugLog(
					$"  hit[{hitIndex}] driveItemCast={asDriveItem != null} name={name ?? "<null>"} " +
					$"parentId={parentId ?? "<null>"} inScope={inScope} hitId={h.HitId}");
				hitIndex++;
			}

			var files = container.Hits
				.Select(h => h.Resource as DriveItem)
				.Where(i => i != null
							&& i.Folder == null
							&& !string.IsNullOrEmpty(i.Name)
							&& (context.AllowedParentIds == null
								|| (i.ParentReference?.Id != null
									&& context.AllowedParentIds.Contains(i.ParentReference.Id)))
							&& (allowedExtensions == null
								|| allowedExtensions.Contains(Path.GetExtension(i.Name).TrimStart('.'))))
				.ToList();

			DebugLog($"  -> {files.Count} hit(s) kept after filtering");

			foreach (var file in files)
			{
				if (collected.Count < context.PageSize)
				{
					collected.Add(file);
				}
				else
				{
					context.PageRemainderBuffer.Enqueue(file);
				}
			}
		}

		/// <summary>
		/// Appends a debug line to <c>C:\Skyline DataMiner\Logging\DocumentHub_Search.txt</c>.
		/// </summary>
		/// <remarks>
		/// Ad-hoc file logger for the SharePoint search path; the handler runs inside the
		/// DataMiner process where <c>Console.WriteLine</c> goes nowhere. Wrapped in a
		/// try/catch so a failing log write can never break the caller. Remove or gate behind
		/// a flag before shipping.
		/// </remarks>
		private static void DebugLog(string message)
		{
			try
			{
				const string logPath = @"C:\Skyline DataMiner\Logging\DocumentHub_Search.txt";
				var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)
					+ " " + message + Environment.NewLine;
				File.AppendAllText(logPath, line);
			}
			catch
			{
				// Never let logging break the caller.
			}
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
