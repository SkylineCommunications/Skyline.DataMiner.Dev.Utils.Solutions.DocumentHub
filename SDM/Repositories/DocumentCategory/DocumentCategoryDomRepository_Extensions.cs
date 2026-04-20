namespace Skyline.DataMiner.Solutions.DocumentHub.SDM
{
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

    public static class DocumentCategoryDomRepository_Extensions
    {

        public static IBulkRepository<DocumentCategory> WithMiddleware(
            this IBulkRepository<DocumentCategory> repository,
            IMiddlewareMarker<DocumentCategory> middleware)
        {
            return new DocumentCategoryDomRepository_Middleware(repository, middleware);
        }
    }
}
