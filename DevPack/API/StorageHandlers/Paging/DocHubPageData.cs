namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
	using System;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Generic factory class that provides methods to create DocHub related objects.
	/// </summary>
	/// <remarks>
	/// This static factory encapsulates the logic for selecting the appropriate
	/// <see cref="DocHubPageData"/> implementation (for example, SharePoint or local storage)
	/// and ensures that instances are initialized with sensible defaults.
	/// </remarks>
	public static class DocHubPageFactory
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
		public static DocHubPageData CreatePageData(StorageType storageType)
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
		public static DocHubPageData CreatePageData(StorageType storageType, int pageSize)
		{
			switch (storageType)
			{
				case StorageType.SharePoint:
					return new SharePointPageData() { PageSize = pageSize };

				case StorageType.Local:
					return new LocalPageData() { PageSize = pageSize };

				case StorageType.DOM:
					return new DOMPageData() { PageSize = pageSize };

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
		/// Gets or sets the maximum number of files returned per page.
		/// </summary>
		public int PageSize { get; set; } = 100;

		/// <summary>
		/// Abstract level member function checks if there are more pages left.
		/// </summary>
		/// <returns>
		///   <c>true</c> if there are more pages; otherwise, <c>false</c>.
		/// </returns>
		public abstract bool HasNextPage();
	}
}