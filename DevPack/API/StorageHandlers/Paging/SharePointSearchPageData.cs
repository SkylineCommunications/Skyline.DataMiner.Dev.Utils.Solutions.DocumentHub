namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
	using System.Collections.Concurrent;
	using Microsoft.Graph.Models;

	/// <summary>
	/// Paging context used when searching files in SharePoint storage via the Microsoft Graph
	/// <c>POST /search/query</c> endpoint.
	/// </summary>
	/// <remarks>
	/// The Microsoft Search endpoint uses cursor-based paging via <see cref="From"/> and
	/// <see cref="DocHubPageData.PageSize"/> (as <c>size</c>) instead of an <c>@odata.nextLink</c>.
	/// The <see cref="PathClause"/> is derived once from <c>DocumentBucket.UploadPath</c> and
	/// re-used across pages.
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
		/// Gets the buffer for DriveItems that were retrieved from Microsoft Search but did not
		/// fit into the previous logical page.
		/// </summary>
		public ConcurrentQueue<DriveItem> PageRemainderBuffer { get; } = new ConcurrentQueue<DriveItem>();

		/// <summary>
		/// Gets the next result offset (Microsoft Search <c>from</c> parameter).
		/// </summary>
		public int From { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether Microsoft Search reported more results beyond the
		/// current cursor.
		/// </summary>
		public bool MoreResultsAvailable { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether the initial search request has been issued.
		/// </summary>
		public bool SearchStarted { get; internal set; }

		/// <summary>
		/// Gets or sets the cached KQL <c>path:</c> clause used to scope the search to the
		/// folder referenced by <c>DocumentBucket.UploadPath</c>. Populated on the first page
		/// request and reused for subsequent pages.
		/// </summary>
		public string PathClause { get; internal set; }

		/// <summary>
		/// Checks if there are more pages left.
		/// </summary>
		/// <returns>
		///   <c>true</c> if there are more pages; otherwise, <c>false</c>.
		/// </returns>
		public override bool HasNextPage()
		{
			return !SearchStarted
				|| MoreResultsAvailable
				|| PageRemainderBuffer.Count > 0;
		}
	}
}
