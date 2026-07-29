namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;

	/// <summary>
	/// Represents the configuration options for executing a search against a storage backend
	/// that supports search (currently SharePoint via Microsoft Graph KQL).
	/// </summary>
	public class SearchFilesConfiguration
	{
		/// <summary>
		/// Gets or sets the optional paging context that maintains paging state between calls.
		/// Reuse the same instance to continue paging through subsequent result pages.
		/// </summary>
		public DocHubPageData Context { get; set; }

		/// <summary>
		/// Gets or sets the Microsoft Search region (e.g. <c>"EUR"</c>, <c>"NAM"</c>, <c>"APC"</c>).
		/// </summary>
		/// <remarks>
		/// Required by Microsoft Graph <c>/search/query</c> when the request is issued with an
		/// application-only token (which is the case for SharePoint here, because the handler
		/// authenticates via <c>ClientSecretCredential</c>). When left <c>null</c> the Graph
		/// endpoint will return <c>400 Bad Request</c>.
		/// </remarks>
		public string Region { get; set; }
	}
}
