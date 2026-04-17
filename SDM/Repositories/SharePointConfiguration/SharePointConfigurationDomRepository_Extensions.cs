namespace Skyline.DataMiner.SDM
{
    using Skyline.DataMiner.SDM.Middleware;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.SDM;

    public static class SharePointConfigurationDomRepository_Extensions
    {

        public static Skyline.DataMiner.SDM.IBulkRepository<Skyline.DataMiner.DocumentHub.SDM.Models.SharePointConfiguration> WithMiddleware(
            this Skyline.DataMiner.SDM.IBulkRepository<Skyline.DataMiner.DocumentHub.SDM.Models.SharePointConfiguration> repository,
            IMiddlewareMarker<Skyline.DataMiner.DocumentHub.SDM.Models.SharePointConfiguration> middleware)
        {
            return new SharePointConfigurationDomRepository_Middleware(repository, middleware);
        }
    }
}
