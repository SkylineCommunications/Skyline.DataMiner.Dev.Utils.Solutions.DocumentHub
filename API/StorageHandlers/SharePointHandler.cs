namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using Azure.Identity;
	using Microsoft.Graph;
	using Microsoft.IdentityModel.Tokens;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.SLDataGateway.Management.Recommendations;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.Paging;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.Security;
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
	/// var files = handler.ReadFiles(new WebFileReadData { Category = category });
	/// </code>
	/// </example>
	internal class SharePointHandler : IStorageHandler
	{
		#region Globals

		/// <summary>
		/// SharePoint configuration retrieved from the Document Hub configuration model.
		/// </summary>
		private readonly Models.Sources.SharePointConfiguration _sharePoint;

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

		/// <summary>
		/// DataMiner connection instance.
		/// </summary>
		private readonly IConnection _connection;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="SharePointHandler"/> class.
		/// </summary>
		/// <param name="helpers">Helper object to retrieve configuration from DOM.</param>
		/// <param name="connection">DataMiner connection instance.</param>
		/// <exception cref="NullReferenceException">
		/// Thrown when the configured SharePoint document library cannot be found.
		/// </exception>
		internal SharePointHandler(DataHelpersDocumentHub helpers, IConnection connection)
		{
			// Store DataMiner connection reference
			_connection = connection;

			// Defensive check to avoid reinitialization
			if (_graphClient != null && _drive != null)
				return;

			// Retrieve SharePoint configuration from DOM
			_sharePoint = helpers.SharePointConfigurations.Read().FirstOrDefault();

			// Retrieve client secret
			var clientSecret = RetrieveClientSecret();

			// Authenticate using Azure AD client credentials flow
			var credential = new ClientSecretCredential(
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
					break;
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

			var category = args.Category;
			var filePath = args.FilePath;
			var name = args.Name;

			return UploadFileAsync(category.UploadPath, filePath, name)
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

			var category = args.Category;
			var filter = args.Filter;

			// Initialize category root exactly once (replace initial "root" sentinel)
			if (category != null
				&& spContext.FolderQueue.Count == 1
				&& spContext.FolderQueue.Peek() == "root"
				&& spContext.NextPageRequest == null)
			{
				var folder = _graphClient
					.Sites[_site.Id]
					.Drives[_drive.Id]
					.Root
					.ItemWithPath(category.UploadPath)
					.Request()
					.GetAsync()
					.GetAwaiter()
					.GetResult();

				if (folder == null || folder.Folder == null)
					throw new InvalidOperationException("Could not find folder with path /" + category.UploadPath);

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

			// Fetch next logical page (Graph paging + remainder buffer)
			var driveItems = FetchNextPageInternal(filter, spContext);

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
		/// Retrieves the next logical page of DriveItems while recursively traversing folders.
		/// </summary>
		/// <remarks>
		/// ⚠ Paging Pitfall:
		/// Microsoft Graph returns pages per folder. When combining folders into a global page,
		/// partially consumed Graph pages MUST be buffered or files will be skipped.
		/// </remarks>
		private IList<DriveItem> FetchNextPageInternal(string filter, SharePointPageData context)
		{
			var collected = new List<DriveItem>(context.PageSize);

			// Drain buffered items from previous partial Graph page
			while (collected.Count < context.PageSize && context.PageRemainderBuffer.Count > 0)
			{
				collected.Add(context.PageRemainderBuffer.Dequeue());
			}

			// Continue traversal until page is full or no more data exists
			while (collected.Count < context.PageSize &&
				   (context.FolderQueue.Count > 0 || context.NextPageRequest != null))
			{
				// Start paging a new folder if required
				if (context.NextPageRequest == null)
				{
					var folderId = context.FolderQueue.Dequeue();

					context.NextPageRequest = _graphClient
						.Drives[_drive.Id]
						.Items[folderId]
						.Children
						.Request()
						.Top(context.PageSize);
				}

				// Execute Graph request
				var page = context.NextPageRequest.GetAsync().GetAwaiter().GetResult();
				context.NextPageRequest = page.NextPageRequest;

				// Queue discovered subfolders
				foreach (var folder in page.CurrentPage.Where(i => i.Folder != null))
					context.FolderQueue.Enqueue(folder.Id);

				// Filter only file items
				var files = page.CurrentPage
					.Where(i => i.File != null &&
								(string.IsNullOrEmpty(filter) ||
								 i.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
					.ToList();

				// Fill logical page and buffer remaining items
				foreach (var file in files)
				{
					if (collected.Count < context.PageSize)
						collected.Add(file);
					else
						context.PageRemainderBuffer.Enqueue(file);
				}
			}

			return collected;
		}

		/// <summary>
		/// Checks if a file exists asynchronously.
		/// </summary>
		private async Task<bool> FileExistsAsync(string directory, string name)
		{
			try
			{
				var driveItem = await _graphClient
					.Sites[_site.Id]
					.Drives[_drive.Id]
					.Root
					.ItemWithPath($"{directory}/{name}")
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
				// Ensure folder structure exists
				await EnsureFolderPathExistsAsync(directory);

				using (var stream = File.OpenRead(filepath))
				{
					var path = $"{directory.TrimEnd('/')}/{name}";

					var item = await _graphClient
						.Sites[_site.Id]
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(path)
						.Content
						.Request()
						.PutAsync<DriveItem>(stream);

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
				MemoryStream jpegStream = new MemoryStream();

				// Serialize image to memory
				image.Save(jpegStream, ImageFormat.Jpeg);
				jpegStream.Position = 0;

				var item = await _graphClient
					.Sites[_site.Id]
					.Drives[_drive.Id]
					.Root
					.ItemWithPath($"{name}.jpeg")
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
			var segments = directory.Trim('/').Split('/');

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
