namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using Azure.Identity;
	using Skyline.DataMiner.Net;
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
		/// <summary>
		/// Name of the Automation script executed on every agent to save or delete a secret locally.
		/// The script only needs to call <see cref="ApplyOnThisAgent"/> with its parameters.
		/// </summary>
		public const string ScriptName = "SLC-DH-AS-SaveClientSecret";

		/// <summary>Action value that saves the secret.</summary>
		public const string SaveAction = "Save";

		/// <summary>Action value that deletes the secret.</summary>
		public const string DeleteAction = "Delete";

		private readonly ISecretStore _store;
		private readonly IAgentSecretDistributor _distributor;

		internal Secrets(IConnection connection)
			: this(new DpapiFileSecretStore(), new AgentSecretDistributor(connection))
		{
		}

		internal Secrets(ISecretStore store, IAgentSecretDistributor distributor = null)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			_distributor = distributor;
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
		/// Saves the Azure AD client secret of a SharePoint app registration on every running agent of the cluster, replacing any existing one.
		/// </summary>
		/// <remarks>
		/// Secrets are encrypted per machine, so the script <see cref="ScriptName"/> is executed on each agent to save it locally.
		/// Agents that are not running are skipped and reported in the result; repeat the save once they are back.
		/// </remarks>
		/// <param name="tenantId">The Azure AD tenant identifier, as stored in the SharePoint configuration.</param>
		/// <param name="clientId">The client (application) identifier, as stored in the SharePoint configuration.</param>
		/// <param name="clientSecret">The client secret.</param>
		/// <returns>The result per agent.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID, or <paramref name="clientSecret"/> is null or empty.
		/// </exception>
		/// <exception cref="InvalidOperationException">Thrown when the script <see cref="ScriptName"/> does not exist.</exception>
		public ClusterSecretResult SaveSharePointClientSecret(string tenantId, string clientId, string clientSecret)
		{
			if (String.IsNullOrEmpty(clientSecret))
			{
				throw new ArgumentException("The client secret cannot be null or empty.", nameof(clientSecret));
			}

			SharePointSecrets.ValidateIds(tenantId, clientId);
			return GetDistributor().Execute(SaveAction, tenantId, clientId, clientSecret);
		}

		/// <summary>
		/// Deletes the client secret of the SharePoint app registration on every running agent of the cluster.
		/// </summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <returns>The result per agent.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> or <paramref name="clientId"/> is not a valid GUID.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the script <see cref="ScriptName"/> does not exist.</exception>
		public ClusterSecretResult DeleteSharePointClientSecret(string tenantId, string clientId)
		{
			SharePointSecrets.ValidateIds(tenantId, clientId);
			return GetDistributor().Execute(DeleteAction, tenantId, clientId, null);
		}

		/// <summary>
		/// Saves or deletes the secret on the local agent only. Called by the script <see cref="ScriptName"/> on each agent.
		/// </summary>
		/// <param name="action"><see cref="SaveAction"/> or <see cref="DeleteAction"/>.</param>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <param name="clientSecret">The client secret; ignored for <see cref="DeleteAction"/>.</param>
		/// <exception cref="ArgumentException">Thrown when an argument is invalid or the action is unknown.</exception>
		public void ApplyOnThisAgent(string action, string tenantId, string clientId, string clientSecret)
		{
			var key = SharePointSecrets.ClientSecret(tenantId, clientId);

			if (String.Equals(action, DeleteAction, StringComparison.OrdinalIgnoreCase))
			{
				_store.Delete(key);
			}
			else if (String.IsNullOrEmpty(action) || String.Equals(action, SaveAction, StringComparison.OrdinalIgnoreCase))
			{
				if (String.IsNullOrEmpty(clientSecret))
				{
					throw new ArgumentException("The client secret cannot be null or empty.", nameof(clientSecret));
				}

				_store.Save(key, clientSecret);
			}
			else
			{
				throw new ArgumentException($"Unknown action '{action}'. Expected '{SaveAction}' or '{DeleteAction}'.", nameof(action));
			}
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

		private IAgentSecretDistributor GetDistributor()
		{
			return _distributor ?? throw new InvalidOperationException("No DataMiner connection is available to reach the other agents.");
		}
	}
}
