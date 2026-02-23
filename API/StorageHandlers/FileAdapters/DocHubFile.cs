namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.FileAdapters
{
	using System;

	/// <summary>
	/// Represents a file returned by the DocumentHub API.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface abstract the underlying storage system,
	/// such as local file storage or SharePoint document libraries.
	/// </remarks>
	public interface IDocHubFile
	{
		/// <summary>
		/// Gets the full path to the file within the storage backend.
		/// </summary>
		/// <returns>
		/// A storage specific path identifying the file location.
		/// </returns>
		string GetFilePath();

		/// <summary>
		/// Gets the full file reference used to access or download the file.
		/// </summary>
		/// <returns>
		/// A string representing the file reference.
		/// </returns>
		string GetFile();

		/// <summary>
		/// Gets the type of the item.
		/// </summary>
		/// <returns>
		/// A string describing the file type.
		/// Example. File
		/// </returns>
		string GetType();

		/// <summary>
		/// Gets the file extension.
		/// </summary>
		/// <returns>
		/// The file extension including the leading dot.
		/// Example. .pdf
		/// </returns>
		string GetExtension();

		/// <summary>
		/// Gets the name of the file.
		/// </summary>
		/// <returns>
		/// The file name including extension.
		/// </returns>
		string GetName();

		/// <summary>
		/// Gets the size of the file.
		/// </summary>
		/// <returns>
		/// File size represented as a string.
		/// </returns>
		string GetSize();

		/// <summary>
		/// Gets the creation date and time of the file.
		/// </summary>
		/// <returns>
		/// The creation timestamp in UTC.
		/// </returns>
		DateTime GetCreatedAt();

		/// <summary>
		/// Gets the identifier of the user or system that created the file.
		/// </summary>
		/// <returns>
		/// Creator name or identifier when available.
		/// </returns>
		string GetCreatedBy();

		/// <summary>
		/// Gets the directory containing the file.
		/// </summary>
		/// <returns>
		/// A path relative to the storage root.
		/// </returns>
		string GetDirectory();

		/// <summary>
		/// Gets the module inside with the DOM instance to whom the file is attached.
		/// </summary>
		/// <returns>
		/// The module name or identifier when available.
		/// </returns>
		string GetModule();

		/// <summary>
		/// Gets the name of the DOM instance to which the file is attached.
		/// </summary>
		/// <returns>
		/// The DOM instance name or identifier when available.
		/// </returns>
		string GetInstanceName();

		/// <summary>
		/// Gets the identifier of the DOM instance to which the file is attached.
		/// </summary>
		/// <returns>
		/// The DOM instance identifier when available.
		/// </returns>
		Guid GetInstanceId();
	}
}
