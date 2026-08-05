namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Configurations
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;

	/// <summary>
	/// Represents the configuration options for reading files, including paging context, file name filtering, and the set
	/// of DOM instance identifiers to retrieve attachments for.
	/// </summary>
	public class ReadFilesConfiguration
	{
		/// <summary>
		/// Gets or sets the optional paging context that maintains paging state between calls.
		/// </summary>
		public DocHubPageData Context { get; set; }

		/// <summary>
		/// Gets or sets the optional case-insensitive filter applied to file names.
		/// </summary>
		public string Filter { get; set; }

		/// <summary>
		/// Gets or sets the DOM instance identifiers whose attachments should be retrieved.
		/// </summary>
		public IEnumerable<Guid> DomInstanceIds { get; set; }
	}
}
