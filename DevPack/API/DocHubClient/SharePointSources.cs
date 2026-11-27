namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Repositories.SharePointConfiguration;

	/// <summary>Shared source lifecycle for Automation and authenticated SLNet clients, including gRPC.</summary>
	public sealed class SharePointSources
	{
		private readonly IConnection connection;

		/// <summary>Initializes a new instance of the <see cref="SharePointSources"/> class using the caller's connection and permissions.</summary>
		/// <param name="connection">The caller's connection. Never substitute a local-loop connection.</param>
		public SharePointSources(IConnection connection)
		{
			this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		/// <summary>Stores the credential and declares it on the consuming scripts before persisting non-secret source metadata.</summary>
		/// <param name="configuration">Source metadata. Its non-empty GUID must remain stable across retries.</param>
		/// <param name="clientSecret">A new client secret, or an empty value to preserve an existing native credential.</param>
		/// <remarks>Failure can leave an unused native credential; retry with the same source ID. It never falls back to DPAPI.</remarks>
		public void Save(SharePointConfiguration configuration, string clientSecret)
		{
			var reference = new SharePointCredentialReference(configuration);
			if (string.IsNullOrWhiteSpace(configuration.Name)
				|| string.IsNullOrWhiteSpace(configuration.SiteURL)
				|| string.IsNullOrWhiteSpace(configuration.DocumentLibraryName))
				throw new ArgumentException("Source name, site URL and document library are required.");

			configuration.Identifier = reference.SourceId.ToString("D");
			configuration.TenantID = reference.OrganizationId.ToString("D");
			configuration.ClientID = reference.ClientId.ToString("D");
			new Secrets(connection).SaveSharePointClientSecret(configuration, clientSecret);
			configuration.Status = SharePointConfigurationStatus.Disconnected;
			try
			{
				new SharePointConfigurationDomRepository(connection).CreateOrUpdate(new[] { configuration });
			}
			catch (Exception error)
			{
				throw CredentialFailure.At("The credential was stored and assigned, but source metadata could not be saved. Retry with the same source ID.", error);
			}
		}

		/// <summary>
		/// Declares every saved source's credential again on the installed DocumentHub scripts. A package upgrade
		/// replaces those scripts and with them the declarations.
		/// </summary>
		/// <returns>The number of saved sources.</returns>
		public int RestoreAssignments()
		{
			var platform = new NativeCredentialPlatform(connection);
			var sources = new DocumentHubApiHelper(connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).ToList();
			foreach (var source in sources)
			{
				platform.Assign(new SharePointCredentialReference(source));
			}

			return sources.Count;
		}

		/// <summary>Removes an unreferenced source, retaining credentials which may have other consumers.</summary>
		/// <param name="identifier">Immutable source GUID.</param>
		public void Delete(string identifier)
		{
			var platform = new NativeCredentialPlatform(connection);
			var sourceId = SharePointCredentialReference.Parse(identifier);
			var api = new DocumentHubApiHelper(connection);
			var source = api.SharePointConfigurations.Read(SharePointConfigurationExposers.Identifier.Equal(sourceId.ToString("D"))).SingleOrDefault()
				?? throw CredentialFailure.At("The source does not exist.");
			if (api.DocumentBuckets.Read(new TRUEFilterElement<DocumentBucket>())
				.Any(b => Guid.TryParse(b.SharePointConfiguration.Identifier, out var id) && id == sourceId))
				throw CredentialFailure.At("The source is still linked to a bucket. Unlink it before removal.");

			// There is no transactional, complete reverse-reference query for credentials. Do not delete one here.
			platform.Revoke(new SharePointCredentialReference(source));
			api.SharePointConfigurations.Delete(source);
		}
	}
}
