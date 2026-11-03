namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.IO;
	using Azure.Core;
	using Azure.Identity;

	/// <summary>
	/// Creates the Azure AD credential used to authenticate against a SharePoint configuration.
	/// </summary>
	/// <remarks>
	/// The client secret is read and consumed inside this class only: it is handed straight to the credential and is
	/// never returned, so no other code (public or internal) can obtain the secret value through it.
	/// </remarks>
	internal sealed class SharePointCredentialProvider
	{
		private const string LegacySecretPath = @"C:\Skyline DataMiner\Security\DocumentHub\graphsecret.dat";

		private readonly ISecretReader _secretReader;

		/// <summary>
		/// Initializes a new instance of the <see cref="SharePointCredentialProvider"/> class.
		/// </summary>
		/// <param name="secretReader">
		/// The store the client secret is read from. When <c>null</c>, a <see cref="DpapiFileSecretStore"/> using the default
		/// DocumentHub security folder is used.
		/// </param>
		internal SharePointCredentialProvider(ISecretReader secretReader = null)
		{
			_secretReader = secretReader ?? new DpapiFileSecretStore();
		}

		/// <summary>
		/// Creates a client-secret credential for the given app registration.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier, as stored in the SharePoint configuration.</param>
		/// <param name="clientId">The client (application) identifier, as stored in the SharePoint configuration.</param>
		/// <returns>A credential that can acquire tokens; it does not expose the secret.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID.</exception>
		/// <exception cref="FileNotFoundException">Thrown when the secret exists neither in the secret store nor in the legacy secret file.</exception>
		/// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when the secret exists but cannot be decrypted.</exception>
		internal TokenCredential CreateCredential(string tenantId, string clientId)
		{
			return new ClientSecretCredential(tenantId, clientId, GetSecret(SharePointSecrets.ClientSecret(tenantId, clientId)));
		}

		private string GetSecret(SecretKey key)
		{
			if (_secretReader.Exists(key))
			{
				return _secretReader.Read(key);
			}

			// Legacy fallback: single global secret file used before per-configuration secrets existed.
			if (!File.Exists(LegacySecretPath))
			{
				throw new FileNotFoundException($"Secret '{key}' not found.", LegacySecretPath);
			}

			return DPAPIHelper.Decrypt(File.ReadAllText(LegacySecretPath));
		}
	}
}
