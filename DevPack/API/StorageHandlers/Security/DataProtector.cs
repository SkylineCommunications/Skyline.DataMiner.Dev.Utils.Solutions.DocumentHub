namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Security.Cryptography;
	using System.Text;

	internal static class DPAPIHelper
	{
		internal static string Encrypt(string plainText, byte[] entropy = null)
		{
			byte[] data = Encoding.UTF8.GetBytes(plainText);
			byte[] encrypted = ProtectedData.Protect(data, entropy, DataProtectionScope.LocalMachine);
			return Convert.ToBase64String(encrypted);
		}

		internal static string Decrypt(string encryptedText, byte[] entropy = null)
		{
			byte[] encryptedData = Convert.FromBase64String(encryptedText);
			byte[] decrypted = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.LocalMachine);
			return Encoding.UTF8.GetString(decrypted);
		}
	}
}