namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.Paging;
    using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

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

		internal Files(DataHelpersDocumentHub helpers, IConnection connection)
		{
			Helpers = helpers;
			_connection = connection;
		}

		internal DataHelpersDocumentHub Helpers { get; }

		#region Upload

		/// <summary>
		/// Uploads a file to the storage location configured in the given document category.
		/// </summary>
		/// <param name="category">
		/// The document category defining the storage type and upload path.
		/// </param>
		/// <param name="filePath">
		/// The full local path of the file to upload.
		/// </param>
		/// <param name="name">
		/// Optional custom file name without extension.
		/// If null, the original file name is used.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="category"/> or <paramref name="filePath"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a file with the same name already exists in the target location.
		/// </exception>
		/// <returns>
		/// The path or identifier of the uploaded file.
		/// </returns>
		public string UploadFile(Models.DocumentCategory category, string filePath, string name = null)
		{
			// Validate parameters
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));

			// Create appropriate storage handler based on category's storage type.
			var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers, _connection);

			// Rename file if necessary.
			if (string.IsNullOrEmpty(name))
			{
				name = Path.GetFileNameWithoutExtension(filePath);
			}

			// Check for existing file to prevent overwriting.
			string extension = Path.GetExtension(filePath);
			if (storageHandler.FileExists(new WebFileExistsData
			{
				Directory = category.UploadPath,
				Name = $"{name}{extension}",
			}))
			{
				throw new InvalidOperationException($"The file '{name}' already exists.");
			}

			// Upload the file using the storage handler.
			return storageHandler.UploadFile(new WebFileUploadData
			{
				Category = category,
				FilePath = filePath,
				Name = $"{name}{extension}",
			});
		}

		/// <summary>
		/// Uploads a file to the storage location configured in the given document category, linking it to a specific DOM instance.
		/// </summary>
		/// <param name="category">
		/// The document category defining the storage type and upload path.
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
		/// Thrown when <paramref name="category"/>, <paramref name="filePath"/>, or <paramref name="domInstanceId"/> is null or empty.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown when a file with the same name already exists in the target location.
		/// </exception>
		public string UploadFile(Models.DocumentCategory category, string filePath, Guid domInstanceId, string name = null)
		{
			// Validate parameters
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));
			if (domInstanceId == Guid.Empty)
				throw new ArgumentNullException(nameof(domInstanceId));

			// Create appropriate storage handler based on category's storage type.
			var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers, _connection);

			// Rename file if necessary.
			if (string.IsNullOrEmpty(name))
			{
				name = Path.GetFileNameWithoutExtension(filePath);
			}

			// Check for existing file to prevent overwriting.
			string extension = Path.GetExtension(filePath);
			if (storageHandler.FileExists(new DomFileExistsData
			{
				Category = category,
				DomInstanceId = domInstanceId,
				Name = $"{name}{extension}",
			}))
			{
				throw new InvalidOperationException($"The file '{name}' already exists in this DOM instance.");
			}

			// Upload the file using the storage handler.
			return storageHandler.UploadFile(new DomFileUploadData
			{
				Category = category,
				DomInstanceId = domInstanceId,
				FilePath = filePath,
				Name = $"{name}{extension}",
			});
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
		/// ReadFiles(Storagetype.DOM, filter: "invoice");
		/// ReadFiles(Storagetype.DOM, context: pageData);
		/// </code>
		/// </remarks>
		public List<IDocHubFile> ReadFiles(Storagetype storageType, DocHubPageData context = null, string filter = null)
		{
			ReadData data = storageType == Storagetype.DOM
				? (ReadData)new DomFileReadData()
				: new WebFileReadData();

			data.Filter = filter;
			data.Context = context;

			return ReadFiles(storageType, data);
		}

		/// <summary>
		/// Reads files associated with the specified document category.
		/// </summary>
		/// <param name="category">
		/// The document category defining the storage type and base path.
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
		/// Thrown if <paramref name="category"/> is null.
		/// </exception>
		/// <remarks>
		/// Named arguments allow callers to specify only the parameters they need:
		/// <code>
		/// ReadFiles(category, filter: "invoice");
		/// ReadFiles(category, context: pageData);
		/// </code>
		/// </remarks>
		public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, DocHubPageData context = null, string filter = null)
		{
			if (category == null)
				throw new ArgumentNullException(nameof(category));

			ReadData data = category.StorageType == Storagetype.DOM
				? (ReadData)new DomFileReadData()
				: new WebFileReadData();

			data.Category = category;
			data.Filter = filter;
			data.Context = context;

			return ReadFiles(category.StorageType, data);
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
		public List<IDocHubFile> ReadFiles(Models.Sources.DomSource source, IEnumerable<Guid> domInstanceIds, DocHubPageData context = null, string filter = null)
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

			return ReadFiles(Storagetype.DOM, data);
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

		#region Internal Methods
		internal List<IDocHubFile> ReadFiles(Storagetype storagetype, ReadData data)
		{
			var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers, _connection);
			return storageHandler.ReadFiles(data);
		}
		#endregion
	}
}
