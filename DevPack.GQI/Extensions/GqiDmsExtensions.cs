namespace Skyline.DataMiner.Solutions.DocumentHub.GQI
{
    using System;

    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
    using Skyline.DataMiner.Solutions.DocumentHub.API.Extensions;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Extensions;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

    /// <summary>
    /// Defines extension methods on the <see cref="GqiDmsExtensions"/> class.
    /// </summary>
    public static class GqiDmsExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IDocumentHubApiHelper"/> interface.
        /// </summary>
        /// <param name="dms">The <see cref="GQIDMS"/> instance.</param>
        /// <returns>Instance of the <see cref="IDocumentHubApiHelper"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dms"/> is <see langword="null" />.</exception>
        public static IDocumentHubApiHelper GetDocumentHubApiHelper(this GQIDMS dms)
        {
            if (dms == null)
            {
                throw new ArgumentNullException(nameof(dms), "DMS cannot be null.");
            }

            return dms.GetConnection().GetDocumentHubApiHelper();
        }

        /// <summary>
        /// Retrieves an instance of the <see cref="DocHubClient"/> interface.
        /// </summary>
        /// <param name="dms">The <see cref="GQIDMS"/> instance.</param>
        /// <returns>Instance of the <see cref="DocHubClient"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dms"/> is <see langword="null" />.</exception>
        public static DocHubClient GetDocHubClient(this GQIDMS dms)
        {
            if (dms is null)
            {
                throw new ArgumentNullException(nameof(dms), "DMS cannot be null.");
            }

            return dms.GetConnection().GetDocHubClient();
        }
    }
}