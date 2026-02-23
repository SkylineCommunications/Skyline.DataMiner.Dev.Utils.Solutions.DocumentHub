namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using Skyline.DataMiner.Net;

	internal sealed class DataHelpersDocumentHub
	{
		internal DataHelpersDocumentHub(IConnection connection)
		{
			DocumentCategories = new DataHelperCategory(connection);
			SharePointConfigurations = new DataHelperSharePointConfiguration(connection);
			DOMSources = new DataHelperDOMSource(connection);
		}

		internal DataHelperCategory DocumentCategories { get; }

		internal DataHelperSharePointConfiguration SharePointConfigurations { get; }

		internal DataHelperDOMSource DOMSources { get; set; }
	}
}
