namespace Skyline.DataMiner.Utils.DocumentHub.API.StorageHandlers.Security
{
	using System;
	using System.Security.Cryptography;
	using System.Text;

	internal static class DPAPIHelper
	{
		internal static string Decrypt(string encryptedText)
		{
			byte[] encryptedData = Convert.FromBase64String(encryptedText);
			byte[] decrypted = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
			return Encoding.UTF8.GetString(decrypted);
		}
	}
}