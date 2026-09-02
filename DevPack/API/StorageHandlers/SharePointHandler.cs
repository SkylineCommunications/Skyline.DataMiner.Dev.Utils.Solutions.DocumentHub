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
	using Drive = Microsoft.Graph.Drive;
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
				.Request()
				.GetAsync()
				.GetAwaiter()
				.GetResult();

			// Retrieve document libraries (drives)
			var drives = _graphClient.Sites[_site.Id].Drives
				.Request()
				.GetAsync()
				.GetAwaiter()
				.GetResult();

			// Select configured document library
			_drive = drives.FirstOrDefault(d => d.Name.Equals(_sharePoint.DocumentLibraryName, StringComparison.OrdinalIgnoreCase));
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
		public List<IDocHubFile> ReadFiles(ReadData data)
		{
			if (!(data is WebFileReadData args))
				throw new ArgumentException("SharePointHandler requires WebFileReadData.", nameof(data));

			// If paging context exists, return next page only
			if (args.Context != null)
				return ReadPage(args);

			// Initialize paging context
			var context = new SharePointPageData();
			args.Context = context;

			var files = new List<IDocHubFile>();

			// Iterate until no more data is available
			while (context.HasNextPage())
			{
				var page = ReadPage(args);

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

		private Stream OpenDownloadStream(DriveItem item)
		{
			return _graphClient
				.Drives[_drive.Id]
				.Items[item.Id]
				.Content
				.Request()
				.GetAsync()
				.GetAwaiter()
				.GetResult();
		}

		/// <summary>
		/// Reads a single logical page of SharePoint files.
		/// </summary>
		private List<IDocHubFile> ReadPage(ReadData data)
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
				&& spContext.NextPageRequest == null)
			{
				var trimmedPath = (bucket.UploadPath ?? string.Empty).Trim('/', '\\');

				DriveItem folder;
				if (string.IsNullOrEmpty(trimmedPath))
				{
					folder = _graphClient
						.Sites[_site.Id]
						.Drives[_drive.Id]
						.Root
						.Request()
						.GetAsync()
						.GetAwaiter()
						.GetResult();
				}
				else
				{
					folder = _graphClient
						.Sites[_site.Id]
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(trimmedPath)
						.Request()
						.GetAsync()
						.GetAwaiter()
						.GetResult();
				}

				if (folder == null || folder.Folder == null)
					throw new InvalidOperationException("Could not find folder with path /" + bucket.UploadPath);

				// Do NOT replace the queue instance (other code may hold references)
				spContext.FolderQueue.Clear();
				spContext.FolderQueue.Enqueue(folder.Id);
			}

			// End-of-traversal: no folders, no Graph pages, no buffered items
			if (spContext.FolderQueue.Count == 0
				&& spContext.NextPageRequest == null
				&& spContext.PageRemainderBuffer.Count == 0)
			{
				return new List<IDocHubFile>();
			}

			// Parse allowed extensions from bucket
			var allowedExtensions = bucket != null && !string.IsNullOrEmpty(bucket.Extensions)
										? new HashSet<string>(bucket.Extensions.Split(','), StringComparer.OrdinalIgnoreCase)
										: null;

			// Fetch next logical page (Graph paging + remainder buffer)
			var driveItems = FetchNextPageInternal(filter, allowedExtensions, spContext);

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
		private IList<DriveItem> FetchNextPageInternal(string filter, HashSet<string> allowedExtensions, SharePointPageData context)
		{
			var collected = new List<DriveItem>(context.PageSize);

			// Drain buffered items from previous partial Graph pages
			DrainRemainderBuffer(context, collected);

			// Continue folder traversal until logical page is full or no data remains
			while (ShouldContinuePaging(context, collected))
			{
				EnsureNextPageRequest(context);

				var page = ExecuteGraphPageRequest(context);

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
				   (context.FolderQueue.Count > 0 || context.NextPageRequest != null);
		}

		/// <summary>
		/// Ensures a Graph paging request exists for the current folder.
		/// </summary>
		private void EnsureNextPageRequest(SharePointPageData context)
		{
			if (context.NextPageRequest != null)
				return;

			var folderId = context.FolderQueue.Dequeue();

			context.NextPageRequest = _graphClient
				.Drives[_drive.Id]
				.Items[folderId]
				.Children
				.Request()
				.Top(context.PageSize);
		}

		/// <summary>
		/// Executes the current Graph page request and updates the continuation token.
		/// </summary>
		private IDriveItemChildrenCollectionPage ExecuteGraphPageRequest(SharePointPageData context)
		{
			var page = context.NextPageRequest.GetAsync().GetAwaiter().GetResult();
			context.NextPageRequest = page.NextPageRequest;
			return page;
		}

		/// <summary>
		/// Enqueues subfolders discovered in the current Graph page.
		/// </summary>
		private static void EnqueueSubFolders(SharePointPageData context, IDriveItemChildrenCollectionPage page)
		{
			foreach (var folder in page.CurrentPage.Where(i => i.Folder != null))
			{
				context.FolderQueue.Enqueue(folder.Id);
			}
		}

		/// <summary>
		/// Collects file items into the logical page and buffers overflow items.
		/// </summary>
		private static void CollectFiles(
			string filter,
			HashSet<string> allowedExtensions,
			SharePointPageData context,
			ICollection<DriveItem> collected,
			IDriveItemChildrenCollectionPage page)
		{
			var files = page.CurrentPage
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
				// Normalize directory path
				var normalizedDirectory = NormalizeDirectoryPath(directory);

				string path;
				if (string.IsNullOrEmpty(normalizedDirectory))
				{
					// File in root directory
					path = name;
				}
				else
				{
					// File in subdirectory
					path = $"{normalizedDirectory}/{name}";
				}

				var driveItem = await _graphClient
					.Sites[_site.Id]
					.Drives[_drive.Id]
					.Root
					.ItemWithPath(path)
					.Request()
					.GetAsync();

				return true;
			}
			catch (ServiceException ex)
			{
				if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
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
							.Sites[_site.Id]
							.Drives[_drive.Id]
							.Root
							.ItemWithPath(name)
							.Content
							.Request()
							.PutAsync<DriveItem>(stream);
					}
					else
					{
						// Upload to subdirectory
						var path = $"{normalizedDirectory}/{name}";
						item = await _graphClient
							.Sites[_site.Id]
							.Drives[_drive.Id]
							.Root
							.ItemWithPath(path)
							.Content
							.Request()
							.PutAsync<DriveItem>(stream);
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
					.Sites[_site.Id]
					.Drives[_drive.Id]
					.Root
					.ItemWithPath(path)
					.Content
					.Request()
					.PutAsync<DriveItem>(jpegStream);
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
						.Sites[_site.Id]
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(currentPath)
						.Request()
						.GetAsync();
				}
				catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
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
							.Sites[_site.Id]
							.Drives[_drive.Id]
							.Root
							.Children
							.Request()
							.AddAsync(folder);
					}
					else
					{
						await _graphClient
							.Sites[_site.Id]
							.Drives[_drive.Id]
							.Root
							.ItemWithPath(parentPath)
							.Children
							.Request()
							.AddAsync(folder);
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