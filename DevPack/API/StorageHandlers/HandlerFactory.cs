namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

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
		/// Reads files from the storage based on the specified bucket and filter.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing bucket and filter information.
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
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
		/// <returns>
		/// An instance of <see cref="IStorageHandler"/> corresponding to the specified storage type.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if no handler is registered for the specified storage type.
		/// </exception>
		internal static IStorageHandler Create(StorageType storage, IConnection connection)
		{
			// Select the appropriate storage handler based on the given storage type.
			switch (storage)
			{
				// TODO: if it's sharepoint, don't initialize each time. Instead, consider caching the handler instance or implementing a singleton pattern if appropriate.
				case StorageType.SharePoint:
					// SharePoint-based storage implementation.
					return new SharePointHandler(connection);

				case StorageType.Local:
					// Local file system storage implementation.
					return new LocalHandler();

				case StorageType.DOM:
					// DataMiner Object Model attachments storage implementation.
					return new DomAttachmentsHandler(connection);

				default:
					// Throw an exception if no matching handler exists.
					throw new ArgumentException($"No handler registered for storage type '{storage}'.");
			}
		}
	}
	#endregion
}