namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
	using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
    using static DomHelpers.SlcDocumenthub.SlcDocumenthubIds.Enums;

	#region Interface

	/// <summary>
	/// Defines the contract for different file storage implementations.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface (e.g., <see cref="LocalHandler"/> and <see cref="SharePointHandler"/>)
	/// handle how files or images are uploaded and verified in their respective storage environments.
	/// </remarks>
	internal interface IStorageHandler
	{
		/// <summary>
		/// Checks whether a file with the specified name already exists in the given directory.
		/// </summary>
		/// <param name="directory">The target directory or storage path.</param>
		/// <param name="name">The name of the file to check.</param>
		/// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
		bool FileExists(string directory, string name);

		/// <summary>
		/// Uploads an image to the target directory or storage system.
		/// </summary>
		/// <param name="image">The <see cref="Bitmap"/> image to upload.</param>
		/// <param name="directory">The destination directory or storage path.</param>
		/// <param name="name">The name to assign to the uploaded image file.</param>
		void UploadImage(Bitmap image, string directory, string name);

        /// <summary>
        /// Uploads a file from a specified local path to the target directory or storage system.
        /// </summary>
        /// <param name="filePath">The full local path of the file to upload.</param>
        /// <param name="directory">The destination directory or storage path.</param>
        /// <param name="name">The name to assign to the uploaded file.</param>
        /// <returns>The relative path of the uploaded file.</returns>
        string UploadFile(string filePath, string directory, string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="filter"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        List<IDocHubFile> ReadFiles(Models.DocumentCategory category, string filter, PageContext context);
	}

	#endregion

	#region Factory

	/// <summary>
	/// Provides a centralized mechanism for creating instances of storage handlers.
	/// </summary>
	/// <remarks>
	/// This factory is responsible for returning the correct implementation of <see cref="IStorageHandler"/>
	/// based on the specified <see cref="Storage"/> type (e.g., Local or SharePoint).
	/// It encapsulates the logic for constructing each handler, ensuring the rest of the system remains decoupled
	/// from concrete storage implementations.
	/// </remarks>
	internal static class StorageHandlerFactory
	{
        /// <summary>
        /// Creates a new instance of a storage handler that matches the specified <see cref="Storage"/> type.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="helpers"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static IStorageHandler Create(Storagetype storage, DataHelpersDocumentHub helpers)
		{
			// Select the appropriate storage handler based on the given storage type.
			switch (storage)
			{
				case Storagetype.Sharepoint:
					// SharePoint-based storage implementation.
					return new SharePointHandler(helpers);

				case Storagetype.Local:
					// Local file system storage implementation.
					return new LocalHandler();

				default:
					// Throw an exception if no matching handler exists.
					throw new ArgumentException($"No handler registered for storage type '{storage}'.");
			}
		}
	}

	#endregion
}
