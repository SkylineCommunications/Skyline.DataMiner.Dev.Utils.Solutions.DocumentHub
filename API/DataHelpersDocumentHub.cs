using Skyline.DataMiner.Net;
using Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub;
using System;
using System.Collections.Generic;
using System.Text;

namespace Skyline.DataMiner.Utils.DocumentHub.API
{
    internal sealed class DataHelpersDocumentHub
    {
        internal DataHelpersDocumentHub(IConnection connection)
        {
            DocumentCategories = new DataHelperCategory(connection);
            SharePointConfigurations = new DataHelperSharePointConfiguration(connection);
        }

        internal DataHelperCategory DocumentCategories { get; }

        internal DataHelperSharePointConfiguration SharePointConfigurations { get; }
    }
}
