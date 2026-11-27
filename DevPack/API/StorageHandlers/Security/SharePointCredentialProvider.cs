namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using Microsoft.Identity.Client;
	using Microsoft.Kiota.Abstractions.Authentication;

	/// <summary>
	/// Creates the Azure AD credential used to authenticate against a SharePoint configuration.
	/// </summary>
	/// <remarks>
	/// The caller supplies the authorized native retrieval mechanism. There is no file-store fallback.
	/// </remarks>
	internal sealed class SharePointCredentialProvider
	{
		private readonly Func<string> _getSecret;

		internal SharePointCredentialProvider(Func<string> getSecret)
		{
			_getSecret = getSecret ?? throw new ArgumentNullException(nameof(getSecret));
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
		internal IAuthenticationProvider CreateCredential(string tenantId, string clientId)
		{
			return CreateCredential(tenantId, clientId, _getSecret());
		}
	}
}
