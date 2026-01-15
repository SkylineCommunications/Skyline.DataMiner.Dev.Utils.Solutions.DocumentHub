namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers;
	using Skyline.DataMiner.Utils.DocumentHub.SDM;
	using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

	/// <summary>
	/// Entry point for interacting with the DocumentHub API.
	/// Provides access to document categories and file operations.
	/// </summary>
	public class Client
    {
        private readonly IConnection _connection;

		/// <summary>
		/// Initializes a new instance of the <see cref="Client"/> class.
		/// Initializes a new DocumentHub client using an active DataMiner connection.
		/// </summary>
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
        public Client(IConnection connection)
        {
            _connection = connection;
            Helpers = new DataHelpersDocumentHub(_connection);

            Categories = new Categories(Helpers);
            Files = new Files(Helpers);
        }

        /// <summary>
        /// Gets or sets internal helper layer for DataMiner DOM and storage operations.
        /// </summary>
        internal DataHelpersDocumentHub Helpers { get; set; }

		/// <summary>
		/// Gets or sets provides access to document category management.
		/// </summary>
        public Categories Categories { get; set; }

		/// <summary>
		/// Gets or sets provides access to document file operations such as upload and read.
		/// </summary>
        public Files Files { get; set; }
    }

    /// <summary>
    /// Provides file related operations within DocumentHub.
    /// This class is the main entry point for uploading and reading files
    /// across different storage backends such as local storage and SharePoint.
    /// </summary>
    public class Files
    {
        internal Files(DataHelpersDocumentHub helpers)
        {
            Helpers = helpers;
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
        public string UploadFile(Models.DocumentCategory category, string filePath, string name = null)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);

            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileNameWithoutExtension(filePath);
            }

            if (storageHandler.FileExists(category.UploadPath, $"{name}{extension}"))
            {
                throw new InvalidOperationException($"The file '{name}' already exists.");
            }

            return storageHandler.UploadFile(filePath, category.UploadPath, $"{name}{extension}");
        }

        #endregion

        #region Read without paging

        /// <summary>
        /// Reads all files from the specified storage type.
        /// This method loads all files into memory.
        /// </summary>
        /// <param name="storagetype">
        /// The storage backend to read from.
        /// </param>
        /// <returns>
        /// A list containing all files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Storagetype storagetype)
        {
            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, null);
        }

        /// <summary>
        /// Reads all files associated with the given document category.
        /// This method loads all files into memory.
        /// </summary>
        /// <param name="category">
        /// The document category defining the storage type and base path.
        /// </param>
        /// <returns>
        /// A list containing all files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, null);
        }

        /// <summary>
        /// Reads all files from the specified storage type using a filter.
        /// This method loads all matching files into memory.
        /// </summary>
        /// <param name="storagetype">
        /// The storage backend to read from.
        /// </param>
        /// <param name="filter">
        /// Optional case insensitive filter applied to file names.
        /// </param>
        /// <returns>
        /// A list containing all matching files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Storagetype storagetype, string filter)
        {
            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, filter);
        }

        /// <summary>
        /// Reads all files associated with the given document category using a filter.
        /// This method loads all matching files into memory.
        /// </summary>
        /// <param name="category">
        /// The document category defining the storage type and base path.
        /// </param>
        /// <param name="filter">
        /// Optional case insensitive filter applied to file names.
        /// </param>
        /// <returns>
        /// A list containing all matching files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, filter);
        }

        #endregion

        #region Read with paging

        /// <summary>
        /// Reads a single page of files from the specified storage type.
        /// Paging state is maintained inside the provided <see cref="DocHubPageData"/>.
        /// </summary>
        /// <param name="storagetype">
        /// The storage backend to read from.
        /// </param>
        /// <param name="context">
        /// Paging context that maintains state between calls.
        /// The same instance must be reused to continue paging.
        /// </param>
        /// <returns>
        /// A list containing the next page of files.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Storagetype storagetype, DocHubPageData context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, null, context);
        }

        /// <summary>
        /// Reads a single page of files associated with the given document category.
        /// Paging state is maintained inside the provided <see cref="DocHubPageData"/>.
        /// </summary>
        /// <param name="category">
        /// The document category defining the storage type and base path.
        /// </param>
        /// <param name="context">
        /// Paging context that maintains state between calls.
        /// The same instance must be reused to continue paging.
        /// </param>
        /// <returns>
        /// A list containing the next page of files.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, DocHubPageData context)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, null, context);
        }

        /// <summary>
        /// Reads a single page of filtered files from the specified storage type.
        /// Paging state is maintained inside the provided <see cref="DocHubPageData"/>.
        /// </summary>
        /// <param name="storagetype">
        /// The storage backend to read from.
        /// </param>
        /// <param name="filter">
        /// Optional case insensitive filter applied to file names.
        /// </param>
        /// <param name="context">
        /// Paging context that maintains state between calls.
        /// The same instance must be reused to continue paging.
        /// </param>
        /// <returns>
        /// A list containing the next page of matching files.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Storagetype storagetype, string filter, DocHubPageData context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, filter, context);
        }

        /// <summary>
        /// Reads a single page of filtered files associated with the given document category.
        /// Paging state is maintained inside the provided <see cref="DocHubPageData"/>.
        /// </summary>
        /// <param name="category">
        /// The document category defining the storage type and base path.
        /// </param>
        /// <param name="filter">
        /// Optional case insensitive filter applied to file names.
        /// </param>
        /// <param name="context">
        /// Paging context that maintains state between calls.
        /// The same instance must be reused to continue paging.
        /// </param>
        /// <returns>
        /// A list containing the next page of matching files.
        /// </returns>
        public List<IDocHubFile> ReadFiles(
            Models.DocumentCategory category,
            string filter,
            DocHubPageData context)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, filter, context);
        }

        #endregion
    }


    /// <summary>
    /// Provides document category management operations.
    /// </summary>
    public class Categories
    {
        internal Categories(DataHelpersDocumentHub helpers)
        {
            Helpers = helpers;
        }

        internal DataHelpersDocumentHub Helpers { get; set; }

        /// <summary>
        /// Creates a new document category or updates an existing one.
        /// </summary>
        /// <param name="category">
        /// The document category to create or update.
        /// </param>
        public void CreateOrUpdateCategory(Models.DocumentCategory category)
        {
            Helpers.DocumentCategories.CreateOrUpdate(category);
        }

        /// <summary>
        /// Retrieves all document categories.
        /// </summary>
        /// <returns>
        /// A list of document categories.
        /// </returns>
        public List<Models.DocumentCategory> GetCategories()
        {
            return Helpers.DocumentCategories.Read();
        }

        /// <summary>
        /// Retrieves document categories by their identifiers.
        /// </summary>
        /// <param name="ids">
        /// Collection of category identifiers.
        /// </param>
        /// <returns>
        /// A list of matching document categories.
        /// </returns>
        public List<Models.DocumentCategory> GetCategory(List<Guid> ids)
        {
            FilterElement<Models.DocumentCategory> filter = new ORFilterElement<Models.DocumentCategory>();
            foreach (var id in ids)
            {
                filter = filter.OR(CategoryExposers.Id.Equal(id));
            }

            return Helpers.DocumentCategories.Read(filter);
        }

        /// <summary>
        /// Retrieves document categories using a custom filter.
        /// </summary>
        /// <param name="filter">
        /// A DOM filter used to query document categories.
        /// </param>
        /// <returns>
        /// A list of matching document categories.
        /// </returns>
        public List<Models.DocumentCategory> GetCategory(FilterElement<Models.DocumentCategory> filter)
        {
            return Helpers.DocumentCategories.Read(filter);
        }

        /// <summary>
        /// Attempts to delete the given document categories.
        /// </summary>
        /// <param name="items">
        /// The categories to delete.
        /// </param>
        /// <returns>
        /// True if all categories were deleted successfully, otherwise false.
        /// </returns>
        public bool TryDeleteCategory(IEnumerable<Models.DocumentCategory> items)
        {
            return Helpers.DocumentCategories.TryDelete(items);
        }
    }
}
