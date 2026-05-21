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

		/// <summary>
		/// Validates the <see cref="DocumentBucket"/> and its related <see cref="DomSource"/>.
		/// </summary>
		/// <param name="bucket">The document bucket to validate.</param>
		/// <param name="source">DOM source returned if the validation succeeded as an <see langword="out"/> parameter.</param>
		/// <param name="errorMessage">Error message returned if the validation fails as an <see langword="out"/> parameter.</param>
		/// <returns>
		/// <see langword="false"/> if any either of the <paramref name="bucket"/>
		/// or its <see cref="DomSource"/> are null or missing. Returns <see langword="true"/> otherwise.
		/// </returns>
		internal bool ValidateAndGetDOMSourceBucket(DocumentBucket bucket, out DomSource source, out string errorMessage)
		{
			try
			{
				if (bucket == null)
					throw new ArgumentNullException(nameof(bucket));

				var bucketSource = bucket.DOMSource;
				if (bucketSource == null || string.IsNullOrWhiteSpace(bucketSource.Identifier))
				{
					throw new ArgumentException(
						$"The bucket '{bucket.Name}' is not of source type DOM, but it is expected to be for this operation." +
						$" Try using one of the other 'ReadFiles' method overloads.");
				}

				var domSource = apiHelper.DomSources.Read(DomSourceExposers.Identifier.Equal(bucketSource.Identifier)).FirstOrDefault()
					?? throw new ArgumentException($"The DOM Source (ID: {bucketSource.Identifier}) tied to the bucket does not exist in the repository.");
				if (string.IsNullOrEmpty(domSource.Module))
					throw new ArgumentException("DOM Module of the specified DOM source cannot be null or empty.");

				errorMessage = string.Empty;
				source = domSource;
				return true;
			}
			catch (Exception ex)
			{
				source = default;
				errorMessage = ex.Message;
				return false;
			}
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

		/// <summary>
		/// Tries to retrieve the bucket from the storage to ensure it exists and to get the stored version of it. This is needed because the bucket provided as a parameter might not be up to date.
		/// </summary>
		/// <param name="bucket">The bucket to search for in the repository by the "Identifier" property.</param>
		/// <returns>Stored <see cref="DocumentBucket"/>, if found.</returns>
		/// <exception cref="InvalidOperationException">The operation is cancelled if the bucket is not stored.</exception>
		private DocumentBucket TryGetStoredBucket(DocumentBucket bucket)
		{
			var storedBucket = apiHelper.DocumentBuckets.Read(DocumentBucketExposers.Identifier.Equal(bucket.Identifier)).SingleOrDefault()
				?? throw new InvalidOperationException($"The bucket to which the file is supposed to be uploaded doesn't exist.");
			return storedBucket;
		}
	}
}
