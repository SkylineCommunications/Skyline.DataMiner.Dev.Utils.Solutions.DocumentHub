namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Security.Cryptography;
	using System.Text;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal sealed class SharePointCredentialReference
	{
		internal SharePointCredentialReference(SharePointConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			SourceId = Parse(configuration.Identifier);
			OrganizationId = Parse(configuration.TenantID);
			ClientId = Parse(configuration.ClientID);
			Name = $"{OrganizationId:D}_{ClientId:D}_{SourceId:D}";

			// A source keeps its library reference when its app registration is replaced.
			Id = CreateId("SharePointClientSecret", SourceId);
		}

		internal Guid SourceId { get; }

		internal Guid OrganizationId { get; }

		internal Guid ClientId { get; }

		internal Guid Id { get; }

		// The library credential's name. Scripts declare it under the same name and select it with engine.GetCredential(name).
		internal string Name { get; }

		internal static Guid CreateId(string purpose, Guid sourceId)
		{
			using (var sha = SHA256.Create())
			{
				var hash = sha.ComputeHash(Encoding.ASCII.GetBytes("DocumentHub/" + purpose + "/v1/" + sourceId.ToString("D")));
				var bytes = new byte[16];
				Array.Copy(hash, bytes, bytes.Length);
				return new Guid(bytes);
			}
		}

		internal static Guid Parse(string value)
		{
			if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
				throw new ArgumentException("Source, organization and client identifiers must be non-empty GUIDs.");

			return id;
		}
	}
}
