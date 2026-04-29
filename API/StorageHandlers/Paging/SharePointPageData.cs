namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
    using System.Collections.Generic;
    using Microsoft.Graph;

    /// <summary>
    /// Paging context used when reading files from SharePoint storage.
    /// This context keeps track of the folder traversal state
    /// and the SharePoint Graph paging request.
    /// </summary>
    public class SharePointPageData : DocHubPageData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SharePointPageData"/> class.
		/// SharePointPageData constructor is internal.
		/// SharePointPageData is created by factory.
		/// </summary>
		public SharePointPageData()
		{
		}

		/// <summary>
		/// Buffer for DriveItems that were retrieved from Microsoft Graph but could not fit into the
		/// current logical page.
		/// </summary>
		public Queue<DriveItem> PageRemainderBuffer { get; } = new Queue<DriveItem>();

		/// <summary>
		/// Gets queue containing the identifiers of folders that still need to be traversed.
		/// This is used internally to recursively enumerate folders across pages.
		/// </summary>
		public Queue<string> FolderQueue { get; internal set; } = new Queue<string>(new[] { "root" });

		/// <summary>
		/// Gets the Graph API request used to retrieve the next page of drive items.
		/// When null, a new folder traversal request will be started.
		/// </summary>
		public IDriveItemChildrenCollectionRequest NextPageRequest { get; internal set; }

		/// <summary>
		/// Checks if there are more pages left.
		/// </summary>
		/// <returns>
		///   <c>true</c> if there are more pages; otherwise, <c>false</c>.
		/// </returns>
		public override bool HasNextPage()
		{
			return FolderQueue.Count > 0
				|| NextPageRequest != null
				|| PageRemainderBuffer.Count > 0;
		}
	}
}