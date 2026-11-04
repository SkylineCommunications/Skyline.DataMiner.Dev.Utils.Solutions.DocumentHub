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
		/// Root path for publicly accessible local DataMiner files.
		/// </summary>
		internal const string PublicRoot = WebpagesRoot + @"\Public";

		private readonly string publicRoot;

		/// <summary>
		/// Root path for the DataMiner Webpages folder, used to compute web-resolvable paths.
		/// </summary>
		internal const string WebpagesRoot = @"C:\Skyline DataMiner\Webpages";

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalHandler"/> class.
		/// </summary>
		public LocalHandler()
			: this(PublicRoot)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="LocalHandler"/> class with an isolated storage root.</summary>
		/// <param name="publicRoot">The Public directory used for file operations.</param>
		internal LocalHandler(string publicRoot)
		{
			this.publicRoot = Path.GetFullPath(publicRoot).TrimEnd('\\', '/');
		}

		/// <summary>
		/// Downloads a local DocumentHub file to the specified destination.
		/// </summary>
		/// <param name="file">The local file to download.</param>
		/// <param name="destinationPath">The full local destination path.</param>
		public void DownloadFile(IDocHubFile file, string destinationPath)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!(file is FileInfoAdapter))
				throw new ArgumentException("LocalHandler requires a local DocumentHub file.", nameof(file));

			var sourcePath = ResolveContainedPath(this.publicRoot, file.GetFilePath());
			if (string.IsNullOrWhiteSpace(sourcePath))
				throw new ArgumentException("The local file does not contain a source path.", nameof(file));
			if (!File.Exists(sourcePath))
				throw new FileNotFoundException($"Source file not found: '{sourcePath}'", sourcePath);

			AtomicFileDownloader.Write(
				destinationPath,
				temporaryPath => File.Copy(sourcePath, temporaryPath, overwrite: false));
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

			string filePath = ResolveContainedPath(ResolveLocalDirectory(args.Directory, this.publicRoot), args.Name);
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
			directory = ResolveLocalDirectory(directory, this.publicRoot);
			// Ensure the target directory exists
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// Construct the full file path and save the image as JPEG
			string filePath = ResolveContainedPath(directory, $"{name}.jpeg");
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
			var targetDirectory = ResolveLocalDirectory(bucket.UploadPath, this.publicRoot);

			// Ensure the target directory exists
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			// Combine directory and target filename
			string targetPath = ResolveContainedPath(targetDirectory, name);

			// Copy the file to the target location (overwrite if exists)
			File.Copy(filePath, targetPath, overwrite: true);

			// Return the web-resolvable path of the uploaded file.
			return '/' + FileInfoAdapter.GetRelativePath(targetPath, Path.GetDirectoryName(this.publicRoot));
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

			var directory = ResolveLocalDirectory(data.Bucket?.UploadPath, this.publicRoot);
			var filePath = ResolveContainedPath(directory, data.Name);

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
		/// Asynchronous version of <see cref="ReadFiles(ReadData)"/>.
		/// </summary>
		/// <param name="data">The storage handler data containing bucket and filter information.</param>
		/// <returns>A list of all <see cref="IDocHubFile"/> matching the criteria.</returns>
		public async Task<List<IDocHubFile>> ReadFilesAsync(ReadData data)
		{
			return await Task.FromResult(ReadFiles(data));
		}

#pragma warning disable SLC_SC0002 // Canonicalized paths are checked against the storage root after combination.
		/// <summary>
		/// Resolves an upload path to a directory confined to <see cref="PublicRoot"/>.
		/// </summary>
		/// <param name="relativePath">A relative path (may be null, empty, or have leading slashes).</param>
		/// <returns>The full local directory path.</returns>
		internal static string ResolveLocalDirectory(string relativePath)
		{
			return ResolveLocalDirectory(relativePath, PublicRoot);
		}

		private static string ResolveLocalDirectory(string relativePath, string root)
		{
			var directory = (relativePath ?? string.Empty).Replace('/', '\\');
			if (directory.StartsWith(@"\\", StringComparison.Ordinal))
				throw new ArgumentException("Network and device paths are not supported.", nameof(relativePath));

			// A single leading slash is the existing bucket notation for a Public-relative path.
			if (directory.StartsWith(@"\", StringComparison.Ordinal))
				directory = directory.Substring(1);

			return ResolveContainedPath(root, directory);
		}

		private static string ResolveContainedPath(string root, string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			path = path.Replace('/', '\\');
			if (path.StartsWith(@"\\", StringComparison.Ordinal)
				|| (path.IndexOf(':') >= 0 && (!Path.IsPathRooted(path) || path.Length < 3 || path[2] != '\\'))
				|| path.IndexOf(':', Math.Min(2, path.Length)) >= 0)
				throw new ArgumentException("Network, device, and alternate stream paths are not supported.", nameof(path));

			var fullPath = Path.GetFullPath(Path.Combine(root, path));
			if (!fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)
				&& !fullPath.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("Local file paths must remain inside the storage root.", nameof(path));

			// Do not follow junctions or symbolic links that could redirect access outside Public.
			for (var current = fullPath; current != null; current = Path.GetDirectoryName(current))
			{
				if ((File.Exists(current) || Directory.Exists(current))
					&& (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
					throw new IOException("Local storage does not support reparse points.");
			}

			return fullPath;
		}

		private static IEnumerable<FileInfo> EnumerateFiles(string root)
		{
			foreach (var file in Directory.EnumerateFiles(root))
				yield return new FileInfo(ResolveContainedPath(root, file));

			foreach (var directory in Directory.EnumerateDirectories(root))
			{
				foreach (var file in EnumerateFiles(ResolveContainedPath(root, directory)))
					yield return file;
			}
		}
#pragma warning restore SLC_SC0002

		/// <summary>Reads one filtered page of files within the bucket's directory.</summary>
		/// <param name="data">The bucket, filter, and paging context.</param>
		/// <returns>The next page of local files.</returns>
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
			if (localContext.FileEnumerator == null)
			{
				localContext.CurrentRoot = ResolveLocalDirectory(bucket?.UploadPath, this.publicRoot);
			}

			// Initialize file enumeration if not already created
			if (localContext.FileEnumerator == null)
			{
				// Return empty list if root directory does not exist
				if (!Directory.Exists(localContext.CurrentRoot))
					return new List<IDocHubFile>();

				// Enumerate all files recursively
				var files = EnumerateFiles(localContext.CurrentRoot);

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
				   localContext.TryReadNext(out var file))
			{
				result.Add(new FileInfoAdapter
				{
					fileInfo = file,
					publicRoot = this.publicRoot,
				});
			}

			return result;
		}
	}
}
