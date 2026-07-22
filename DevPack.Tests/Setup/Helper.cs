namespace Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

	public static class Helper
	{
		public static IDocumentHubApiHelper GetHelper()
		{
			return ConnectionHelper.CreateConnection().GetMockedHelper();
		}

		public static IDocumentHubApiHelper GetHelper(IConnection connection)
		{
			return connection.GetMockedHelper();
		}
	}
}