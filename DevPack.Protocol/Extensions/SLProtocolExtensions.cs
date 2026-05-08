namespace Skyline.DataMiner.Solutions.DocumentHub.Protocol
{
    using System;
    using Skyline.DataMiner.Scripting;
    using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
    using Skyline.DataMiner.Solutions.DocumentHub.API.Extensions;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Extensions;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

    /// <summary>
    /// Defines extension methods on the <see cref="SLProtocolExtensions"/> class.
    /// </summary>
    public static class SLProtocolExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IDocumentHubApiHelper"/> interface.
        /// </summary>
        /// <param name="protocol">The <see cref="SLProtocol"/> instance.</param>
        /// <returns>Instance of the <see cref="IDocumentHubApiHelper"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="protocol"/> is <see langword="null" />.</exception>
        public static IDocumentHubApiHelper GetDocumentHubApiHelper(this SLProtocol protocol)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol), "Protocol cannot be null.");
            }

            return protocol.GetUserConnection().GetDocumentHubApiHelper();
        }

        /// <summary>
        /// Retrieves an instance of the <see cref="DocHubClient"/> interface.
        /// </summary>
        /// <param name="protocol">The <see cref="SLProtocol"/> instance.</param>
        /// <returns>Instance of the <see cref="DocHubClient"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="protocol"/> is <see langword="null" />.</exception>
        public static DocHubClient GetDocHubClient(this SLProtocol protocol)
        {
            if (protocol is null)
            {
                throw new ArgumentNullException(nameof(protocol), "Protocol cannot be null.");
            }

            return protocol.GetUserConnection().GetDocHubClient();
        }
    }
}