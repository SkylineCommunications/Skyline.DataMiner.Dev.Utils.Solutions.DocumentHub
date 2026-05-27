namespace Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters
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
		/// Gets the web-resolvable or storage-specific path to the file.
		/// </summary>
		/// <returns>
		/// For local storage: a relative web path (e.g., "/Public/WebFileManager/docs/report.pdf").
		/// For SharePoint: the web URL of the file.
		/// For DOM: an empty string.
		/// </returns>
		string GetFilePath();

		/// <summary>
		/// Gets the file name including extension.
		/// </summary>
		/// <returns>
		/// The file name with extension (e.g., "report.pdf").
		/// </returns>
		string GetFile();

		/// <summary>
		/// Gets the type or category of the item.
		/// </summary>
		/// <returns>
		/// For local storage: the name of the parent directory (e.g., "technical").
		/// For other storage types: an empty string.
		/// </returns>
		string GetType();

		/// <summary>
		/// Gets the file extension.
		/// </summary>
		/// <returns>
		/// The file extension without the leading dot (e.g., "pdf").
		/// </returns>
		string GetExtension();

		/// <summary>
		/// Gets the name of the file without extension.
		/// </summary>
		/// <returns>
		/// The file name without extension (e.g., "report").
		/// </returns>
		string GetName();

		/// <summary>
		/// Gets the size of the file in a human-readable format.
		/// </summary>
		/// <returns>
		/// A formatted string such as "1.5 MB" or "256 KB".
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
		/// Gets the directory containing the file, relative to the storage root.
		/// </summary>
		/// <returns>
		/// For local storage: relative path from WebFileManager root (e.g., "docs/technical").
		/// For SharePoint: relative path from the document library root.
		/// For DOM: an empty string.
		/// </returns>
		string GetDirectory();

		/// <summary>
		/// Gets the DOM module in which the file's DOM instance resides.
		/// </summary>
		/// <returns>
		/// The module name when the file is stored as a DOM attachment; otherwise, an empty string.
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