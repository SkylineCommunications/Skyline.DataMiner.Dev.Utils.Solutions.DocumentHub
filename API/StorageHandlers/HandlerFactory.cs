namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;
    using Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters;
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
		/// <param name="data">
		/// The storage handler data containing directory and file name information.
		/// </param>
		/// <returns>
		/// <c>true</c> if the file exists; otherwise, <c>false</c>.
		/// </returns>
		bool FileExists(FileExistsData data);

		/// <summary>
		/// Uploads a file from a specified local path to the target directory or storage system.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing file and target information.
		/// </param>
		/// <returns>
		/// The relative path of the uploaded file.
		/// </returns>
		string UploadFile(UploadData data);

		/// <summary>
		/// Reads files from the storage based on the specified category and filter.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing category and filter information.
		/// </param>
		/// <returns>
		/// A list of <see cref="IDocHubFile"/> objects representing the files read from storage.
		/// </returns>
		List<IDocHubFile> ReadFiles(ReadData data);
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
		/// <param name="storage">
		/// The type of storage for which to create a handler.
		/// </param>
		/// <param name="helpers">
		/// The document hub helpers instance to be used by the storage handler.
		/// </param>
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
		/// <returns>
		/// An instance of <see cref="IStorageHandler"/> corresponding to the specified storage type.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if no handler is registered for the specified storage type.
		/// </exception>
		internal static IStorageHandler Create(Storagetype storage, DataHelpersDocumentHub helpers, IConnection connection)
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

				case Storagetype.DOM:
					// DataMiner Object Model attachments storage implementation.
					return new DOMAttachmentsHandler(connection);

				default:
					// Throw an exception if no matching handler exists.
					throw new ArgumentException($"No handler registered for storage type '{storage}'.");
			}
		}
	}
	#endregion
}
