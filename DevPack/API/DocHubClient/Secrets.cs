namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.SharePointConfiguration;

	/// <summary>Provisions native, source-scoped credentials. Secret values are never returned.</summary>
	public class Secrets
	{
		/// <summary>The native credential administration and explicit migration script.</summary>
		public const string ScriptName = "SLC-DH-AS-ManageSharePointSecret";

		/// <summary>Value of the script's <c>Action</c> parameter that saves the secret.</summary>
		[Obsolete("The script no longer accepts secret Save or Delete actions.")]
		public const string SaveAction = "Save";

		/// <summary>Value of the script's <c>Action</c> parameter that deletes the secret.</summary>
		[Obsolete("The script no longer accepts secret Save or Delete actions.")]
		public const string DeleteAction = "Delete";

		private readonly IConnection _connection;
		private readonly NativeCredentialPlatform _platform;
		private readonly Func<string, string> _scriptCredentials;

		internal Secrets(IConnection connection, Func<string, string> scriptCredentials = null)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_platform = new NativeCredentialPlatform(connection);
			_scriptCredentials = scriptCredentials;
		}

		/// <summary>Stores DOM attachment-service credentials, not Automation or connector-owned secrets.</summary>
		/// <param name="sourceIdentifier">Immutable DOM source GUID.</param>
		/// <param name="existingCredentialId">The source's persisted credential GUID, if any.</param>
		/// <param name="username">New username, or empty to preserve an existing value.</param>
		/// <param name="password">New password, or empty to preserve an existing value.</param>
		/// <returns>The native credential GUID for the module's attachment settings.</returns>
		public Guid SaveDomSourceCredential(string sourceIdentifier, string existingCredentialId, string username, string password)
		{
			var sourceId = SharePointCredentialReference.Parse(sourceIdentifier);
			var id = string.IsNullOrEmpty(existingCredentialId)
				? SharePointCredentialReference.CreateId("DomNetworkShare", sourceId)
				: SharePointCredentialReference.Parse(existingCredentialId);
			var name = "DocHub_" + sourceId.ToString("D");
			var credentials = _platform.Read();
			var existing = credentials.SingleOrDefault(c => c.Info.GUID == id);
			if ((existing != null && (!(existing is UserNameAndPasswordCredential) || !string.Equals(existing.Info.Name, name, StringComparison.OrdinalIgnoreCase)))
				|| credentials.Any(c => c.Info.GUID != id && string.Equals(c.Info.Name, name, StringComparison.OrdinalIgnoreCase)))
				throw CredentialFailure.At("The DOM source credential reference is already in use.");
			if (existing == null && (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)))
				throw CredentialFailure.At("A new DOM attachment credential requires a username and password.");

			if (existing != null && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password)
				&& existing.Info.Name == name && existing.Info.Groups != null)
				return id;

			WriteDomCredential(id, name, existing, username, password);
			var saved = _platform.Read().SingleOrDefault(c => c.Info.GUID == id);
			if (!(saved is UserNameAndPasswordCredential) || saved.Info.Name != name)
				throw CredentialFailure.At("The DOM attachment credential could not be verified.");
			return id;
		}

		/// <summary>Creates or updates a source credential and declares it on the DocumentHub scripts that use it.</summary>
		/// <param name="configuration">Source metadata including its immutable identifier.</param>
		/// <param name="clientSecret">New secret, or an empty value to preserve an existing native credential.</param>
		/// <remarks>Uses the caller's SLNet permissions. A new credential gets the caller's DataMiner user groups; an existing one keeps its groups.</remarks>
		public void SaveSharePointClientSecret(SharePointConfiguration configuration, string clientSecret)
		{
			new CredentialProvisioner(_platform).Save(configuration, clientSecret);
		}

		/// <summary>Checks the saved source with its native credential.</summary>
		/// <param name="sharePoint">The saved source.</param>
		/// <returns>Whether authentication and document-library access succeed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sharePoint"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the check could not be completed, for example when no credential is assigned.</exception>
		public bool IsSharePointConnected(SharePointConfiguration sharePoint)
		{
			return ReadSharePointFlag(sharePoint, "Check");
		}

		/// <summary>Checks an in-memory candidate without persisting it.</summary>
		/// <param name="sharePoint">Source metadata.</param>
		/// <param name="clientSecret">Candidate received through a password control or authenticated API request body, never a script parameter.</param>
		/// <returns>Whether authentication and document-library access succeed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sharePoint"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when an identifier is not a valid GUID or <paramref name="clientSecret"/> is null or empty.</exception>
		public bool IsSharePointConnected(SharePointConfiguration sharePoint, string clientSecret)
		{
			if (sharePoint == null)
			{
				throw new ArgumentNullException(nameof(sharePoint));
			}

			try
			{
				var credential = SharePointCredentialProvider.CreateCredential(sharePoint.TenantID, sharePoint.ClientID, clientSecret);
				return SharePointConnectionTester.IsConnected(sharePoint, credential);
			}
			catch (Exception error) when (!(error is ArgumentException))
			{
				throw CredentialFailure.At("The candidate could not authenticate or access the configured document library.", error);
			}
		}

		/// <summary>Checks supported retrieval without exposing the secret.</summary>
		/// <param name="sharePoint">The saved source.</param>
		/// <returns>Whether its declared native credential is non-empty.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="sharePoint"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the check could not be completed.</exception>
		public bool HasSharePointClientSecret(SharePointConfiguration sharePoint)
		{
			return ReadSharePointFlag(sharePoint, "Has");
		}

		/// <summary>Migrates this agent's legacy files explicitly. Must run inside Automation on that agent.</summary>
		/// <param name="includeUnscopedLegacyFile">Explicitly permits migration of graphsecret.dat when exactly one app registration exists.</param>
		/// <returns>The number of legacy files removed after all required native consumers were verified.</returns>
		public int MigrateLocalSharePointSecrets(bool includeUnscopedLegacyFile = false)
		{
			try
			{
				RequireManagementPermission();
				var configurations = new SharePointConfigurationDomRepository(_connection)
					.Read(new TRUEFilterElement<SharePointConfiguration>()).ToList();
				return new CredentialMigration(_platform, new DpapiFileSecretStore(), IsSharePointConnected)
					.Migrate(configurations, includeUnscopedLegacyFile);
			}
			catch (Exception error)
			{
				throw CredentialFailure.At("Legacy migration was not completed. Unverified legacy files were retained.", error);
			}
		}

		/// <summary>The pair-only write API is no longer safe when sources use different client secrets.</summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <param name="clientSecret">The client secret.</param>
		/// <returns>Nothing: this overload always throws <see cref="NotSupportedException"/>.</returns>
		[Obsolete("Use SaveSharePointClientSecret(configuration, clientSecret) with an immutable source identifier.")]
		public ClusterSecretResult SaveSharePointClientSecret(string tenantId, string clientId, string clientSecret)
		{
			throw new NotSupportedException("A source identifier is required. Use the source-scoped native credential API.");
		}

		/// <summary>Automatic deletion is intentionally unsupported because other scripts may reference the credential.</summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <returns>Nothing: this method always throws <see cref="NotSupportedException"/>.</returns>
		[Obsolete("Native credentials are retained on source removal. Review references in the Credentials Library before deleting.")]
		public ClusterSecretResult DeleteSharePointClientSecret(string tenantId, string clientId)
		{
			throw new NotSupportedException("Native credentials must not be deleted by tenant/client pair. Review all source and script references first.");
		}

		/// <summary>The pair-only lookup cannot distinguish isolated source credentials.</summary>
		/// <param name="tenantId">The Azure AD tenant identifier.</param>
		/// <param name="clientId">The client (application) identifier.</param>
		/// <returns>Nothing: this overload always throws <see cref="NotSupportedException"/>.</returns>
		[Obsolete("Use HasSharePointClientSecret(configuration).")]
		public bool HasSharePointClientSecret(string tenantId, string clientId)
		{
			throw new NotSupportedException("A source identifier is required. Use the source-scoped native credential API.");
		}

		/// <summary>Checks administrative access before performing agent-local operations not authorized by SLNet.</summary>
		internal void RequireManagementPermission() => _platform.RequireAdministrator("This agent-local operation requires an unrestricted DataMiner administrator.");

		private void WriteDomCredential(Guid id, string name, ALibraryCredential existing, string username, string password)
		{
			// SLNet replaces the groups on every update: an existing credential keeps its own, a new one gets the saving user's.
			var groups = existing == null ? _platform.CallerGroups() : existing.Info.Groups?.ToList() ?? new List<int>();
			var credential = new UserNameAndPasswordCredential(id, name, groups);
			if (!string.IsNullOrEmpty(username))
				credential.UserName = username;
			if (!string.IsNullOrEmpty(password))
				credential.Password = password;
			_platform.Write(credential);
		}

		private bool ReadSharePointFlag(SharePointConfiguration sharePoint, string operation)
		{
			if (sharePoint == null)
			{
				throw new ArgumentNullException(nameof(sharePoint));
			}

			var id = SharePointCredentialReference.Parse(sharePoint.Identifier);

			// A script that declares the source's credential checks it itself; other callers ask the DocumentHub script that does.
			var secret = _scriptCredentials?.Invoke(new SharePointCredentialReference(sharePoint).Name);
			if (!string.IsNullOrEmpty(secret))
			{
				try
				{
					return operation == "Has"
						|| SharePointConnectionTester.IsConnected(sharePoint, SharePointCredentialProvider.CreateCredential(sharePoint.TenantID, sharePoint.ClientID, secret));
				}
				catch (Exception error)
				{
					throw CredentialFailure.At("The source could not be checked with the credential this script declares.", error);
				}
			}

			var result = SharePointScriptTransport.Execute(_connection, new SharePointScriptRequest { Operation = operation, SourceId = id.ToString("D") });
			if (!bool.TryParse(result.Value, out var value))
				throw CredentialFailure.At("The credential-consuming script returned no valid result.");

			return value;
		}
	}
}
