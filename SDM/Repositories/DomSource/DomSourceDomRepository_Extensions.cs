namespace Skyline.DataMiner.SDM
{
    using Skyline.DataMiner.Solutions.DocumentHub.SDM;

    public static class DomSourceDomRepository_Extensions
    {

        public static IBulkRepository<Solutions.DocumentHub.SDM.Models.DomSource> WithMiddleware(
            this IBulkRepository<Solutions.DocumentHub.SDM.Models.DomSource> repository,
            IMiddlewareMarker<Solutions.DocumentHub.SDM.Models.DomSource> middleware)
        {
            return new DomSourceDomRepository_Middleware(repository, middleware);
        }
    }
}
