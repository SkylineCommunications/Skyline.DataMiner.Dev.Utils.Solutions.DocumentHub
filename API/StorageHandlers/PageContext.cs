using Microsoft.Graph;
using System.Collections.Generic;
using System.IO;

namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers
{
    /// <summary>
    /// Represents the base paging context used when reading files from a storage backend.
    /// The context instance stores paging state and must be reused between calls
    /// to continue retrieving subsequent pages.
    /// </summary>
    public abstract class PageContext
    {
        /// <summary>
        /// The maximum number of files returned per page.
        /// Defaults to 200 if not explicitly set.
        /// </summary>
        public int PageSize { get; set; } = 200;
    }

    /// <summary>
    /// Paging context used when reading files from SharePoint storage.
    /// This context keeps track of the folder traversal state
    /// and the SharePoint Graph paging request.
    /// </summary>
    public class SharePointPageContext : PageContext
    {
        /// <summary>
        /// Queue containing the identifiers of folders that still need to be traversed.
        /// This is used internally to recursively enumerate folders across pages.
        /// </summary>
        public Queue<string> FolderQueue { get; internal set; } = new Queue<string>(new[] { "root" });

        /// <summary>
        /// The Graph API request used to retrieve the next page of drive items.
        /// When null, a new folder traversal request will be started.
        /// </summary>
        public IDriveItemChildrenCollectionRequest NextPageRequest { get; internal set; }
    }

    /// <summary>
    /// Paging context used when reading files from local file system storage.
    /// This context maintains the file enumeration state across paging calls.
    /// </summary>
    public class LocalPageContext : PageContext
    {
        /// <summary>
        /// Enumerator used to iterate through files in the current directory tree.
        /// The enumerator position is preserved between paging calls.
        /// </summary>
        public IEnumerator<FileInfo> FileEnumerator { get; internal set; }

        /// <summary>
        /// The root directory currently being enumerated.
        /// Used to detect changes in scope and reinitialize enumeration when needed.
        /// </summary>
        public string CurrentRoot { get; internal set; } = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";
    }
}
