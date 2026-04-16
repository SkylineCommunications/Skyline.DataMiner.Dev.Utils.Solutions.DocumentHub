namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using Skyline.DataMiner.Net;

	internal sealed class DataHelpersDocumentHub
	{
		internal DataHelpersDocumentHub(IConnection connection)
		{
			SharePointConfigurations = new DataHelperSharePointConfiguration(connection);
		}

		internal DataHelperSharePointConfiguration SharePointConfigurations { get; }
	}
}
