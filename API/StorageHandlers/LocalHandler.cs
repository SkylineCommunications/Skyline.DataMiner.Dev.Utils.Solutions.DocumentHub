namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
    using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;
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
        private IEnumerator<FileInfo> _fileEnumerator;
        private string _currentRoot;

        /// <summary>
        /// Checks if a file exists at the given directory path.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="name">The filename to check.</param>
        /// <returns>True if the file exists; otherwise false.</returns>
        public bool FileExists(string directory, string name)
		{
			string filePath = SecurePath.ConstructSecurePath(directory, $"{name}");
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

        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            const int pageSize = 200;

            // Determine root path
            string root = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";

            if (category != null && !string.IsNullOrWhiteSpace(category.UploadPath))
            {
                root = Path.Combine(root, category.UploadPath.TrimStart('\\', '/'));
            }

            // Initialize enumeration only once or when root changes
            if (_fileEnumerator == null || !string.Equals(_currentRoot, root, StringComparison.OrdinalIgnoreCase))
            {
                _currentRoot = root;

                if (!Directory.Exists(root))
                    return new List<IDocHubFile>();

                var files = Directory
                    .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f));

                if (!string.IsNullOrEmpty(filter))
                {
                    files = files.Where(fi =>
                        fi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                _fileEnumerator = files
                    .OrderByDescending(fi => fi.CreationTimeUtc)
                    .GetEnumerator();
            }

            var result = new List<IDocHubFile>();

            while (result.Count < pageSize && _fileEnumerator.MoveNext())
            {
                result.Add(new FileInfoAdapter
                {
                    fileInfo = _fileEnumerator.Current
                });
            }

            return result;
        }
    }
}
