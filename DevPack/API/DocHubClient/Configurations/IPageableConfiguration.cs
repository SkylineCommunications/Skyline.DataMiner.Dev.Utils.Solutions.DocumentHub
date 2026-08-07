// Ignore Spelling: Pageable

namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Configurations
{
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;

	/// <summary>
	/// Interface representing pageable API operations.
	/// </summary>
	public interface IPageableConfiguration
	{
		/// <summary>
		/// Gets the optional paging context that maintains paging state between calls.
		/// </summary>
		DocHubPageData Context { get; }
	}
}