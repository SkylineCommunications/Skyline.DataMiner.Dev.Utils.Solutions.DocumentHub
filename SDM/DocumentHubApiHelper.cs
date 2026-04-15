namespace Skyline.DataMiner.DocumentHub.SDM
{
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.SDM;
    using System;

    internal class DocumentHubApiHelper : IDocumentHubApiHelper
    {
        private readonly IBulkRepository<DocumentCategory> _documentCategories;
        private readonly IBulkRepository<DomSource> _domSources;
        private bool _disposed;

        public DocumentHubApiHelper(IConnection connection)
        {
            Connection = connection;
            _documentCategories = new DocumentCategoryDomRepository(connection);
        }

        public IConnection Connection { get; }

        public IBulkRepository<DocumentCategory> DocumentCategories 
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DocumentHubApiHelper));
                }

                return _documentCategories;
            }
        }

        public IBulkRepository<DomSource> DomSources 
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DocumentHubApiHelper));
                }

                return _domSources;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
