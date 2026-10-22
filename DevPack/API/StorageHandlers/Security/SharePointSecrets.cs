namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;

	/// <summary>
	/// Declares the secrets required by the SharePoint storage type and builds the <see cref="SecretKey"/> for each of them.
	/// </summary>
	/// <remarks>
	/// Both the code that saves a secret (<c>Secrets</c>) and the code that uses it (<c>SharePointCredentialProvider</c>)
	/// must use these helpers, so they always agree on the key and file location.
	/// Other storage types that need credentials should get an equivalent static class.
	/// </remarks>
	internal static class SharePointSecrets
	{
		private const string Source = "sharepoint";

		/// <summary>
		/// Creates the key of the Azure AD client secret of an app registration.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier registered in Azure AD.</param>
		/// <returns>
		/// A <see cref="SecretKey"/> with source <c>sharepoint</c>, scope <paramref name="tenantId"/>
		/// and name <paramref name="clientId"/>, which maps to <c>sharepoint\&lt;tenantId&gt;\&lt;clientId&gt;.dat</c> in the file-based store.
		/// One file per app registration, so a tenant can have several.
		/// </returns>
		internal static SecretKey ClientSecret(Guid tenantId, Guid clientId) => new SecretKey(Source, clientId.ToString("D"), tenantId.ToString("D"));

		/// <summary>
		/// Parses the tenant and client identifiers as stored in a SharePoint configuration and creates the key of its client secret.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier, as a GUID string.</param>
		/// <param name="clientId">The client (application) identifier, as a GUID string.</param>
		/// <returns>The key of the client secret.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID.</exception>
		internal static SecretKey ClientSecret(string tenantId, string clientId)
		{
			if (!Guid.TryParse(tenantId, out Guid tenant) || tenant == Guid.Empty)
			{
				throw new ArgumentException("The tenant ID must be a valid GUID.", nameof(tenantId));
			}

			if (!Guid.TryParse(clientId, out Guid client) || client == Guid.Empty)
			{
				throw new ArgumentException("The client ID must be a valid GUID.", nameof(clientId));
			}

			return ClientSecret(tenant, client);
		}
	}
}
