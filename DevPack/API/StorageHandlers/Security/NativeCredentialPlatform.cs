namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

	internal interface ICredentialPlatform
	{
		IReadOnlyList<ALibraryCredential> Read();

		List<int> CallerGroups();

		void Write(ALibraryCredential credential);

		void Assign(SharePointCredentialReference reference);
	}

	internal sealed class NativeCredentialPlatform : ICredentialPlatform
	{
		// The installed DocumentHub scripts that reach SharePoint. Each declares every source's credential under the
		// library credential's name, so engine.GetCredential(name) selects the right one when several are declared.
		internal static readonly string[] Consumers =
		{
			Secrets.ScriptName,
			"SLC-DH-AS-CheckConnection",
			"SLC-DH-AS-SaveConfiguration",
			"SLC-DH-IAS-UploadFile",
			"SLC-DH-UDAPI-DownloadFile",
			"SLC-DH-UDAPI-ManageFiles",
			"SLC-DH-UDAPI-ManageSharepointSources",
		};

		// Saving a script replaces all of its declarations, so saves in this process must not interleave.
		private static readonly object DeclarationGate = new object();

		private readonly IConnection connection;

		internal NativeCredentialPlatform(IConnection connection)
		{
			this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public IReadOnlyList<ALibraryCredential> Read()
		{
			const string stage = "Credential metadata could not be read.";
			var response = Send(new GetLibraryCredentialsRequestMessage(), stage) as GetLibraryCredentialsResponseMessage;
			if (response?.Credentials == null)
				throw CredentialFailure.At(stage);

			return response.Credentials;
		}

		public List<int> CallerGroups()
		{
			const string stage = "The caller's DataMiner user groups could not be read.";
			var security = Send(new GetInfoMessage(InfoType.SecurityInfo), stage) as GetUserInfoResponseMessage;
			if (security == null)
				throw CredentialFailure.At(stage);

			// No user entry or no groups, typically the built-in Administrator: the credential stays ungrouped.
			return security.FindUserByLoginName(connection.UserDomainName)?.Groups.ToList() ?? new List<int>();
		}

		public void Write(ALibraryCredential credential)
		{
			const string stage = "Credential creation or update failed.";
			DMSMessage[] responses;
			try
			{
				responses = connection.HandleMessages(new DMSMessage[] { new UpdateLibraryCredentialMessage { Credential = credential } });
			}
			catch (Exception error)
			{
				throw CredentialFailure.At(stage, error);
			}

			// The platform returns no messages on success.
			if (responses == null || responses.Length != 0)
				throw CredentialFailure.At(stage, responses?.OfType<ExceptionMessage>().FirstOrDefault()?.Exception);
		}

		public void Assign(SharePointCredentialReference reference)
		{
			lock (DeclarationGate)
			{
				var consumers = InstalledConsumers();

				// Callers that cannot declare a credential themselves, such as GQI data sources, rely on this script.
				if (!consumers.Contains(Secrets.ScriptName, StringComparer.OrdinalIgnoreCase))
					throw CredentialFailure.At($"The DocumentHub script {Secrets.ScriptName} is not installed.");

				var accessible = Read().Select(c => c.Info.GUID).ToList();
				foreach (var script in consumers)
				{
					var definition = ReadScript(script);
					var declarations = definition.Credentials ?? Array.Empty<AutomationCredentialInfo>();
					var own = declarations.FirstOrDefault(c => c.CredentialId == reference.Id)
						?? declarations.FirstOrDefault(c => c.CredentialId == Guid.Empty && IsNamed(c, reference));

					// engine.GetCredential(name) returns the first match, so another entry under this name would shadow the source's own.
					if (declarations.Any(c => c != own && IsNamed(c, reference)))
						throw CredentialFailure.At($"Script {script} already declares this credential name for another credential.");

					if (own != null && own.CredentialId == reference.Id && own.Name == reference.Name && own.Type == AutomationCredentialType.Token)
						continue;

					var declaration = new AutomationCredentialInfo
					{
						Id = own?.Id ?? Enumerable.Range(1, declarations.Length + 1).First(id => declarations.All(c => c.Id != id)),
						Name = reference.Name,
						CredentialId = reference.Id,
						Type = AutomationCredentialType.Token,
					};
					definition.Credentials = declarations.Where(c => c != own).Concat(new[] { declaration }).ToArray();
					SaveDeclarations(definition, accessible, "Credential script assignment failed.");
				}
			}
		}

		internal void RequireAdministrator(string reason)
		{
			var response = Send(new GetInfoMessage(InfoType.ClientSecurity), "The caller's DataMiner security could not be read.") as ClientSecurityChangeEventMessage;
			if (response?.uia == null || !new ClientSecurity(response.uia).Unrestricted)
				throw new UnauthorizedAccessException(reason);
		}

		internal void Revoke(SharePointCredentialReference reference)
		{
			lock (DeclarationGate)
			{
				var accessible = Read().Select(c => c.Info.GUID).ToList();
				foreach (var script in InstalledConsumers())
				{
					var definition = ReadScript(script);
					if (definition.Credentials?.Any(c => c.CredentialId == reference.Id) != true)
						continue;

					definition.Credentials = definition.Credentials.Where(c => c.CredentialId != reference.Id).ToArray();
					SaveDeclarations(definition, accessible, "The source's credential access could not be revoked.");
				}
			}
		}

		private static bool IsNamed(AutomationCredentialInfo declaration, SharePointCredentialReference reference)
		{
			return string.Equals(declaration.Name, reference.Name, StringComparison.OrdinalIgnoreCase);
		}

		private List<string> InstalledConsumers()
		{
			const string stage = "The installed DocumentHub scripts could not be listed.";
			var scripts = Send(new GetInfoMessage(InfoType.Scripts), stage) as GetScriptsResponseMessage;
			if (scripts?.Scripts == null)
				throw CredentialFailure.At(stage);

			return Consumers.Where(c => scripts.Scripts.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
		}

		private GetScriptInfoResponseMessage ReadScript(string name)
		{
			var stage = $"Script {name} could not be read.";
			return Send(new GetScriptInfoMessage(name), stage) as GetScriptInfoResponseMessage ?? throw CredentialFailure.At(stage);
		}

		private void SaveDeclarations(GetScriptInfoResponseMessage definition, List<Guid> accessible, string stage)
		{
			// SLNet unlinks, without failing, every declared credential that the saving user cannot access.
			if (definition.Credentials.Any(c => c.CredentialId != Guid.Empty && !accessible.Contains(c.CredentialId)))
			{
				throw CredentialFailure.At($"Script {definition.Name} declares credentials that this account cannot access. "
					+ "Saving it would unlink them, so nothing was changed.");
			}

			var response = Send(new SaveAutomationScriptMessage { Definition = definition, IsUpdate = true }, stage) as SaveAutomationScriptResponse;
			if (response?.HasSucceeded != true)
			{
				var detail = response?.FailedMessages.FirstOrDefault()?.Message;
				throw CredentialFailure.At(stage, detail == null ? null : new InvalidOperationException(detail));
			}

			var saved = ReadScript(definition.Name).Credentials ?? Array.Empty<AutomationCredentialInfo>();
			if (saved.Length != definition.Credentials.Length
				|| definition.Credentials.Any(sent => !saved.Any(c => c.Id == sent.Id && c.CredentialId == sent.CredentialId && c.Name == sent.Name)))
			{
				throw CredentialFailure.At($"The credential assignments of script {definition.Name} could not be verified.");
			}
		}

		private DMSMessage Send(DMSMessage request, string stage)
		{
			DMSMessage response;
			try
			{
				response = connection.HandleSingleResponseMessage(request);
			}
			catch (Exception error)
			{
				throw CredentialFailure.At(stage, error);
			}

			if (response is ExceptionMessage failure)
				throw CredentialFailure.At(stage, failure.Exception);
			return response;
		}
	}
}
