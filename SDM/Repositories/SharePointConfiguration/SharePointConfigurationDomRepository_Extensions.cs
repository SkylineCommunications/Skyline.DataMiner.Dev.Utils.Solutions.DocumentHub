namespace Skyline.DataMiner.Solutions.DocumentHub.SDM
{
    using Skyline.DataMiner.SDM;

    public static class SharePointConfigurationDomRepository_Extensions
    {

        public static IBulkRepository<Models.SharePointConfiguration> WithMiddleware(
            this IBulkRepository<Models.SharePointConfiguration> repository,
            IMiddlewareMarker<Models.SharePointConfiguration> middleware)
        {
            return new SharePointConfigurationDomRepository_Middleware(repository, middleware);
        }
    }
}
