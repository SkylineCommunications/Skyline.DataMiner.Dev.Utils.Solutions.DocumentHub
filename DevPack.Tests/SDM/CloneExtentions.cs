namespace DevPack.Tests.SDM
{
	using System;
	using Newtonsoft.Json;
	using Skyline.DataMiner.SDM;

	internal static class CloneExtensions
	{
		public static T Clone<T>(this T source) where T : SdmObject<T>
		{
			string serialized = JsonConvert.SerializeObject(source);

			return JsonConvert.DeserializeObject<T>(serialized)
				?? throw new InvalidOperationException("Cloning failed - deserialization returned null.");
		}
	}
}
