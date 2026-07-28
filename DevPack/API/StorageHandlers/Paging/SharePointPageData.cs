namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using Microsoft.Graph.Models;

	/// <summary>
	/// Paging context used when reading files from SharePoint storage.
	/// </summary>
	public class SharePointPageData : DocHubPageData
	{
		public SharePointPageData()
		{
		}

		/// <summary>
		/// Gets the buffer for DriveItems that did not fit into the current logical page.
		/// </summary>
		public ConcurrentQueue<DriveItem> PageRemainderBuffer { get; } = new ConcurrentQueue<DriveItem>();

		/// <summary>
		/// Gets the identifiers of folders that still need to be traversed.
		/// </summary>
		public ConcurrentQueue<string> FolderQueue { get; internal set; } = new ConcurrentQueue<string>(new[] { "root" });

		/// <summary>
		/// Gets the continuation URL (@odata.nextLink) for the folder currently being paged.
		/// Null when a new folder traversal must be started.
		/// </summary>
		public string NextPageLink { get; internal set; }

		/// <summary>
		/// Gets the ID of the folder whose children are currently being paged.
		/// </summary>
		public string CurrentFolderId { get; internal set; }

		public override bool HasNextPage()
		{
			return FolderQueue.Count > 0
				|| NextPageLink != null
				|| PageRemainderBuffer.Count > 0;
		}
	}
}
