namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using DomHelpers.SlcDocumenthub;
	using Microsoft.Graph;

    /// <summary>
    /// Generic factory class that provides methods to create DocHub related objects.
    /// </summary>
    /// <remarks>
    /// This static factory encapsulates the logic for selecting the appropriate
    /// <see cref="DocHubPageData"/> implementation (for example, SharePoint or local storage)
    /// and ensures that instances are initialized with sensible defaults.
    /// </remarks>
    public static class DocHubFactory
	{
		/// <summary>
		/// Creates and initializes a <see cref="DocHubPageData"/> instance appropriate for the specified storage type..
		/// </summary>
		/// <param name="storageType">
		/// The storage type that determines which <see cref="DocHubPageData"/> implementation is created.
		/// </param>
		/// <returns>
		/// A concrete <see cref="DocHubPageData"/> instance corresponding to the specified
		/// <paramref name="storageType"/>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the specified <paramref name="storageType"/> is not supported.
		/// </exception>
		public static DocHubPageData CreatePageData(SlcDocumenthubIds.Enums.Storagetype storageType)
		{
			return CreatePageData(storageType, 200);
		}

		/// <summary>
		/// Creates and initializes a <see cref="DocHubPageData"/> instance appropriate for the specified storage type,
		/// configuring it with the provided page size.
		/// </summary>
		/// <param name="storageType">
		/// The storage type that determines which <see cref="DocHubPageData"/> implementation is created.
		/// </param>
		/// <param name="pageSize">
		/// The number of items to include per page. This value is assigned to the created instance’s
		/// <c>PageSize</c> property.
		/// </param>
		/// <returns>
		/// A concrete <see cref="DocHubPageData"/> instance corresponding to the specified
		/// <paramref name="storageType"/>, initialized with the given <paramref name="pageSize"/>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the specified <paramref name="storageType"/> is not supported.
		/// </exception>
		public static DocHubPageData CreatePageData(SlcDocumenthubIds.Enums.Storagetype storageType, int pageSize)
		{
			switch (storageType)
			{
				case SlcDocumenthubIds.Enums.Storagetype.Sharepoint:
					return new SharePointPageData() { PageSize = pageSize };

				case SlcDocumenthubIds.Enums.Storagetype.Local:
					return new LocalPageData() { PageSize = pageSize };

				default:
					throw new NotSupportedException(
						$"Storage Type '{storageType}' is not supported.");
			}
		}
	}

	/// <summary>
	/// Represents the base paging context used when reading files from a storage backend.
	/// The context instance stores paging state and must be reused between calls
	/// to continue retrieving subsequent pages.
	/// </summary>
	public abstract class DocHubPageData
    {
        /// <summary>
        /// The maximum number of files returned per page.
        /// Defaults to 200 if not explicitly set.
        /// </summary>
        public int PageSize { get; set; } = 200;

		/// <summary>
		/// Abstract level member function checks if there are more pages left.
		/// </summary>
		/// <returns></returns>
		public abstract bool HasNextPage();
    }

    /// <summary>
    /// Paging context used when reading files from SharePoint storage.
    /// This context keeps track of the folder traversal state
    /// and the SharePoint Graph paging request.
    /// </summary>
    public class SharePointPageData : DocHubPageData
    {
		/// <summary>
		/// SharePointPageData constructor is internal.
		/// SharePointPageData is created by factory.
		/// </summary>
		internal SharePointPageData()
		{
		}

		/// <summary>
		/// Queue containing the identifiers of folders that still need to be traversed.
		/// This is used internally to recursively enumerate folders across pages.
		/// </summary>
		public Queue<string> FolderQueue { get; internal set; } = new Queue<string>(new[] { "root" });

        /// <summary>
        /// The Graph API request used to retrieve the next page of drive items.
        /// When null, a new folder traversal request will be started.
        /// </summary>
        public IDriveItemChildrenCollectionRequest NextPageRequest { get; internal set; }

		/// <summary>
		/// Checks if there are more pages left.
		/// </summary>
		/// <returns></returns>
		public override bool HasNextPage()
		{
			return FolderQueue.Count > 0 || NextPageRequest != null;
		}
	}

    /// <summary>
    /// Paging context used when reading files from local file system storage.
    /// This context maintains the file enumeration state across paging calls.
    /// </summary>
    public class LocalPageData : DocHubPageData
    {
		/// <summary>
		/// LocalPageData constructor is internal.
		/// LocalPageData is created by factory.
		/// </summary>
		internal LocalPageData()
		{
		}

		/// <summary>
		/// Enumerator used to iterate through files in the current directory tree.
		/// The enumerator position is preserved between paging calls.
		/// </summary>
		public IEnumerator<FileInfo> FileEnumerator { get; internal set; }

        /// <summary>
        /// The root directory currently being enumerated.
        /// Used to detect changes in scope and reinitialize enumeration when needed.
        /// </summary>
        public string CurrentRoot { get; internal set; } = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";

		/// <summary>
		/// Checks if theres more pages left.
		/// </summary>
		/// <returns></returns>
		public override bool HasNextPage()
		{
			return FileEnumerator != null && FileEnumerator.MoveNext();
		}
	}
}
