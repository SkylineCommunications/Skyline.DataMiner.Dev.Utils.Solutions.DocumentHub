using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.Serialization;
using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers;
using Skyline.DataMiner.Utils.DocumentHub.API.UploadHandlers;
using Skyline.DataMiner.Utils.DocumentHub.SDM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
    public class Client
    {
        private IConnection _connection;

        public Client(Net.IConnection connection)
        {
            _connection = connection;
            Helpers = new DataHelpersDocumentHub(_connection);

            Categories = new Categories(Helpers);
            Files = new Files(Helpers);
        }
        internal DataHelpersDocumentHub Helpers { get; set; }

        public Categories Categories { get; set; }

        public Files Files { get; set; }

    }

    public class Files
    {
        internal Files(DataHelpersDocumentHub helpers)
        {
            Helpers = helpers;
        }

        internal DataHelpersDocumentHub Helpers { get; set; }

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

        public List<IDocHubFile> ReadFiles(Storagetype storagetype)
        {
            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, null);
        }

        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category)
        {
            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, null);
        }

        public List<IDocHubFile> ReadFiles(Storagetype storagetype, string filter)
        {
            var storageHandler = StorageHandlerFactory.Create(storagetype, Helpers);
            return storageHandler.ReadFiles(null, filter);

        }

        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles(category, filter);
        }
    }

    public class Categories
    {
        internal Categories(DataHelpersDocumentHub helpers)
        {
            Helpers = helpers;
        }

        internal DataHelpersDocumentHub Helpers { get; set; }

        public void CreateOrUpdateCategory(Models.DocumentCategory category)
        {
            Helpers.DocumentCategories.CreateOrUpdate(category);
        }

        public List<Models.DocumentCategory> GetCategories()
        {
            return Helpers.DocumentCategories.Read();
        }

        public List<Models.DocumentCategory> GetCategory(List<Guid> ids)
        {

            FilterElement<Models.DocumentCategory> filter = new ORFilterElement<Models.DocumentCategory>();
            foreach (var id in ids)
            {
                filter = filter.OR(CategoryExposers.Id.Equal(id));
            }
            return Helpers.DocumentCategories.Read(filter);
        }

        public List<Models.DocumentCategory> GetCategory(FilterElement<Models.DocumentCategory> filter)
        {
            return Helpers.DocumentCategories.Read(filter);
        }

        public bool TryDeleteCategory(IEnumerable<Models.DocumentCategory> items)
        {
            return Helpers.DocumentCategories.TryDelete(items);
        }
    }
}
