namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.Repositories.DocumentCategory;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.DomSource;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.SharePointConfiguration;

	/// <summary>
	/// Provides centralized access to Document Hub repositories and configuration data.
	/// </summary>
	/// <remarks>
	/// This helper class centralizes access to document categories, DOM sources, and SharePoint
	/// configuration repositories. It simplifies data access patterns for consumers of the Document Hub API.
	/// </remarks>
	public class DocumentHubApiHelper : IDocumentHubApiHelper
    {
        private readonly IBulkRepository<DocumentCategory> _documentCategories;
        private readonly IRepository<DomSource> _domSources;
        private readonly IRepository<SharePointConfiguration> _sharePointConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentHubApiHelper"/> class using the specified connection.
        /// </summary>
        /// <param name="connection">The <see cref="IConnection"/> to use for data repository access.</param>
        public DocumentHubApiHelper(IConnection connection)
        {
            Connection = connection;
            _documentCategories = new DocumentCategoryDomRepository(connection);
            _domSources = new DomSourceDomRepository(connection);
            _sharePointConfigurations = new SharePointConfigurationDomRepository(connection);
        }

        /// <inheritdoc/>
        public IConnection Connection { get; }

        /// <inheritdoc/>
        public IBulkRepository<DocumentCategory> DocumentCategories
        {
            get
            {
                return _documentCategories;
            }
        }

        /// <inheritdoc/>
        public IRepository<DomSource> DomSources
        {
            get
            {
                return _domSources;
            }
        }

        /// <inheritdoc/>
        public IRepository<SharePointConfiguration> SharePointConfigurations
        {
            get
            {
                return _sharePointConfigurations;
            }
        }
    }
}