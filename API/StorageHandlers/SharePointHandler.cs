namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
    using Azure.Identity;
    using Microsoft.Graph;
    using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Drive = Microsoft.Graph.Drive;
    using File = System.IO.File;

    /// <summary>
    /// Handles uploading files and images to a SharePoint document library.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IStorageHandler"/> to provide SharePoint-specific storage operations.
    /// Responsible for connecting to SharePoint, checking file existence, creating folders,
    /// and uploading files or images.
    /// </remarks>
    internal class SharePointHandler : IStorageHandler
	{
		#region Globals

		/// <summary>
		/// Holds SharePoint instance configuration from the DOM.
		/// </summary>
		private readonly Models.SharePointConfiguration _sharePoint;

		/// <summary>
		/// Client used to access Microsoft Graph API.
		/// </summary>
		private readonly GraphServiceClient _graphClient;

		/// <summary>
		/// Represents the SharePoint site.
		/// </summary>
		private readonly Site _site;

		/// <summary>
		/// Represents the document library (drive) in SharePoint.
		/// </summary>
		private readonly Drive _drive;

        private readonly Queue<string> _folderQueue; // Queue to manage folder traversal in breadth-first order.
        private IDriveItemChildrenCollectionRequest _nextPageRequest; // Tracks the next page of Graph API results.

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SharePointHandler"/> class.
        /// </summary>
        /// <param name="helpers">The DOM helper for accessing SharePoint configuration.</param>
        /// <exception cref="Exception">Thrown if the SharePoint library cannot be found.</exception>
        internal SharePointHandler(DataHelpersDocumentHub helpers)
		{
			// Avoid re-initialization if client and drive are already set
			if (_graphClient != null && _drive != null)
				return;

			// Load SharePoint configuration
			_sharePoint = helpers.SharePointConfigurations.Read().FirstOrDefault();

            // Authenticate to Microsoft Graph using client credentials
            var credential = new ClientSecretCredential(
				_sharePoint.TenantID,
				_sharePoint.ClientID,
				_sharePoint.ClientSecret);

			_graphClient = new GraphServiceClient(credential);
            // Start traversal at root folder.
            _folderQueue = new Queue<string>(new[] { "root" });

            // Build site URI
            var uri = new UriBuilder("https://" + _sharePoint.SiteURL).Uri;
			string hostname = uri.Host;
			string path = uri.AbsolutePath;

			// Retrieve the SharePoint site object
			_site = _graphClient.Sites[$"{hostname}:{path}"]
				.Request()
				.GetAsync()
				.GetAwaiter()
				.GetResult();

			// Retrieve the document library (drive)
			var drives = _graphClient.Sites[_site.Id].Drives
				.Request()
				.GetAsync()
				.GetAwaiter()
				.GetResult();

			_drive = drives.FirstOrDefault(d => d.Name.Equals(_sharePoint.DocumentLibraryName, StringComparison.OrdinalIgnoreCase));
			if (_drive == null)
				throw new NullReferenceException($"Library '{_sharePoint.DocumentLibraryName}' not found.");
		}

        #endregion

        #region Public

		/// <summary>
		/// 
		/// </summary>
		/// <param name="category"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
			const int pageSize = 200;

            // Initialize traversal only once per handler instance
            if (_folderQueue.Count == 0 && _nextPageRequest == null)
            {
                string startFolder;

                if (category == null || string.IsNullOrWhiteSpace(category.UploadPath))
                {
                    startFolder = "root";
                }
                else
                {
                    startFolder = category.UploadPath;
                }

                _folderQueue.Enqueue(startFolder);
            }

            var driveItems = FetchNextPageInternal(pageSize, filter);

            var result = new List<IDocHubFile>();

            foreach (var item in driveItems)
            {
                result.Add(new DriveItemAdapter(_sharePoint)
                {
                    driveItem = item
                });
            }

            return result;
        }

        /// <summary>
        /// Checks synchronously if a file exists in the specified SharePoint directory.
        /// </summary>
        /// <returns>Whether the file exists or not.</returns>
        public bool FileExists(string directory, string name)
		{
			return FileExistsAsync(directory, name)
				.GetAwaiter()
				.GetResult();
		}

		/// <summary>
		/// Uploads a file from local disk to SharePoint.
		/// </summary>
		public void UploadFile(string filePath, string directory, string name)
		{
			UploadFileAsync(filePath, directory, name)
				.GetAwaiter() // TODO: Async uploads to be supported later.
				.GetResult();
		}

		/// <summary>
		/// Uploads an in-memory image to SharePoint as JPEG.
		/// </summary>
		public void UploadImage(Bitmap image, string directory, string name)
		{
			UploadImageAsync(image, directory, name)
				.GetAwaiter() // TODO: Async uploads to be supported later.
				.GetResult();
		}

        #endregion

        #region Private

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pageSize"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
        private IList<DriveItem> FetchNextPageInternal(int pageSize, string filter)
        {
            while (_folderQueue.Count > 0 || _nextPageRequest != null)
            {
                if (_nextPageRequest == null)
                {
                    var folderId = _folderQueue.Dequeue();

                    _nextPageRequest = _graphClient
                        .Drives[_drive.Id]
                        .Items[folderId]
                        .Children
                        .Request()
                        .Top(pageSize);
                }

                var page = _nextPageRequest
                    .GetAsync()
                    .GetAwaiter()
                    .GetResult();

                _nextPageRequest = page.NextPageRequest;

                foreach (var folder in page.CurrentPage.Where(i => i.Folder != null))
                    _folderQueue.Enqueue(folder.Id);

                var files = page.CurrentPage
                    .Where(i =>
                        i.File != null &&
                        (string.IsNullOrEmpty(filter) ||
                         i.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();

                return files;
            }

            return new List<DriveItem>();
        }

        /// <summary>
        /// Checks asynchronously if a file exists at the specified SharePoint path.
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
				// File not found
				if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
					return false;

				// Other errors are rethrown
				throw;
			}
		}

		/// <summary>
		/// Uploads a local file asynchronously to SharePoint, creating folders if needed.
		/// </summary>
		private async Task UploadFileAsync(string filepath, string directory, string name)
		{
			try
			{
				// Ensure target directory exists
				await EnsureFolderPathExistsAsync(directory);

				using (var stream = File.OpenRead(filepath))
				{
					var path = $"{directory.TrimEnd('/')}/{name}";

					// Upload file to SharePoint
					var item = await _graphClient
						.Sites[_site.Id]
						.Drives[_drive.Id]
						.Root
						.ItemWithPath(path)
						.Content
						.Request()
						.PutAsync<DriveItem>(stream);
				}
			}
			catch (Exception e)
			{
				throw new IOException(ExtractMessage(e));
			}
		}

		/// <summary>
		/// Uploads a <see cref="Bitmap"/> image asynchronously to SharePoint as JPEG.
		/// </summary>
		private async Task UploadImageAsync(Bitmap image, string directory, string name)
		{
			try
			{
				MemoryStream jpegStream = new MemoryStream();

				// Save image to memory stream
				image.Save(jpegStream, ImageFormat.Jpeg);
				jpegStream.Position = 0; // Reset stream for reading

				// Upload the image
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
		/// Ensures that a folder path exists in SharePoint; creates missing folders recursively.
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
					// Attempt to get the folder
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
					var parentPath = Path.GetDirectoryName(currentPath).Replace("\\", "/");
					var folderName = Path.GetFileName(currentPath);

					var folder = new DriveItem
					{
						Name = folderName,
						Folder = new Folder(),
					};

					if (string.IsNullOrEmpty(parentPath) || parentPath == ".")
					{
						// Create at root
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
						// Create under parent folder
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
		/// Extracts the innermost meaningful message from an exception.
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
