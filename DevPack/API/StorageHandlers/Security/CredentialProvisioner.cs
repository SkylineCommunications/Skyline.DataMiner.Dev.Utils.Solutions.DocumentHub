namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal sealed class CredentialProvisioner
	{
		private readonly ICredentialPlatform platform;

		internal CredentialProvisioner(ICredentialPlatform platform)
		{
			this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
		}

		internal static bool BelongsToSource(string name, Guid sourceId)
		{
			if (name == null || name.Length != 110)
				return false;

			var parts = name.Split('_');
			return parts.Length == 3
				&& Guid.TryParseExact(parts[0], "D", out var organization) && organization != Guid.Empty
				&& Guid.TryParseExact(parts[1], "D", out var client) && client != Guid.Empty
				&& Guid.TryParseExact(parts[2], "D", out var source) && source == sourceId;
		}

		internal void Save(SharePointConfiguration configuration, string secret, bool keepExistingSecret = false)
		{
			var reference = new SharePointCredentialReference(configuration);
			var credentials = platform.Read();
			var existing = credentials.SingleOrDefault(c => c.Info.GUID == reference.Id);
			if (credentials.Any(c => c.Info.GUID != reference.Id && string.Equals(c.Info.Name, reference.Name, StringComparison.OrdinalIgnoreCase)))
				throw CredentialFailure.At("The deterministic credential name is already in use.");

			if (existing != null && (existing.GetType() != typeof(PasswordCredential)
				|| existing.Info.Type != nameof(PasswordCredential)
				|| !BelongsToSource(existing.Info.Name, reference.SourceId)))
			{
				throw CredentialFailure.At("The deterministic credential reference is already in use.");
			}

			if (existing == null && string.IsNullOrEmpty(secret))
				throw CredentialFailure.At("This source needs a client secret or an explicit legacy migration.");

			if (keepExistingSecret && existing != null)
				secret = null;

			if (existing != null && !string.Equals(existing.Info.Name, reference.Name, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(secret))
				throw CredentialFailure.At("Changing the organization or client requires a new client secret.");

			// A metadata-only edit must not rewrite a canonical credential; it still repairs the declarations below.
			if (existing == null || !string.IsNullOrEmpty(secret) || existing.Info.Name != reference.Name || existing.Info.Groups == null)
			{
				WriteCredential(reference, existing, secret);
			}

			platform.Assign(reference);
		}

		private void WriteCredential(SharePointCredentialReference reference, ALibraryCredential existing, string secret)
		{
			// SLNet replaces the groups on every update: an existing credential keeps its own, a new one gets the saving user's.
			var groups = existing == null ? platform.CallerGroups() : existing.Info.Groups?.ToList() ?? new List<int>();
			var credential = new PasswordCredential(reference.Id, reference.Name, groups);
			if (!string.IsNullOrEmpty(secret))
				credential.Password = secret;
			platform.Write(credential);
		}
	}
}
