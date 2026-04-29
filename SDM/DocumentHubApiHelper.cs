namespace Skyline.DataMiner.Solutions.DocumentHub.SDM
{
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

    /// <summary>
    /// Provides access to repositories and configuration data for the Document Hub API.
    /// </summary>
    /// <remarks>This helper class centralizes access to document categories, DOM sources, and SharePoint
    /// configuration repositories.. It is intended to simplify data access patterns for
    /// consumers of the Document Hub API.</remarks>
    public class DocumentHubApiHelper : IDocumentHubApiHelper
    {
        private readonly IBulkRepository<DocumentCategory> _documentCategories;
        private readonly IRepository<DomSource> _domSources;
        private readonly IRepository<SharePointConfiguration> _sharePointConfigurations;

        /// <summary>
        /// Initializes a new instance of the DocumentHubApiHelper class using the specified connection.
        /// </summary>
        /// <param name="connection">The <see cref="IConnection"/> to use for data repository access.</param>
        public DocumentHubApiHelper(IConnection connection)
        {
            Connection = connection;
            _documentCategories = new DocumentCategoryDomRepository(connection);
            _domSources = new DomSourceDomRepository(connection);
            _sharePointConfigurations = new SharePointConfigurationDomRepository(connection);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConnection Connection { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IBulkRepository<DocumentCategory> DocumentCategories 
        {
            get
            {
                return _documentCategories;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IRepository<DomSource> DomSources 
        {
            get
            {
                return _domSources;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IRepository<SharePointConfiguration> SharePointConfigurations
        {
            get
            {
                return _sharePointConfigurations;
            }
        }
    }
}