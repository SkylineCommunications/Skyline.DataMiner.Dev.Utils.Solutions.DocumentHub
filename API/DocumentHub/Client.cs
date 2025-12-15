using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers;
using Skyline.DataMiner.Utils.DocumentHub.SDM;
using System;
using System.Collections.Generic;
using System.IO;
using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
    /// <summary>
    /// Entry point for interacting with the DocumentHub API.
    /// Provides access to document categories and file operations.
    /// </summary>
    public class Client
    {
        private readonly IConnection _connection;

        /// <summary>
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
        /// Internal helper layer for DataMiner DOM and storage operations.
        /// </summary>
        internal DataHelpersDocumentHub Helpers { get; set; }

        /// <summary>
        /// Provides access to document category management.
        /// </summary>
        public Categories Categories { get; set; }

        /// <summary>
        /// Provides access to document file operations such as upload and read.
        /// </summary>
        public Files Files { get; set; }
    }

    /// <summary>
    /// Provides file related operations within DocumentHub.
    /// </summary>
    public class Files
    {
        internal Files(DataHelpersDocumentHub helpers)
        {
            Helpers = helpers;
        }

        internal DataHelpersDocumentHub Helpers { get; set; }

        /// <summary>
        /// Uploads a file to the storage location configured in the given category.
        /// </summary>
        /// <param name="category">
        /// The document category defining storage type and upload path.
        /// </param>
        /// <param name="filePath">
        /// Full local path of the file to upload.
        /// </param>
        /// <param name="name">
        /// Optional custom file name without extension.
        /// If null, the original file name is used.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a file with the same name already exists in the target location.
        /// </exception>
        public void UploadFile(Models.DocumentCategory category, string filePath, string name = null)
        {
            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);

            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileNameWithoutExtension(filePath);
            }

            // Check if a file with the same name already exists in the target location.
            if (storageHandler.FileExists(category.UploadPath, $"{name}{extension}"))
            {
                throw new InvalidOperationException($"The file '{name}' already exists.");
            }

            // Perform the upload using the configured storage handler.
            storageHandler.UploadFile(filePath, category.UploadPath, $"{name}{extension}");
        }

        /// <summary>
        /// Reads all files from the specified storage type.
        /// </summary>
        /// <param name="storagetype">
        /// The storage backend to read from.
        /// </param>
        /// <returns>
        /// A list of files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Storagetype storagetype)
        {
            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, null);
        }

        /// <summary>
        /// Reads all files associated with the given document category.
        /// </summary>
        /// <param name="category">
        /// The document category defining storage type and base path.
        /// </param>
        /// <returns>
        /// A list of files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category)
        {
            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, null);
        }

        /// <summary>
        /// Reads files from the specified storage type using a filter.
        /// </summary>
        /// <param name="storagetype">
        /// The storage backend to read from.
        /// </param>
        /// <param name="filter">
        /// Optional filter string applied by the storage handler.
        /// </param>
        /// <returns>
        /// A filtered list of files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Storagetype storagetype, string filter)
        {
            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, filter);
        }

        /// <summary>
        /// Reads files associated with the given category using a filter.
        /// </summary>
        /// <param name="category">
        /// The document category defining storage type and base path.
        /// </param>
        /// <param name="filter">
        /// Optional filter string applied by the storage handler.
        /// </param>
        /// <returns>
        /// A filtered list of files represented as <see cref="IDocHubFile"/>.
        /// </returns>
        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, filter);
        }
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
