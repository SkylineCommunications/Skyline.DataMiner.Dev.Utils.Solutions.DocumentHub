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
		public void UploadFile(string filePath, string directory, string name)
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
		}

        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter, PageContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!(context is LocalPageContext localContext))
                throw new ArgumentException(
                    "LocalHandler requires LocalPageContext.",
                    nameof(context));

            if (category != null && localContext.FileEnumerator == null)
            {
                localContext.CurrentRoot = Path.Combine(localContext.CurrentRoot, category.UploadPath.TrimStart('\\', '/'));
            }

            // Initialize enumeration only once or when root changes
            if (localContext.FileEnumerator == null)
            {

                if (!Directory.Exists(localContext.CurrentRoot))
                    return new List<IDocHubFile>();

                var files = Directory
                    .EnumerateFiles(localContext.CurrentRoot, "*.*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f));

                if (!string.IsNullOrEmpty(filter))
                {
                    files = files.Where(fi =>
                        fi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                localContext.FileEnumerator = files
                    .OrderByDescending(fi => fi.CreationTimeUtc)
                    .GetEnumerator();
            }

            var result = new List<IDocHubFile>();

            while (result.Count < localContext.PageSize && localContext.FileEnumerator.MoveNext())
            {
                result.Add(new FileInfoAdapter
                {
                    fileInfo = localContext.FileEnumerator.Current,
                });
            }

            return result;
        }

        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            List<IDocHubFile> files = new List<IDocHubFile>();

            var context = new LocalPageContext();
            while (true)
            {
                var page = ReadFiles(category, filter, context);

                if (page.Count == 0)
                    break;

                files.AddRange(page);
            }

            return files;
        }
    }
}
