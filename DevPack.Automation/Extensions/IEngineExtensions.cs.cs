namespace Skyline.DataMiner.Solutions.DocumentHub.Automation
{
    using System;

    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
    using Skyline.DataMiner.Solutions.DocumentHub.API.Extensions;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Extensions;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

    /// <summary>
    /// Defines extension methods on the <see cref="IEngine"/> interface.
    /// </summary>
    public static class IEngineExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IDocumentHubApiHelper"/> interface.
        /// </summary>
        /// <param name="engine">The <see cref="IEngine"/> implementation.</param>
        /// <returns>Instance of the <see cref="IDocumentHubApiHelper"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null" />.</exception>
        public static IDocumentHubApiHelper GetDocumentHubApiHelper(this IEngine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine), "Engine cannot be null.");
            }

            return engine.GetUserConnection().GetDocumentHubApiHelper();
        }

        /// <summary>
        /// Retrieves an instance of the <see cref="DocHubClient"/> interface.
        /// </summary>
        /// <param name="engine">The <see cref="IEngine"/> instance.</param>
        /// <returns>Instance of the <see cref="DocHubClient"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null" />.</exception>
        public static DocHubClient GetDocHubClient(this IEngine engine)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine), "Engine cannot be null.");
            }

            return engine.GetUserConnection().GetDocHubClient();
        }
    }
}