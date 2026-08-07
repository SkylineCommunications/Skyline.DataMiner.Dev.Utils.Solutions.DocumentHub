namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using Microsoft.Graph.Models;

	/// <summary>
	/// Paging context used when searching files in SharePoint storage via the Microsoft Graph
	/// <c>POST /search/query</c> endpoint.
	/// </summary>
	/// <remarks>
	/// The Microsoft Search endpoint uses cursor-based paging via <see cref="From"/> and
	/// <see cref="DocHubPageData.PageSize"/> (as <c>size</c>) instead of an <c>@odata.nextLink</c>.
	/// The <see cref="ScopingClause"/> and <see cref="AllowedParentIds"/> set are derived once
	/// from <c>DocumentBucket.UploadPath</c> and re-used across pages.
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
		/// Gets or sets the cached KQL scoping clause (currently a <c>site:</c> refiner)
		/// applied server-side. Populated on the first page request and reused for subsequent
		/// pages.
		/// </summary>
		public string ScopingClause { get; internal set; }

		/// <summary>
		/// Gets or sets the set of driveItem ids that represent the bucket's UploadPath folder
		/// and every folder nested underneath it. Search hits whose
		/// <c>parentReference.id</c> is not in this set are filtered out client-side so the
		/// final result set stays scoped to the bucket folder subtree.
		/// </summary>
		public HashSet<string> AllowedParentIds { get; internal set; }

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
