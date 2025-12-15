using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
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
        internal DataHelpersDocumentHub Helpers { get; set; }

        public void CreateOrUpdateCategory(Models.DocumentCategory category)
        {
            Helpers.DocumentCategories.CreateOrUpdate(category);
        }

        public List<Models.DocumentCategory> GetAllCategories()
        {
            return Helpers.DocumentCategories.Read();
        }

        public Models.DocumentCategory GetCategoryById(Guid id)
        {
            var filter = CategoryExposers.Id.Equal(id);
            return Helpers.DocumentCategories.Read(filter).SingleOrDefault();
        }

        public bool TryDeleteCategory(IEnumerable<Models.DocumentCategory> items)
        {
             return Helpers.DocumentCategories.TryDelete(items);
        }

        public string[] GetAllowedExtensions(Models.DocumentCategory category)
        {
            var input = category.Extensions;
            if (string.IsNullOrWhiteSpace(input))
            {
                return new string[0];
            }

            // Split by comma, trim whitespace, and prepend a dot to each extension.
            return input
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => "." + ext.Trim())
                .ToArray();
        }

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
            var storageHandler =  StorageHandlerFactory.Create(storagetype, Helpers);
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
            return storageHandler.ReadFiles(null , filter);

        }

        public List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter)
        {
            var storageHandler = StorageHandlerFactory.Create(category.StorageType, Helpers);
            return storageHandler.ReadFiles (category, filter);
        }
    }
}
