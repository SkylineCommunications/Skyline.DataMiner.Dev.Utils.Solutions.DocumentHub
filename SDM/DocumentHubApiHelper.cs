namespace Skyline.DataMiner.Solutions.DocumentHub.SDM
{
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

    internal class DocumentHubApiHelper : IDocumentHubApiHelper
    {
        private readonly IBulkRepository<DocumentCategory> _documentCategories;
        private readonly IRepository<DomSource> _domSources;
        private readonly IRepository<SharePointConfiguration> _sharePointConfigurations;

        public DocumentHubApiHelper(IConnection connection)
        {
            Connection = connection;
            _documentCategories = new DocumentCategoryDomRepository(connection);
            _domSources = new DomSourceDomRepository(connection);
            _sharePointConfigurations = new SharePointConfigurationDomRepository(connection);
        }

        public IConnection Connection { get; }

        public IBulkRepository<DocumentCategory> DocumentCategories 
        {
            get
            {
                return _documentCategories;
            }
        }

        public IRepository<DomSource> DomSources 
        {
            get
            {
                return _domSources;
            }
        }

        public IRepository<SharePointConfiguration> SharePointConfigurations
        {
            get
            {
                return _sharePointConfigurations;
            }
        }
    }
}
