namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
    using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Handles file and image storage on the local filesystem.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IStorageHandler"/> to provide local storage operations.
    /// Used for storing files and images under the DataMiner Webpages folder.
    /// </remarks>
	internal class LocalHandler : IStorageHandler
	{

        /// <summary>
        /// Checks if a file exists at the given directory path.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="name">The filename to check.</param>
        /// <returns>True if the file exists; otherwise false.</returns>
        public bool FileExists(string directory, string name)
		{
			string filePath = Path.Combine(directory, $"{name}");
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
			string filePath = Path.Combine(directory, $"{name}.jpeg");
			image.Save(filePath);
		}

		/// <summary>
		/// Copies a local file to the DataMiner Webpages folder under the specified relative path.
		/// </summary>
		/// <param name="filePath">The full path to the source file.</param>
		/// <param name="directory">The relative path under the WebFileManager folder.</param>
		/// <param name="name">The target filename including extension.</param>
		public string UploadFile(string filePath, string directory, string name)
		{
			// Root path for DataMiner WebFileManager
			var root = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";

			// Remove leading slashes from relative path
			directory = directory.TrimStart('/', '\\');

			// Combine root and relative path to get full target directory
			var targetDirectory = Path.Combine(root, directory);

			// Ensure the target directory exists
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			// Combine directory and target filename
			string targetPath = Path.Combine(targetDirectory, name);

			// Copy the file to the target location (overwrite if exists)
			File.Copy(filePath, targetPath, overwrite: true);

            // Return the web-resolvable path of the uploaded file.
            return '/' + FileInfoAdapter.GetRelativePath(targetPath, @"C:\Skyline DataMiner\Webpages");
        }

        /// <summary>
        /// Reads files from the local file system using a paged enumerator.
        /// Supports optional category-based paths and filename filtering.
        /// </summary>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter, DocHubPageData context)
        {
            // Validate context
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Ensure the context is local
            if (!(context is LocalPageData localContext))
                throw new ArgumentException(
                    "LocalHandler requires LocalPageContext.",
                    nameof(context));

            // Apply category upload path before enumeration starts
            if (category != null && localContext.FileEnumerator == null)
            {
                localContext.CurrentRoot = Path.Combine(
                    localContext.CurrentRoot,
                    category.UploadPath.TrimStart('\\', '/'));
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

        /// <summary>
        /// Reads all files for the given category and filter by iterating
        /// through all available pages until no more results are returned.
        /// </summary>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            // Accumulates all files across pages
            List<IDocHubFile> files = new List<IDocHubFile>();

            // Create a new paging context for full enumeration
            var context = new LocalPageData();

            // Continue reading pages until an empty page is returned
            while (true)
            {
                var page = ReadFiles(category, filter, context);

                // No more files available
                if (page.Count == 0)
                    break;

                files.AddRange(page);
            }

            return files;
        }
    }
}
