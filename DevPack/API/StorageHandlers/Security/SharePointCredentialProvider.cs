namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.IO;
	using Microsoft.Identity.Client;
	using Microsoft.Kiota.Abstractions.Authentication;

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
		/// Creates a client-secret authentication provider for the given app registration.
		/// </summary>
		/// <param name="tenantId">The tenant identifier.</param>
		/// <param name="clientId">The application identifier.</param>
		/// <param name="clientSecret">The candidate secret, consumed without saving or exposing it.</param>
		/// <returns>A Task-based Graph authentication provider backed by MSAL.</returns>
		internal static IAuthenticationProvider CreateCredential(string tenantId, string clientId, string clientSecret)
		{
			SharePointSecrets.ValidateIds(tenantId, clientId);
			if (string.IsNullOrEmpty(clientSecret))
			{
				throw new ArgumentException("The client secret cannot be null or empty.", nameof(clientSecret));
			}

			var application = ConfidentialClientApplicationBuilder.Create(clientId)
				.WithTenantId(tenantId)
				.WithClientSecret(clientSecret)
				.WithClientCapabilities(new[] { "cp1" })
				.Build();

			return new BaseBearerTokenAuthenticationProvider(new SharePointAccessTokenProvider(application));
		}

		/// <summary>
		/// Creates a client-secret authentication provider using the saved secret.
		/// </summary>
		/// <param name="tenantId">The tenant identifier.</param>
		/// <param name="clientId">The application identifier.</param>
		/// <returns>A provider that acquires tokens without exposing the secret.</returns>
		/// <exception cref="ArgumentException">Thrown for invalid identifiers.</exception>
		/// <exception cref="FileNotFoundException">Thrown when no saved or legacy secret exists.</exception>
		/// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when decryption fails.</exception>
		internal IAuthenticationProvider CreateCredential(string tenantId, string clientId)
		{
			return CreateCredential(tenantId, clientId, GetSecret(SharePointSecrets.ClientSecret(tenantId, clientId)));
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
