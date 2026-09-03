namespace Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers
{
	using System;
	using System.IO;

	/// <summary>
	/// Writes downloaded content to a temporary sibling file before atomically publishing it.
	/// </summary>
	internal static class AtomicFileDownloader
	{
		internal static void Write(string destinationPath, Action<string> writeTemporaryFile)
		{
			if (string.IsNullOrWhiteSpace(destinationPath))
			{
				throw new ArgumentNullException(nameof(destinationPath));
			}

			if (writeTemporaryFile == null)
			{
				throw new ArgumentNullException(nameof(writeTemporaryFile));
			}

			var fullDestinationPath = Path.GetFullPath(destinationPath);
			var destinationName = Path.GetFileName(fullDestinationPath);
			if (string.IsNullOrEmpty(destinationName))
			{
				throw new ArgumentException("The destination path must include a file name.", nameof(destinationPath));
			}

			var destinationDirectory = Path.GetDirectoryName(fullDestinationPath);
			if (string.IsNullOrEmpty(destinationDirectory) || !Directory.Exists(destinationDirectory))
			{
				throw new DirectoryNotFoundException($"The destination directory '{destinationDirectory}' does not exist.");
			}

			var temporaryPath = Path.Combine(
				destinationDirectory,
				$".{destinationName}.{Guid.NewGuid():N}.download");

			try
			{
				writeTemporaryFile(temporaryPath);

				if (!File.Exists(temporaryPath))
					throw new IOException("The download did not produce a temporary file.");

				if (File.Exists(fullDestinationPath))
				{
					File.Replace(temporaryPath, fullDestinationPath, null);
				}
				else
				{
					File.Move(temporaryPath, fullDestinationPath);
				}
			}
			finally
			{
				if (File.Exists(temporaryPath))
				{
					File.Delete(temporaryPath);
				}
			}
		}
	}
}
