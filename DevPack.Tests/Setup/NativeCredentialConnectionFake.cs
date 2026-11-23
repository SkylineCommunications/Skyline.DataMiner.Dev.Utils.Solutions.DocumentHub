namespace Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Moq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	// Exercises real DocumentHub message construction, not a live SLNet or Automation process.
	internal sealed class NativeCredentialConnectionFake
	{
		internal const string UnsafeDetail = "synthetic-secret-must-not-escape";

		private readonly DomSLNetMessageHandler dom = new DomSLNetMessageHandler();

		internal NativeCredentialConnectionFake()
		{
			Mock.Setup(c => c.HandleSingleResponseMessage(It.IsAny<DMSMessage>())).Returns((DMSMessage message) => Handle(message).Single());
			Mock.Setup(c => c.HandleMessage(It.IsAny<DMSMessage>())).Returns((DMSMessage message) => Handle(message));
			Mock.Setup(c => c.HandleMessages(It.IsAny<DMSMessage[]>())).Returns((DMSMessage[] messages) => messages.SelectMany(Handle).ToArray());
			Mock.SetupGet(c => c.UserDomainName).Returns("Synthetic caller");
			foreach (var name in NativeCredentialPlatform.Consumers)
			{
				Scripts[name] = new GetScriptInfoResponseMessage
				{
					Name = name,
					Description = "Installed synthetic script",
					Exes = new[] { new AutomationExeInfo { Id = 1, Type = AutomationExeType.CSharpCode, Value = "installed code of " + name } },
					Credentials = Array.Empty<AutomationCredentialInfo>(),
				};
			}
		}

		internal Dictionary<Guid, ALibraryCredential> Credentials { get; } = new Dictionary<Guid, ALibraryCredential>();

		internal Dictionary<Guid, string> Passwords { get; } = new Dictionary<Guid, string>();

		// The installed DocumentHub scripts, as SLNet would return them.
		internal Dictionary<string, GetScriptInfoResponseMessage> Scripts { get; } = new Dictionary<string, GetScriptInfoResponseMessage>(StringComparer.OrdinalIgnoreCase);

		internal List<DMSMessage> Messages { get; } = new List<DMSMessage>();

		internal Mock<IConnection> Mock { get; } = new Mock<IConnection>();

		internal bool Administrator { get; set; } = true;

		internal int[] CallerGroups { get; set; } = { 1 };

		internal bool ClearAssignment { get; set; }

		internal bool RejectUpdate { get; set; }

		internal bool ThrowUpdateError { get; set; }

		internal IConnection Connection => Mock.Object;

		private static ALibraryCredential Metadata(ALibraryCredential credential)
		{
			ALibraryCredential result = credential is UserNameAndPasswordCredential
				? (ALibraryCredential)new UserNameAndPasswordCredential(credential.Info.GUID, credential.Info.Name, credential.Info.Groups?.ToList())
				: new PasswordCredential(credential.Info.GUID, credential.Info.Name, credential.Info.Groups?.ToList());
			result.Info.Type = credential.Info.Type;
			return result;
		}

		private static GetScriptInfoResponseMessage Copy(GetScriptInfoResponseMessage script, IEnumerable<AutomationCredentialInfo> credentials)
		{
			return new GetScriptInfoResponseMessage
			{
				Name = script.Name,
				Description = script.Description,
				Parameters = script.Parameters,
				Exes = script.Exes,
				Credentials = credentials.Select(c => (AutomationCredentialInfo)c.Clone()).ToArray(),
			};
		}

		private bool IsAccessible(ALibraryCredential credential) => Administrator || credential.Info.Groups?.Intersect(CallerGroups).Any() == true;

		private DMSMessage[] Handle(DMSMessage message)
		{
			Messages.Add(message);
			switch (message)
			{
				case GetInfoMessage info when info.Type == InfoType.SecurityInfo:
					// Like the built-in Administrator, the synthetic administrator has no user groups.
					var memberships = CallerGroups.Select(g => new GroupMembershipInfo(g)).ToArray();
					return new DMSMessage[]
					{
						new GetUserInfoResponseMessage
						{
							Users = Administrator
								? Array.Empty<DataMinerUser>()
								: new[] { new DataMinerUser("Synthetic caller", "Synthetic caller", string.Empty) { Memberships = memberships } },
						},
					};
				case GetInfoMessage info when info.Type == InfoType.ClientSecurity:
					return new DMSMessage[]
					{
						new ClientSecurityChangeEventMessage
						{
							uia = Administrator ? Array.Empty<uint>()
								: new ClientSecurity(Array.Empty<ClientSecurityElement>(), Array.Empty<PermissionFlags>(), Array.Empty<int>()).SecurityKey,
						},
					};
				case GetLibraryCredentialsRequestMessage _:
					return new DMSMessage[]
					{
						new GetLibraryCredentialsResponseMessage
						{
							Credentials = Credentials.Values.Where(IsAccessible).Select(Metadata).ToList(),
						},
					};
				case UpdateLibraryCredentialMessage update:
					if (RejectUpdate)
					{
						var error = new InvalidOperationException(UnsafeDetail, new DataMinerSecurityException("No permission to add library credentials."));
						if (ThrowUpdateError)
							throw error;
						return new DMSMessage[] { new ExceptionMessage(error) };
					}

					// SLNet checks group membership when editing, not when adding.
					if (Credentials.TryGetValue(update.Credential.Info.GUID, out var stored)
						&& stored.GetType() == update.Credential.GetType() && !IsAccessible(stored))
					{
						return new DMSMessage[]
						{
							new ExceptionMessage(new DataMinerSecurityException("User 'Synthetic caller' is not a member of any group authorized to edit the credential.")),
						};
					}

					Credentials[update.Credential.Info.GUID] = update.Credential;
					if (update.Credential.dirtyProperties.Contains(nameof(PasswordCredential.Password)))
						Passwords[update.Credential.Info.GUID] = ((PasswordCredential)update.Credential).Password;
					return Array.Empty<DMSMessage>();
				case GetInfoMessage info when info.Type == InfoType.Scripts:
					return new DMSMessage[] { new GetScriptsResponseMessage { Scripts = Scripts.Keys.ToArray() } };
				case SaveAutomationScriptMessage save:
					// SLNet clears a declared credential that the saving user cannot access.
					var links = save.Definition.Credentials.Select(c => (AutomationCredentialInfo)c.Clone()).ToList();
					foreach (var link in links)
					{
						if (ClearAssignment || !Credentials.TryGetValue(link.CredentialId, out var declared) || !IsAccessible(declared))
							link.CredentialId = Guid.Empty;
					}

					Scripts[save.Definition.Name] = Copy(save.Definition, links);
					return new DMSMessage[] { new SaveAutomationScriptResponse() };
				case GetScriptInfoMessage script:
					return new DMSMessage[] { Copy(Scripts[script.Name], Scripts[script.Name].Credentials) };
				case ExecuteScriptMessage execute:
					// The DocumentHub script selects the credential it declares under the source's library name.
					var encoded = execute.Options.Sa.Single(o => o.StartsWith("PARAMETER:3:", StringComparison.Ordinal)).Substring("PARAMETER:3:".Length);
					var request = JsonConvert.DeserializeObject<SharePointScriptRequest>(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)))!;
					var name = Credentials.Values.Select(c => c.Info.Name).FirstOrDefault(n => n.EndsWith(request.SourceId, StringComparison.OrdinalIgnoreCase));
					var declaration = Scripts[execute.ScriptName].Credentials.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
					if (declaration == null || !Passwords.ContainsKey(declaration.CredentialId))
						return new DMSMessage[] { new ExecuteScriptResponseMessage { saRet = new SA { Sa = new[] { "-1", UnsafeDetail } } } };
					return new DMSMessage[]
					{
						new ExecuteScriptResponseMessage { ScriptOutput = new Dictionary<string, string> { { "Result", "{\"Value\":\"True\"}" } } },
					};
				default:
					if (dom.TryHandleMessage(message, out var response))
						return new[] { response };
					throw new NotSupportedException(message.GetType().Name);
			}
		}
	}
}
