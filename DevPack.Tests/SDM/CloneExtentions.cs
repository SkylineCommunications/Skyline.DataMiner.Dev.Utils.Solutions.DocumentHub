namespace DevPack.Tests.SDM
{
	using Newtonsoft.Json;
	using Skyline.DataMiner.SDM;

	internal static class CloneExtensions
	{
		public static T Clone<T>(this T source) where T : SdmObject<T>
		{
			var serialized = JsonConvert.SerializeObject(source);

			return JsonConvert.DeserializeObject<T>(serialized);
		}
	}
}
