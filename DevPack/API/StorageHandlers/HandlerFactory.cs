namespace Skyline.DataMiner.Solutions.DocumentHub.API
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers;
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

		/// <summary>
		/// Asynchronously reads files from the storage based on the specified category and filter.
		/// </summary>
		/// <param name="data">
		/// The storage handler data containing category and filter information.
		/// </param>
		/// <returns>/ list of <see cref="IDocHubFile"/> objects representing the files read from storage.</returns>
		Task<List<IDocHubFile>> ReadFilesAsync(ReadData data);
	}

	/// <summary>
	/// Defines the contract for storage handlers that support file deletion.
	/// </summary>
	/// <remarks>
	/// Handlers that support deleting files from storage should implement this interface.
	/// This allows deletion capability to be extended to new handlers without modifying <see cref="IStorageHandler"/>.
	/// </remarks>
	internal interface IDeletableStorageHandler
	{
		/// <summary>
		/// Deletes a file from the storage.
		/// </summary>
		/// <param name="data">
		/// The delete data containing the bucket and file name.
		/// </param>
		void DeleteFile(DeleteData data);
	}
	#endregion

	#region Factory

	/// <summary>
	/// Provides a centralized mechanism for creating instances of storage handlers.
	/// </summary>
	/// <remarks>
	/// This factory is responsible for returning the correct implementation of <see cref="IStorageHandler"/>
	/// based on the specified <see cref="DocumentBucket.StorageType"/> (e.g., Local or SharePoint) and
	/// it encapsulates the logic for constructing each handler, ensuring the rest of the system remains decoupled
	/// from concrete storage implementations.
	/// </remarks>
	internal static class StorageHandlerFactory
	{
		/// <summary>
		/// Creates a new instance of a storage handler that corresponds to the provided <see cref="DocumentBucket"/>.
		/// </summary>
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
		/// <param name="bucket">
		/// The <see cref="DocumentBucket"/> containing the storage type and related configuration for which a handler is to be created.
		/// </param>
		/// <returns>
		/// An instance of <see cref="IStorageHandler"/> corresponding to the specified storage type.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if no handler is registered for the specified storage type.
		/// </exception>
		internal static IStorageHandler Create(IConnection connection, DocumentBucket bucket)
		{
			// Select the appropriate storage handler based on the given storage type.
			var storage = bucket?.StorageType;
			switch (storage)
			{
				case StorageType.SharePoint:
					// SharePoint-based storage implementation.
					return new SharePointHandler(connection, bucket);

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