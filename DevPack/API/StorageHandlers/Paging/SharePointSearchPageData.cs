namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
	using System.Collections.Concurrent;
	using Microsoft.Graph.Models;

	/// <summary>
	/// Paging context used when searching files in SharePoint storage via Microsoft Graph's
	/// driveItem <c>search</c> endpoint.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="SharePointPageData"/>, no folder queue is required because Graph search
	/// returns a flat, drive-wide result set. Only the OData continuation link and an overflow
	/// buffer are maintained so multiple Graph pages can be aggregated into a single logical page
	/// of <see cref="DocHubPageData.PageSize"/> items.
	/// </remarks>
	public class SharePointSearchPageData : DocHubPageData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SharePointSearchPageData"/> class.
		/// </summary>
		public SharePointSearchPageData()
		{
		}

		/// <summary>
		/// Gets the buffer for DriveItems that were retrieved from Microsoft Graph but did not
		/// fit into the previous logical page.
		/// </summary>
		public ConcurrentQueue<DriveItem> PageRemainderBuffer { get; } = new ConcurrentQueue<DriveItem>();

		/// <summary>
		/// Gets the Graph <c>@odata.nextLink</c> continuation URL. When <c>null</c> and
		/// <see cref="SearchStarted"/> is <c>true</c>, no more Graph pages are available.
		/// </summary>
		public string NextPageLink { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether the initial search request has been issued.
		/// </summary>
		public bool SearchStarted { get; internal set; }

		/// <summary>
		/// Gets or sets the resolved Graph item id of the folder the search is scoped to
		/// (derived from <c>DocumentBucket.UploadPath</c>). Populated on the first page
		/// request and reused for subsequent pages.
		/// </summary>
		public string FolderId { get; internal set; }

		/// <summary>
		/// Checks if there are more pages left.
		/// </summary>
		/// <returns>
		///   <c>true</c> if there are more pages; otherwise, <c>false</c>.
		/// </returns>
		public override bool HasNextPage()
		{
			return !SearchStarted
				|| NextPageLink != null
				|| PageRemainderBuffer.Count > 0;
		}
	}
}
