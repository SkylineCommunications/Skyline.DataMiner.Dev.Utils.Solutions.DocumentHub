namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers
{
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

    /// <summary>
    /// Provides access to repositories for interacting with Document Hub data sources and configurations.
    /// </summary>
    public interface IDocumentHubApiHelper
    {
        /// <summary>
        /// Gets the underlying connection used for communication with the repositories.
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Gets the repository for <see cref="DocumentBucket"/> objects.
        /// </summary>
        IBulkRepository<DocumentBucket> DocumentBuckets { get; }

        /// <summary>
        /// Gets the repository for <see cref="DomSource"/> objects.
        /// </summary>
        IRepository<DomSource> DomSources { get; }

        /// <summary>
        /// Gets the repository for <see cref="SharePointConfiguration"/> objects.
        /// </summary>
        IRepository<SharePointConfiguration> SharePointConfigurations { get; }
    }
}