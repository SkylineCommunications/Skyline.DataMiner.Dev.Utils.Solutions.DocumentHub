namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.ManagerStore;

    /// <summary>
    /// Provides paging state and tracking information for reading DOM module data across multiple pages.
    /// </summary>
    public class DOMPageData : DocHubPageData
	{
		internal DOMPageData()
		{
		}

		/// <summary>
		/// Persistent paging helper for the current module
		/// </summary>
		internal PagingHelper<DomInstance> PagingHelper { get; set; }

		/// <summary>
		/// List of modules to read.
		/// </summary>
		internal List<string> Modules { get; set; }

		/// <summary>
		/// Current module index in the Modules list.
		/// </summary>
		internal int ModuleIndex { get; set; }

		/// <summary>
		/// Index of the current instance within the current DOM page.
		/// </summary>
		internal int InstanceIndexInPage { get; set; }

		/// <summary>
		/// Index of the next attachment to return in the current instance.
		/// </summary>
		internal int AttachmentIndex { get; set; }

		/// <summary>
		/// Current instance ID whose attachments are cached.
		/// </summary>
		internal Guid CurrentInstanceId { get; set; }

		/// <summary>
		/// Cached attachments for the current instance.
		/// </summary>
		internal List<string> CurrentAttachments { get; set; }

		/// <summary>
		/// True if all modules have been fully read.
		/// </summary>
		internal bool Done { get; set; }

		/// <summary>
		/// Determines whether there is a subsequent page of results available for retrieval.
		/// </summary>
		/// <returns>
		/// True if another page of results can be fetched; otherwise, false.
		/// </returns>
		public override bool HasNextPage() => !Done;

		/// <summary>
		/// Resets the page data for a fresh read.
		/// </summary>
		internal void Reset()
		{
			PagingHelper = null;
			Modules = null;
			ModuleIndex = 0;
			InstanceIndexInPage = 0;
			AttachmentIndex = 0;
			CurrentInstanceId = Guid.Empty;
			CurrentAttachments = null;
			Done = false;
		}
	}
}