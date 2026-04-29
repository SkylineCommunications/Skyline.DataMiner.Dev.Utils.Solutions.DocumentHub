namespace Skyline.DataMiner.Utils.DocumentHub.Tests.Setup
{
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

	public static class Helper
	{
		public static IDocumentHubApiHelper GetHelper()
		{
			return ConnectionHelper.CreateConnection().GetMockedHelper();
		}
	}
}