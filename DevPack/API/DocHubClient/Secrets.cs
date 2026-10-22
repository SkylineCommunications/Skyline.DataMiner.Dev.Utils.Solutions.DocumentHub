namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using Azure.Identity;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Manages the secrets DocumentHub needs to access external storage, such as the SharePoint client secret.
	/// </summary>
	/// <remarks>
	/// Secrets are write-only: they can be saved, replaced, checked and deleted, but never read back.
	/// They are stored encrypted on the DataMiner Agent (not in DOM) and are only used internally by DocumentHub
	/// to authenticate against the storage.
	/// </remarks>
	public class Secrets
	{
		private readonly ISecretStore _store;

		internal Secrets()
			: this(new DpapiFileSecretStore())
		{
		}

		internal Secrets(ISecretStore store)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
		}

		/// <summary>
		/// Checks whether the SharePoint configuration can log in with its saved client secret and reach its document library.
		/// The secret is used internally and never returned.
		/// </summary>
		/// <param name="sharePoint">The SharePoint configuration (tenant ID, client ID, site URL and document library name).</param>
		/// <returns>
		/// <c>true</c> if authentication succeeds and the document library exists; <c>false</c> if no secret is saved,
		/// the secret is wrong, or the site or library cannot be reached.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sharePoint"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the tenant ID or client ID is not a valid GUID.</exception>
		public bool IsSharePointConnected(SharePointConfiguration sharePoint)
		{
			if (sharePoint == null)
			{
				throw new ArgumentNullException(nameof(sharePoint));
			}

			if (!_store.Exists(SharePointSecrets.ClientSecret(sharePoint.TenantID, sharePoint.ClientID)))
			{
				return false;
			}

			var credential = new SharePointCredentialProvider(_store).CreateCredential(sharePoint.TenantID, sharePoint.ClientID);
			return SharePointConnectionTester.IsConnected(sharePoint, credential);
		}

		/// <summary>
		/// Checks whether the SharePoint configuration can log in with the given client secret, without saving it.
		/// Use this to validate a secret before calling <see cref="SaveSharePointClientSecret"/>.
		/// </summary>
		/// <param name="sharePoint">The SharePoint configuration (tenant ID, client ID, site URL and document library name).</param>
		/// <param name="clientSecret">The candidate client secret.</param>
		/// <returns><c>true</c> if authentication succeeds and the document library exists; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sharePoint"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="clientSecret"/> is null or empty.</exception>
		public bool IsSharePointConnected(SharePointConfiguration sharePoint, string clientSecret)
		{
			if (sharePoint == null)
			{
				throw new ArgumentNullException(nameof(sharePoint));
			}

			if (String.IsNullOrEmpty(clientSecret))
			{
				throw new ArgumentException("The client secret cannot be null or empty.", nameof(clientSecret));
			}

			var credential = new ClientSecretCredential(sharePoint.TenantID, sharePoint.ClientID, clientSecret);
			return SharePointConnectionTester.IsConnected(sharePoint, credential);
		}

		/// <summary>
		/// Saves the Azure AD client secret of a SharePoint app registration, replacing any existing one.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier, as stored in the SharePoint configuration.</param>
		/// <param name="clientId">The client (application) identifier, as stored in the SharePoint configuration.</param>
		/// <param name="clientSecret">The client secret.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID, or <paramref name="clientSecret"/> is null or empty.
		/// </exception>
		public void SaveSharePointClientSecret(string tenantId, string clientId, string clientSecret)
		{
			if (String.IsNullOrEmpty(clientSecret))
			{
				throw new ArgumentException("The client secret cannot be null or empty.", nameof(clientSecret));
			}

			_store.Save(SharePointSecrets.ClientSecret(tenantId, clientId), clientSecret);
		}

		/// <summary>
		/// Checks whether a client secret is saved for the SharePoint app registration. The secret itself is not returned.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <returns><c>true</c> if a secret is saved; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID.</exception>
		public bool HasSharePointClientSecret(string tenantId, string clientId)
		{
			return _store.Exists(SharePointSecrets.ClientSecret(tenantId, clientId));
		}

		/// <summary>
		/// Deletes the client secret of the SharePoint app registration. Does nothing if none is saved.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID.</exception>
		public void DeleteSharePointClientSecret(string tenantId, string clientId)
		{
			_store.Delete(SharePointSecrets.ClientSecret(tenantId, clientId));
		}
	}
}
