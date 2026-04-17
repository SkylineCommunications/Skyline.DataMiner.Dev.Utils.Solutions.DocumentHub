
namespace Skyline.DataMiner.SDM
{
    using Skyline.DataMiner.SDM.Middleware;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.SDM;

    public static class DomSourceDomRepository_Extensions
    {

        public static Skyline.DataMiner.SDM.IBulkRepository<Skyline.DataMiner.DocumentHub.SDM.Models.DomSource> WithMiddleware(
            this Skyline.DataMiner.SDM.IBulkRepository<Skyline.DataMiner.DocumentHub.SDM.Models.DomSource> repository,
            IMiddlewareMarker<Skyline.DataMiner.DocumentHub.SDM.Models.DomSource> middleware)
        {
            return new DomSourceDomRepository_Middleware(repository, middleware);
        }
    }
}
