namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Provides file related operations within DocumentHub.
	/// This class is the main entry point for uploading and reading files
	/// across different storage backends such as local storage and SharePoint.
	/// </summary>
	public class Files
	{
		/// <summary>
		/// The DataMiner connection used for communication with the system.
		/// </summary>
		private readonly IConnection _connection;

		private readonly FileValidator validator;

		internal Files(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			validator = new FileValidator(_connection);
		}

		#region Upload

		/// <summary>
		/// Uploads a file to the storage location configured in the given document buckets.
		/// </summary>
		/// <param name="bucket">
		/// The document bucket defining the storage type and upload path.
		/// </param>
		/// <param name="filePath">
		/// The full local path of the file to upload.
		/// </param>
		/// <param name="name">
		/// Optional custom file name without extension.
		/// If null, the original file name is used.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="bucket"/> or <paramref name="filePath"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a file with the same name already exists in the target location.
		/// </exception>
		/// <returns>
		/// The path or identifier of the uploaded file.
		/// </returns>
		public string UploadFile(DocumentBucket bucket, string filePath, string name = null)
		{
			var uploadData = validator.ValidateAndSanitizeFile(bucket, filePath, name);
			var validatedBucket = uploadData.Bucket;

			// Create appropriate storage handler based on bucket's storage type.
			var storageHandler = StorageHandlerFactory.Create(validatedBucket.StorageType, _connection);

			// Check for existing file to prevent overwriting.
			if (storageHandler.FileExists(new WebFileExistsData
			{
				Directory = bucket.UploadPath,
				Name = uploadData.Name,
			}))
			{
				throw new InvalidOperationException($"The file '{uploadData.Name}' already exists.");
			}

			// Upload the file using the storage handler.
			return storageHandler.UploadFile(new WebFileUploadData(uploadData));
		}

		/// <summary>
		/// Uploads a file to the storage location configured in the given document bucket, linking it to a specific DOM instance.
		/// </summary>
		/// <param name="bucket">
		/// The document bucket defining the storage type and upload path.
		/// </param>
		/// <param name="filePath">
		/// The full local path of the file to upload.
		/// </param>
		/// <param name="domInstanceId">
		/// The unique identifier of the DOM instance to which the uploaded file should be linked.
		/// </param>
		/// <param name="name">
		/// Optional custom file name without extension.
		/// </param>
		/// <returns>
		/// The path or identifier of the uploaded file.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="bucket"/>, <paramref name="filePath"/>, or <paramref name="domInstanceId"/> is null or empty.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a file with the same name already exists in the target location.
		/// </exception>
		public string UploadFile(DocumentBucket bucket, string filePath, Guid domInstanceId, string name = null)
		{
			// Validate parameters
			if (domInstanceId == Guid.Empty)
				throw new ArgumentNullException(nameof(domInstanceId));

			var uploadData = validator.ValidateAndSanitizeFile(bucket, filePath, name);
			var validatedBucket = uploadData.Bucket;

			// Create appropriate storage handler based on bucket's storage type.
			var storageHandler = StorageHandlerFactory.Create(validatedBucket.StorageType, _connection);

			// Check for existing file to prevent overwriting.
			if (storageHandler.FileExists(new DomFileExistsData
			{
				Bucket = validatedBucket,
				DomInstanceId = domInstanceId,
				Name = uploadData.Name,
			}))
			{
				throw new InvalidOperationException($"The file '{uploadData.Name}' already exists in this DOM instance.");
			}

			// Upload the file using the storage handler.
			return storageHandler.UploadFile(new DomFileUploadData(uploadData)
			{
				DomInstanceId = domInstanceId,
			});
		}

		/// <summary>
		/// Uploads a file to the storage location configured in the given document bucket,
		/// allowing the caller to further qualify the bucket's UploadPath with an additional relative segment.
		/// Useful for adding a subfolder or logical qualifier without mutating the original bucket.
		/// </summary>
		/// <param name="bucket">
		/// The document bucket defining the storage type and base upload path.
		/// </param>
		/// <param name="filePath">
		/// The full local path of the file to upload.
		/// </param>
		/// <param name="uploadPathQualifier">
		/// Additional relative path segment to append to the bucket's UploadPath.
		/// May be null or empty to behave the same as <see cref="UploadFile(DocumentBucket,string,string)"/>.
		/// </param>
		/// <param name="name">
		/// Optional custom file name without extension.
		/// If null, the original file name is used.
		/// </param>
		/// <returns>
		/// The path or identifier of the uploaded file.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="bucket"/> or <paramref name="filePath"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when called for DOM storage (use the DOM overload) or when a file with the same name already exists.
		/// </exception>
		public string UploadFile(DocumentBucket bucket, string filePath, string uploadPathQualifier, string name = null)
		{
			var uploadData = validator.ValidateAndSanitizeFile(bucket, filePath, name);

			// This overload is intended for web-like storage backends that use UploadPath.
			// DOM storage uses DOM instances instead; instruct caller to use the DOM overload.
			if (bucket.StorageType == StorageType.DOM)
				throw new InvalidOperationException("This overload is not supported for DOM storage. Use UploadFile(bucket, filePath, domInstanceId, name) instead.");

			var storageHandler = StorageHandlerFactory.Create(bucket.StorageType, _connection);

			// Build effective upload path (do not mutate original bucket).
			string basePath = bucket.UploadPath ?? string.Empty;
			basePath = basePath.TrimEnd('\\', '/');

			string qualifier = uploadPathQualifier ?? string.Empty;
			qualifier = qualifier.TrimStart('\\', '/');

			string effectiveUploadPath;
			if (string.IsNullOrEmpty(basePath))
				effectiveUploadPath = qualifier;
			else if (string.IsNullOrEmpty(qualifier))
				effectiveUploadPath = basePath;
			else
				effectiveUploadPath = basePath + "/" + qualifier; // use forward slash as logical separator for remote stores

			// Prevent overwriting existing file in the qualified path
			if (storageHandler.FileExists(new WebFileExistsData
			{
				Directory = effectiveUploadPath,
				Name = uploadData.Name,
			}))
			{
				throw new InvalidOperationException($"The file '{uploadData.Name}' already exists.");
			}

			// Adjust the bucket's UploadPath so storage handlers see the qualified path
			uploadData.Bucket.UploadPath = effectiveUploadPath;

			// Upload using the adjusted bucket
			return storageHandler.UploadFile(new WebFileUploadData(uploadData));
		}

		#endregion

		#region Read

		/// <summary>
		/// Reads files from the specified storage type.
		/// </summary>
		/// <param name="storageType">
		/// The storage backend to read from.
		/// </param>
		/// <param name="context">
		/// Optional paging context that maintains paging state between calls.
		/// Pass the same instance to continue paging.
		/// Use named arguments to skip this parameter if not needed.
		/// </param>
		/// <param name="filter">
		/// Optional case-insensitive filter applied to file names.
		/// Use named arguments to specify this parameter without passing a paging context.
		/// </param>
		/// <returns>
		/// A list of files represented as <see cref="IDocHubFile"/>.
		/// </returns>
		/// <remarks>
		/// Named arguments allow callers to specify only the parameters they need:
		/// <code>
		/// ReadFiles(<seealso cref="StorageType.DOM"/>, filter: "invoice");
		/// ReadFiles(<seealso cref="StorageType.DOM"/>, context: pageData);
		/// </code>
		/// </remarks>
		public List<IDocHubFile> ReadFiles(StorageType storageType, DocHubPageData context = null, string filter = null)
		{
			ReadData data = storageType == StorageType.DOM
				? (ReadData)new DomFileReadData()
				: new WebFileReadData();

			data.Filter = filter;
			data.Context = context;

			return ReadFiles(storageType, data);
		}

		/// <summary>
		/// Reads files associated with the specified document bucket.
		/// </summary>
		/// <param name="bucket">
		/// The document bucket defining the storage type and base path.
		/// </param>
		/// <param name="context">
		/// Optional paging context that maintains paging state between calls.
		/// Pass the same instance to continue paging.
		/// Use named arguments to skip this parameter if not needed.
		/// </param>
		/// <param name="filter">
		/// Optional case-insensitive filter applied to file names.
		/// Use named arguments to specify this parameter without passing a paging context.
		/// </param>
		/// <returns>
		/// A list of files represented as <see cref="IDocHubFile"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="bucket"/> is null.
		/// </exception>
		/// <remarks>
		/// Named arguments allow callers to specify only the parameters they need:
		/// <code>
		/// ReadFiles(bucket, filter: "invoice");
		/// ReadFiles(bucket, context: pageData);
		/// </code>
		/// </remarks>
		public List<IDocHubFile> ReadFiles(DocumentBucket bucket, DocHubPageData context = null, string filter = null)
		{
			FileValidator.ValidateBaseParameters(bucket, "OK"); // Reusing the validation method for null check on bucket; filePath is irrelevant here so passing dummy value

			ReadData data = bucket.StorageType == StorageType.DOM
				? (ReadData)new DomFileReadData()
				: new WebFileReadData();

			data.Bucket = bucket;
			data.Filter = filter;
			data.Context = context;

			return ReadFiles(bucket.StorageType, data);
		}

		/// <summary>
		/// Reads files from a DOM source for the specified DOM instance IDs.
		/// </summary>
		/// <param name="source">
		/// The DOM source containing the module where the files are stored.
		/// </param>
		/// <param name="domInstanceIds">
		/// The DOM instance identifiers whose attachments should be retrieved.
		/// If this list is empty, all DOM instances in the specified module will be matched.
		/// If <c>null</c>, an <see cref="ArgumentNullException"/> is thrown.
		/// </param>
		/// <param name="context">
		/// Optional paging context that maintains paging state between calls.
		/// Pass the same instance to continue paging.
		/// Use named arguments to skip this parameter if not needed.
		/// </param>
		/// <param name="filter">
		/// Optional case-insensitive filter applied to file names.
		/// Use named arguments to specify this parameter without passing a paging context.
		/// </param>
		/// <returns>
		/// A list of files represented as <see cref="IDocHubFile"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="source"/> or <paramref name="domInstanceIds"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="source.Module"/> is <c>null</c> or empty.
		/// </exception>
		/// <remarks>
		/// Named arguments allow callers to specify only the parameters they need:
		/// <code>
		/// ReadFiles(source, ids, filter: "invoice");
		/// ReadFiles(source, ids, context: pageData);
		/// </code>
		/// </remarks>
		public List<IDocHubFile> ReadFiles(DomSource source, IEnumerable<Guid> domInstanceIds, DocHubPageData context = null, string filter = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrEmpty(source.Module))
				throw new ArgumentException("Module cannot be null or empty.", nameof(source));
			if (domInstanceIds == null)
				throw new ArgumentNullException(nameof(domInstanceIds));

			ReadData data = new DomFileReadData
			{
				Module = source.Module,
				DomInstanceIds = domInstanceIds.ToList(),
				Filter = filter,
				Context = context,
			};

			return ReadFiles(StorageType.DOM, data);
		}

		/// <summary>
		/// Retrieves the raw bytes of a DOM attachment represented by the given <see cref="IDocHubDomFile"/> instance.
		/// </summary>
		/// <param name="domFile">
		/// The DOM file instance from which to retrieve the bytes.
		/// </param>
		/// <returns>
		/// A byte array containing the contents of the specified DOM attachment.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="domFile"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the DOM instance referenced by <paramref name="domFile"/> cannot be found.
		/// </exception>
		public byte[] GetBytes(IDocHubDomFile domFile)
		{
			if (domFile == null)
				throw new ArgumentNullException(nameof(domFile));

			return GetBytes(domFile.GetModule(), domFile.GetInstanceId(), domFile.GetFile());
		}

		/// <summary>
		/// Retrieves the raw bytes of a DOM attachment from the specified module, instance, and filename.
		/// </summary>
		/// <param name="moduleId">
		/// The identifier of the DOM module containing the attachment.
		/// </param>
		/// <param name="instanceId">
		/// The unique identifier of the DOM instance containing the attachment.
		/// </param>
		/// <param name="filename">
		/// The name of the attachment file to retrieve.
		/// </param>
		/// <returns>
		/// A byte array containing the contents of the specified DOM attachment.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="moduleId"/> or <paramref name="filename"/> is null or empty.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the DOM instance identified by <paramref name="instanceId"/> cannot be found in the specified module.
		/// </exception>
		public byte[] GetBytes(string moduleId, Guid instanceId, string filename)
		{
			if (string.IsNullOrEmpty(moduleId))
				throw new ArgumentNullException(nameof(moduleId));
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentNullException(nameof(filename));

			var domHelper = new DomHelper(_connection.HandleMessages, moduleId);

			var instance = domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(instanceId)).SingleOrDefault();
			if (instance == null)
				throw new InvalidOperationException($"Could not find DOM instance with id {instanceId}");

			return domHelper.DomInstances.Attachments.Get(instance.ID, filename);
		}
		#endregion

		#region Delete

		/// <summary>
		/// Deletes a document from local DataMiner storage.
		/// </summary>
		/// <param name="bucket">
		/// The document bucket that defines the upload path where the file is stored.
		/// Must use <see cref="StorageType.Local"/> storage.
		/// </param>
		/// <param name="fileName">
		/// The name of the file to delete (including extension).
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="bucket"/> or <paramref name="fileName"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the bucket's storage type is not <see cref="StorageType.Local"/>.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// Thrown when the file does not exist at the expected path.
		/// </exception>
		public void DeleteFile(DocumentBucket bucket, string fileName)
		{
			FileValidator.ValidateBaseParameters(bucket, fileName);

			var storageHandler = StorageHandlerFactory.Create(bucket.StorageType, _connection);

			if (!(storageHandler is IDeletableStorageHandler deletableHandler))
				throw new InvalidOperationException($"Delete is not supported for storage type '{bucket.StorageType}'.");

			var data = new DeleteData
			{
				Bucket = bucket,
				Name = fileName,
			};

			deletableHandler.DeleteFile(data);
		}

		#endregion

		#region Internal and Private Methods

		internal List<IDocHubFile> ReadFiles(StorageType storagetype, ReadData data)
		{
			var storageHandler = StorageHandlerFactory.Create(storagetype, _connection);
			return storageHandler.ReadFiles(data);
		}

		#endregion
	}
}