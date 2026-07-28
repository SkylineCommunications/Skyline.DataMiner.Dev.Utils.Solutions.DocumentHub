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
	}
}
