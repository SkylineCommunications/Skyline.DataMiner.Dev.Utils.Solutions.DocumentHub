namespace Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;

	/// <summary>
	/// Handles file and image storage on the local filesystem.
	/// </summary>
	/// <remarks>
	/// Implements <see cref="IStorageHandler"/> and <see cref="IDeletableStorageHandler"/> to provide
	/// local storage operations including upload, read, and delete.
	/// Used for storing files and images under the DataMiner Webpages folder.
	/// </remarks>
	internal class LocalHandler : IStorageHandler, IDeletableStorageHandler
	{
		/// <summary>
		/// Root path for the DataMiner WebFileManager local storage.
		/// </summary>
		internal const string WebFileManagerRoot = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";

		/// <summary>
		/// Root path for the DataMiner Webpages folder, used to compute web-resolvable paths.
		/// </summary>
		internal const string WebpagesRoot = @"C:\Skyline DataMiner\Webpages";

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalHandler"/> class.
		/// </summary>
		public LocalHandler()
		{
		}

		/// <summary>
		/// Resolves a relative upload path to the full local directory under <see cref="WebFileManagerRoot"/>.
		/// </summary>
		/// <param name="relativePath">A relative path (may be null, empty, or have leading slashes).</param>
		/// <returns>The full local directory path.</returns>
		internal static string ResolveLocalDirectory(string relativePath)
		{
			var directory = relativePath ?? string.Empty;
			directory = directory.TrimStart('/', '\\');
			return string.IsNullOrEmpty(directory) ? WebFileManagerRoot : Path.Combine(WebFileManagerRoot, directory);
		}

		/// <summary>
		/// Checks if a file exists at the given directory path.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing directory and filename.
		/// </param>
		/// <returns>
		/// True if the file exists; otherwise false.
		/// </returns>
		public bool FileExists(FileExistsData data)
		{
			if (!(data is WebFileExistsData args))
				throw new ArgumentException("LocalHandler requires WebFileFileExistsData.", nameof(data));

			string filePath = Path.Combine(ResolveLocalDirectory(args.Directory), args.Name);
			return File.Exists(filePath);
		}

		/// <summary>
		/// Saves a <see cref="Bitmap"/> image to the specified directory as a JPEG file.
		/// </summary>
		/// <param name="image">The image to save.</param>
		/// <param name="directory">The target directory path.</param>
		/// <param name="name">The desired filename (without extension).</param>
		public void UploadImage(Bitmap image, string directory, string name)
		{
			// Ensure the target directory exists
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// Construct the full file path and save the image as JPEG
			string filePath = SecurePath.ConstructSecurePath(directory, $"{name}.jpeg");
			image.Save(filePath);
		}

		/// <summary>
		/// Copies a local file to the DataMiner Webpages folder under the specified relative path.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing file and target information.
		/// </param>
		/// <returns>The web-resolvable path of the uploaded file.</returns>
		public string UploadFile(UploadData data)
		{
			// Validate input data
			if (!(data is WebFileUploadData args))
				throw new ArgumentException("LocalHandler requires WebFileUploadData.", nameof(data));

			// Extract parameters from data
			var bucket = args.Bucket;
			var filePath = args.FilePath;
			var name = args.Name;

			// Validate that the source file exists
			if (!File.Exists(filePath))
				throw new FileNotFoundException($"Source file not found: '{filePath}'", filePath);

			// Resolve the full target directory from the bucket's relative upload path
			var targetDirectory = ResolveLocalDirectory(bucket.UploadPath);

			// Ensure the target directory exists
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			// Combine directory and target filename
			string targetPath = SecurePath.ConstructSecurePath(targetDirectory, name);

			// Copy the file to the target location (overwrite if exists)
			File.Copy(filePath, targetPath, overwrite: true);

			// Return the web-resolvable path of the uploaded file.
			return '/' + FileInfoAdapter.GetRelativePath(targetPath, WebpagesRoot);
		}

		/// <summary>
		/// Deletes a file from local DataMiner storage.
		/// </summary>
		/// <param name="data">
		/// The delete data containing the bucket and file name.
		/// </param>
		/// <exception cref="FileNotFoundException">
		/// Thrown when the file does not exist at the expected path.
		/// </exception>
		public void DeleteFile(DeleteData data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (string.IsNullOrWhiteSpace(data.Name))
				throw new ArgumentException("File name cannot be null or empty.", nameof(data));

			var directory = ResolveLocalDirectory(data.Bucket?.UploadPath);
			var filePath = Path.Combine(directory, data.Name);

			if (!File.Exists(filePath))
				throw new FileNotFoundException($"The file '{data.Name}' was not found.", filePath);

			File.Delete(filePath);
		}

		/// <summary>
		/// Reads all files for the given bucket and filter by iterating
		/// through all available pages until no more results are returned.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing bucket and filter information.
		/// </param>
		/// <returns>
		/// A list of all <see cref="IDocHubFile"/> matching the criteria.
		/// </returns>
		public List<IDocHubFile> ReadFiles(ReadData data)
		{
			// Validate input data
			if (!(data is WebFileReadData args))
				throw new ArgumentException("LocalHandler requires WebFileReadData.", nameof(data));

			// If a paging context is provided, read a single page
			if (data.Context != null)
				return ReadPage(data);

			// Create a new paging context for full enumeration
			args.Context = new LocalPageData();

			// Accumulates all files across pages
			List<IDocHubFile> files = new List<IDocHubFile>();

			// Continue reading pages until an empty page is returned
			while (true)
			{
				var page = ReadPage(args);

				// No more files available
				if (page.Count == 0)
					break;

				files.AddRange(page);
			}

			return files;
		}

		/// <summary>
		/// Reads files from the local file system using a paged enumerator.
		/// Supports optional bucket-based paths and filename filtering.
		/// </summary>
		/// <param name="data">"
		/// The storage handler data containing bucket, filter, and context.
		/// </param>
		/// <returns>
		/// A list of <see cref="IDocHubFile"/> representing the files in the current page.
		/// </returns>
		private List<IDocHubFile> ReadPage(ReadData data)
		{
			// Validate input data
			if (!(data is WebFileReadData args))
				throw new ArgumentException("LocalHandler requires WebFileReadData.", nameof(data));

			// Extract parameters from data
			var bucket = args.Bucket;
			var filter = args.Filter;

			// Validate context
			var context = args.Context ?? throw new ArgumentNullException(nameof(data));

			// Ensure the context is local
			if (!(context is LocalPageData localContext))
				throw new ArgumentException("LocalHandler requires LocalPageContext.", nameof(context));

			// Apply bucket upload path before enumeration starts
			if (bucket != null && localContext.FileEnumerator == null)
			{
				localContext.CurrentRoot = ResolveLocalDirectory(bucket.UploadPath);
			}

			// Initialize file enumeration if not already created
			if (localContext.FileEnumerator == null)
			{
				// Return empty list if root directory does not exist
				if (!Directory.Exists(localContext.CurrentRoot))
					return new List<IDocHubFile>();

				// Enumerate all files recursively
				var files = Directory
					.EnumerateFiles(localContext.CurrentRoot, "*.*", SearchOption.AllDirectories)
					.Select(f => new FileInfo(f));

				// Apply allowed extensions filter from bucket
				if (bucket != null && !string.IsNullOrEmpty(bucket.Extensions))
				{
					var allowedExtensions = new HashSet<string>(
						bucket.Extensions.Split(','),
						StringComparer.OrdinalIgnoreCase);

					files = files.Where(fi =>
						allowedExtensions.Contains(fi.Extension.TrimStart('.')));
				}

				// Apply optional filename filter
				if (!string.IsNullOrEmpty(filter))
				{
					files = files.Where(fi =>
						fi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
				}

				// Sort newest first and store enumerator in context
				localContext.FileEnumerator = files
					.OrderByDescending(fi => fi.CreationTimeUtc)
					.GetEnumerator();
			}

			// Collect the next page of results
			var result = new List<IDocHubFile>();

			while (result.Count < localContext.PageSize &&
				   localContext.FileEnumerator.MoveNext())
			{
				result.Add(new FileInfoAdapter
				{
					fileInfo = localContext.FileEnumerator.Current,
				});
			}

			return result;
		}
	}
}