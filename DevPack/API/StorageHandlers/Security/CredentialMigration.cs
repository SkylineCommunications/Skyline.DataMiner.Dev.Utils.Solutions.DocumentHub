namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal sealed class CredentialMigration
	{
		private readonly ICredentialPlatform platform;
		private readonly ISecretStore legacy;
		private readonly Func<SharePointConfiguration, bool> connected;

		internal CredentialMigration(ICredentialPlatform platform, ISecretStore legacy, Func<SharePointConfiguration, bool> connected)
		{
			this.platform = platform;
			this.legacy = legacy;
			this.connected = connected;
		}

		internal int Migrate(IReadOnlyCollection<SharePointConfiguration> configurations, bool includeGlobal)
		{
			var groups = configurations.GroupBy(c => SharePointSecrets.ClientSecret(c.TenantID, c.ClientID).RelativePath).ToList();
			var files = 0;
			foreach (var group in groups)
			{
				var first = group.First();
				var key = SharePointSecrets.ClientSecret(first.TenantID, first.ClientID);
				if (!legacy.Exists(key))
				{
					if (legacy is DpapiFileSecretStore global && global.GlobalExists && includeGlobal && groups.Count == 1)
						continue;
					MigrateSources(group, null);
					continue;
				}

				var secret = legacy.Read(key);
				MigrateSources(group, secret);
				legacy.Delete(key);
				files++;
			}

			var fileStore = legacy as DpapiFileSecretStore;
			if (fileStore?.GlobalExists == true)
			{
				if (!includeGlobal || groups.Count != 1)
					throw CredentialFailure.At("The unscoped graphsecret.dat file was retained. Explicit global migration requires exactly one app registration.");

				var secret = fileStore.ReadGlobal();
				MigrateSources(groups.Single(), secret);
				fileStore.DeleteGlobal();
				files++;
			}

			return files;
		}

		private void MigrateSources(IEnumerable<SharePointConfiguration> sources, string secret)
		{
			var provisioner = new CredentialProvisioner(platform);
			foreach (var source in sources)
			{
				provisioner.Save(source, secret, keepExistingSecret: true);
				if (!connected(source))
					throw CredentialFailure.At("Native authentication could not be verified. Legacy secret files were retained; retry migration after resolving access or connectivity.");
			}
		}
	}
}
