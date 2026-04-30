namespace Skyline.DataMiner.Solutions.DocumentHub.API.Paging
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Paging context used when reading files from local file system storage.
    /// This context maintains the file enumeration state across paging calls.
    /// </summary>
    public class LocalPageData : DocHubPageData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LocalPageData"/> class.
		/// LocalPageData constructor is internal.
		/// LocalPageData is created by factory.
		/// </summary>
		internal LocalPageData()
		{
		}

		/// <summary>
		/// Gets enumerator used to iterate through files in the current directory tree.
		/// The enumerator position is preserved between paging calls.
		/// </summary>
		public IEnumerator<FileInfo> FileEnumerator { get; internal set; }

		/// <summary>
		/// Gets the root directory currently being enumerated.
		/// Used to detect changes in scope and reinitialize enumeration when needed.
		/// </summary>
		public string CurrentRoot { get; internal set; } = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";

		/// <summary>
		/// Checks if theres more pages left.
		/// </summary>
		/// <returns>
		///   <c>true</c> if there are more pages; otherwise, <c>false</c>.
		/// </returns>
		public override bool HasNextPage()
		{
			return FileEnumerator != null && FileEnumerator.MoveNext();
		}
	}
}