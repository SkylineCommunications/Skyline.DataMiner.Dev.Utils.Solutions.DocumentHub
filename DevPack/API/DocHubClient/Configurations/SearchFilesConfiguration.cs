namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Configurations
{
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;

	/// <summary>
	/// Represents the configuration options for executing a search against a storage backend
	/// that supports search (currently SharePoint via Microsoft Graph KQL).
	/// </summary>
	public class SearchFilesConfiguration : IPageableConfiguration
	{
		// Required by Microsoft Graph /search/query when the request is issued with an application-only token.
		// Set to "EMEA" by default.
		public string Region => "EMEA";

		/// <inheritdoc/>
		public DocHubPageData Context { get; set; }
	}
}
