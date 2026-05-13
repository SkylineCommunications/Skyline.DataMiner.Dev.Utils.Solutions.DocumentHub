namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Before passing the file path to the correct <see cref="IStorageHandler"/>, files get validated by the <see cref="FileValidator"/>.
	/// This class checks if the file path is valid and does not contain anything which could lead to security vulnerabilities.
	/// </summary>
	internal class FileValidator
	{
		// blocking executables.
		private readonly IReadOnlyCollection<string> blacklistExtensions = new List<string>
		{
			"exe",
			"dll",
			"bat",
			"sh",
		};

		private readonly DocumentHubApiHelper apiHelper;

		internal FileValidator(IConnection connection)
		{
			apiHelper = new DocumentHubApiHelper(connection);
		}

		internal static void ValidateBaseParameters(DocumentBucket bucket, string filePath)
		{
			if (bucket == null)
				throw new ArgumentNullException(nameof(bucket));
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));
		}

		internal UploadData ValidateAndSanitizeFile(DocumentBucket bucket, string filePath, string name = null)
		{
			ValidateBaseParameters(bucket, filePath);
			string sanitizedFilePath = SanitizeFilePath(filePath);
			string sanitizedName = SanitizeFileName(sanitizedFilePath, name);
			if (!ValidateFileType(sanitizedFilePath))
			{
				throw new SecurityException($"The file '{sanitizedName}' is of type which is not allowed for upload through DocumentHub.");
			}

			var storedBucket = TryGetStoredBucket(bucket);

			// Get extension from the source file
			string extension = Path.GetExtension(sanitizedFilePath);

			return new UploadData
			{
				Bucket = storedBucket,
				FilePath = sanitizedFilePath,
				Name = $"{sanitizedName}{extension}",
			};
		}

		private string SanitizeFilePath(string filePath)
		{
			// Trim any whitespace and normalize to absolute path
			string fullFilePath = Path.GetFullPath(filePath.Trim());

			// Validate that the file exists
			if (!File.Exists(fullFilePath))
				throw new FileNotFoundException($"Source file not found: '{fullFilePath}'", filePath);

			return filePath;
		}

		/// <summary>
		/// Validates if the file type is allowed based on its extension. List of disallowed extensions is defined in the <see cref="blacklistExtensions"/> collection.
		/// </summary>
		/// <param name="filePath">Sanitized file path.</param>
		/// <returns><see langword="true"/> if the extension is allowed to be uploaded, <see langword="false"/> otherwise.</returns>
		private bool ValidateFileType(string filePath)
		{
			// We assume that the file path is already sanitized at this point, so we can safely extract the extension
			string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

			return !blacklistExtensions.Contains(extension);
		}

		private string SanitizeFileName(string filePath, string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				name = Path.GetFileNameWithoutExtension(filePath);
			}
			else
			{
				// Sanitize name: extract just the filename without any directory parts
				name = Path.GetFileName(name);
				name = Path.GetFileNameWithoutExtension(name);
			}

			return name;
		}

		private DocumentBucket TryGetStoredBucket(DocumentBucket bucket)
		{
			var storedBucket = apiHelper.DocumentBuckets.Read(DocumentBucketExposers.Identifier.Equal(bucket.Identifier)).SingleOrDefault()
				?? throw new InvalidOperationException($"The bucket to which the file is supposed to be uploaded doesn't exist.");
			return storedBucket;
		}
	}
}
