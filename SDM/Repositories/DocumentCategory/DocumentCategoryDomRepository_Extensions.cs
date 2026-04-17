
namespace Skyline.DataMiner.SDM
{
    using Skyline.DataMiner.SDM.Middleware;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.SDM;

    public static class DocumentCategoryDomRepository_Extensions
    {

        public static Skyline.DataMiner.SDM.IBulkRepository<Skyline.DataMiner.DocumentHub.SDM.Models.DocumentCategory> WithMiddleware(
            this Skyline.DataMiner.SDM.IBulkRepository<Skyline.DataMiner.DocumentHub.SDM.Models.DocumentCategory> repository,
            IMiddlewareMarker<Skyline.DataMiner.DocumentHub.SDM.Models.DocumentCategory> middleware)
        {
            return new DocumentCategoryDomRepository_Middleware(repository, middleware);
        }
    }
}
