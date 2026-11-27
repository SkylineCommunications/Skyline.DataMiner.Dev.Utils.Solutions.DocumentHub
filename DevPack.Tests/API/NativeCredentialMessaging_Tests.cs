namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using FluentAssertions;
	using Moq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;
	using DriveItem = Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Models.DriveItem;

	[TestClass]
	public class NativeCredentialMessaging_Tests
	{
		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void EngineAndDirectConnection_UseSameLifecycleAndPersistMetadataOnly(bool engineMediated)
		{
			var native = new NativeCredentialConnectionFake();
			var engine = new Mock<IEngine>();
			engine.Setup(e => e.GetUserConnection()).Returns(native.Connection);
			var connection = engineMediated ? engine.Object.GetUserConnection() : native.Connection;
			var source = CredentialPlatformFake.Source();
			var reference = new SharePointCredentialReference(source);
			var sources = new SharePointSources(connection);
			sources.Save(source, NativeCredentialConnectionFake.UnsafeDetail);
			sources.Save(source, null);

			native.Credentials.Should().ContainSingle();
			native.Scripts.Values.Should().OnlyContain(s => s.Credentials.Length == 1
				&& s.Credentials[0].Name == reference.Name
				&& s.Credentials[0].CredentialId == reference.Id
				&& s.Credentials[0].Type == AutomationCredentialType.Token);
			native.Messages.OfType<UpdateLibraryCredentialMessage>().Should().ContainSingle();
			native.Messages.OfType<GetInfoMessage>().Should().NotContain(m => m.Type == InfoType.ClientSecurity);
			native.Messages.OfType<SaveAutomationScriptMessage>().Should().HaveCount(NativeCredentialPlatform.Consumers.Length);
			native.Messages.OfType<ExecuteScriptMessage>().Should().BeEmpty();
			var saved = new DocumentHubApiHelper(connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Single();
			JsonConvert.SerializeObject(saved).Should().NotContain(NativeCredentialConnectionFake.UnsafeDetail).And.NotContain("ClientSecret");
			saved.Identifier.Should().Be(source.Identifier);
		}

		[TestMethod]
		public void SavedCheck_WithNoBooleanVerdict_DoesNotReportDisconnected()
		{
			var native = new NativeCredentialConnectionFake();
			native.Mock.Setup(c => c.HandleSingleResponseMessage(It.IsAny<ExecuteScriptMessage>()))
				.Returns(new ExecuteScriptResponseMessage { ScriptOutput = new Dictionary<string, string> { ["Result"] = "{}" } });
			var secrets = new DocHubClient(native.Connection).Secrets;
			secrets.Invoking(s => s.IsSharePointConnected(CredentialPlatformFake.Source())).Should().Throw<InvalidOperationException>();
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void TransportErrorOrExceptionResponse_FailsWithoutSecretInException(bool throws)
		{
			var native = new NativeCredentialConnectionFake { RejectUpdate = true, ThrowUpdateError = throws };
			Action action = () => new SharePointSources(native.Connection).Save(CredentialPlatformFake.Source(), NativeCredentialConnectionFake.UnsafeDetail);
			var error = action.Should().Throw<InvalidOperationException>().Which;
			error.Message.Should().Contain("No permission to add library credentials.");
			error.ToString().Should().NotContain(NativeCredentialConnectionFake.UnsafeDetail);
			error.InnerException.Should().BeNull();
			native.Messages.OfType<SaveAutomationScriptMessage>().Should().BeEmpty();
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Should().BeEmpty();
		}

		[TestMethod]
		public void NamedUser_SavesAndRotatesACredentialInOwnGroups()
		{
			var native = new NativeCredentialConnectionFake { Administrator = false };
			var source = CredentialPlatformFake.Source();
			var id = new SharePointCredentialReference(source).Id;
			var sources = new SharePointSources(native.Connection);
			sources.Save(source, "synthetic-secret");
			sources.Save(source, "rotated-secret");

			native.Credentials[id].Info.Groups.Should().Equal(1);
			native.Passwords[id].Should().Be("rotated-secret");
			native.Scripts.Values.Should().OnlyContain(s => s.Credentials.Single().CredentialId == id);
			native.Messages.OfType<GetInfoMessage>().Should().NotContain(m => m.Type == InfoType.ClientSecurity);
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Should().ContainSingle();
		}

		[TestMethod]
		public void CredentialOfAnotherGroup_IsRefusedByTheNativeGroupCheckAndLeftIntact()
		{
			var native = new NativeCredentialConnectionFake { Administrator = false };
			var source = CredentialPlatformFake.Source();
			var reference = new SharePointCredentialReference(source);
			native.Credentials[reference.Id] = new PasswordCredential(reference.Id, reference.Name, new List<int> { 2 });
			native.Passwords[reference.Id] = "original";

			new SharePointSources(native.Connection).Invoking(s => s.Save(source, "synthetic-secret"))
				.Should().Throw<InvalidOperationException>().WithMessage("*is not a member of any group authorized to edit the credential.");
			native.Credentials[reference.Id].Info.Groups.Should().Equal(2);
			native.Passwords[reference.Id].Should().Be("original");
			native.Messages.OfType<SaveAutomationScriptMessage>().Should().BeEmpty();
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Should().BeEmpty();
		}

		[TestMethod]
		public void SeveralSources_AreDeclaredUnderTheirLibraryNamesBesideExistingDeclarations()
		{
			var native = new NativeCredentialConnectionFake();
			var existing = new AutomationCredentialInfo { Id = 1, Name = "dh-udapi-regression", CredentialId = Guid.NewGuid(), Type = AutomationCredentialType.Token };
			native.Credentials[existing.CredentialId] = new PasswordCredential(existing.CredentialId, existing.Name, new List<int>());
			const string script = "SLC-DH-UDAPI-ManageFiles";
			native.Scripts[script].Credentials = new[] { existing };
			var code = native.Scripts[script].Exes[0].Value;
			var first = CredentialPlatformFake.Source();
			var second = CredentialPlatformFake.Source(Guid.NewGuid().ToString());
			var sources = new SharePointSources(native.Connection);
			sources.Save(first, "first-secret");
			sources.Save(second, "second-secret");

			// A new client keeps the library reference and the declaration ID; only the name follows.
			first.ClientID = Guid.NewGuid().ToString();
			sources.Save(first, "replaced-secret");

			var declarations = native.Scripts[script].Credentials.OrderBy(c => c.Id).ToList();
			declarations.Select(c => c.Id).Should().Equal(1, 2, 3);
			declarations.Select(c => c.Name).Should().Equal(existing.Name, new SharePointCredentialReference(first).Name, new SharePointCredentialReference(second).Name);
			declarations.Select(c => c.CredentialId).Should().Equal(existing.CredentialId, new SharePointCredentialReference(first).Id, new SharePointCredentialReference(second).Id);
			native.Scripts[script].Exes[0].Value.Should().Be(code);
		}

		[TestMethod]
		public void DeclarationOfAnotherUsersCredential_IsNotUnlinkedBySaving()
		{
			var native = new NativeCredentialConnectionFake { Administrator = false };
			var hidden = new AutomationCredentialInfo { Id = 1, Name = "another-users-source", CredentialId = Guid.NewGuid(), Type = AutomationCredentialType.Token };
			native.Credentials[hidden.CredentialId] = new PasswordCredential(hidden.CredentialId, hidden.Name, new List<int> { 2 });
			foreach (var script in native.Scripts.Values)
				script.Credentials = new[] { hidden };

			new SharePointSources(native.Connection).Invoking(s => s.Save(CredentialPlatformFake.Source(), "synthetic-secret"))
				.Should().Throw<InvalidOperationException>().WithMessage("*declares credentials that this account cannot access*");
			native.Messages.OfType<SaveAutomationScriptMessage>().Should().BeEmpty();
			native.Scripts.Values.Should().OnlyContain(s => s.Credentials.Single().CredentialId == hidden.CredentialId);
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Should().BeEmpty();
		}

		[TestMethod]
		public void DeclarationsLostInAnUpgrade_AreRestoredForEverySavedSource()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var sources = new SharePointSources(native.Connection);
			sources.Save(source, "synthetic-secret");
			foreach (var script in native.Scripts.Values)
				script.Credentials = Array.Empty<AutomationCredentialInfo>();

			sources.RestoreAssignments().Should().Be(1);
			native.Scripts.Values.Should().OnlyContain(s => s.Credentials.Single().Name == new SharePointCredentialReference(source).Name);
		}

		[TestMethod]
		public void DeclaringScript_UsesItsOwnCredentialAndOtherCallersAskTheDocumentHubScript()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			new SharePointSources(native.Connection).Save(source, "synthetic-secret");
			native.Messages.Clear();
			var requested = new List<string>();

			new Secrets(native.Connection, name =>
			{
				requested.Add(name);
				return "declared-secret";
			}).HasSharePointClientSecret(source).Should().BeTrue();
			native.Messages.OfType<ExecuteScriptMessage>().Should().BeEmpty();
			requested.Should().Equal(new SharePointCredentialReference(source).Name);

			new Secrets(native.Connection, name => null).HasSharePointClientSecret(source).Should().BeTrue();
			new Secrets(native.Connection).HasSharePointClientSecret(source).Should().BeTrue();
			native.Messages.OfType<ExecuteScriptMessage>().Should().HaveCount(2).And.OnlyContain(m => m.ScriptName == Secrets.ScriptName);
		}

		[TestMethod]
		public void ExistingSource_UsesNativePermissionForRevocation()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var sources = new SharePointSources(native.Connection);
			sources.Save(source, "synthetic-secret");
			native.Messages.Clear();
			native.Mock.Setup(c => c.HandleSingleResponseMessage(It.IsAny<SaveAutomationScriptMessage>()))
				.Returns(new ExceptionMessage(new DataMinerSecurityException("Not authorized to add/edit automation scripts")));

			sources.Invoking(s => s.Delete(source.Identifier)).Should().Throw<InvalidOperationException>()
				.WithMessage("*Not authorized to add/edit automation scripts*");
			native.Messages.OfType<GetInfoMessage>().Should().NotContain(m => m.Type == InfoType.ClientSecurity);
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Should().ContainSingle();
		}

		[TestMethod]
		public void PlatformClearedAssignment_DoesNotPersistSourceOrReportSuccess()
		{
			var native = new NativeCredentialConnectionFake { ClearAssignment = true };
			new SharePointSources(native.Connection).Invoking(s => s.Save(CredentialPlatformFake.Source(), "synthetic-secret"))
				.Should().Throw<InvalidOperationException>();
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>()).Should().BeEmpty();
			native.Credentials.Should().ContainSingle();
		}

		[TestMethod]
		public void Removal_RemovesOnlyTheSourcesOwnDeclarationAndRetainsItsCredential()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var other = CredentialPlatformFake.Source(Guid.NewGuid().ToString());
			var sources = new SharePointSources(native.Connection);
			sources.Save(source, "synthetic-secret");
			sources.Save(other, "other-secret");
			sources.Delete(source.Identifier);
			native.Credentials.Should().HaveCount(2);
			native.Scripts.Values.Should().OnlyContain(s => s.Credentials.Single().CredentialId == new SharePointCredentialReference(other).Id);
			new DocumentHubApiHelper(native.Connection).SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>())
				.Should().ContainSingle().Which.Identifier.Should().Be(other.Identifier);
		}

		[TestMethod]
		public void Removal_WithLinkedBucketDoesNotRevokeOrDeleteAnything()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var sources = new SharePointSources(native.Connection);
			sources.Save(source, "synthetic-secret");
			new DocumentHubApiHelper(native.Connection).DocumentBuckets.Create(Bucket(source.Identifier));
			sources.Invoking(s => s.Delete(source.Identifier)).Should().Throw<InvalidOperationException>();
			native.Credentials.Should().ContainSingle();
			native.Scripts.Values.Should().OnlyContain(s => s.Credentials.Length == 1);
		}

		[TestMethod]
		public void Worker_WrongSourceBucketIsDeniedBeforeCredentialRetrieval()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var api = new DocumentHubApiHelper(native.Connection);
			api.SharePointConfigurations.Create(source);
			var foreign = CredentialPlatformFake.Source(Guid.NewGuid().ToString());
			foreign.TenantID = Guid.NewGuid().ToString();
			api.SharePointConfigurations.Create(foreign);
			var bucket = Bucket(foreign.Identifier);
			api.DocumentBuckets.Create(bucket);
			var probe = new SecretProbe();
			var request = Encode(new SharePointScriptRequest { Operation = "Read", SourceId = source.Identifier, BucketId = bucket.Identifier });
			Action action = () => SharePointScriptHost.Execute(native.Connection, probe.Get, request);
			action.Should().Throw<UnauthorizedAccessException>();
			probe.Retrieved.Should().BeFalse();
		}

		[TestMethod]
		public void Worker_RemovedSourceCannotRetrieveCredential()
		{
			var native = new NativeCredentialConnectionFake();
			var probe = new SecretProbe();
			var request = Encode(new SharePointScriptRequest { Operation = "Has", SourceId = CredentialPlatformFake.Source().Identifier });
			Action action = () => SharePointScriptHost.Execute(native.Connection, probe.Get, request);
			action.Should().Throw<UnauthorizedAccessException>();
			probe.Retrieved.Should().BeFalse();
		}

		[TestMethod]
		public void SeveralSources_TheScriptSelectsTheRequestedSourcesCredentialByLibraryName()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var other = CredentialPlatformFake.Source(Guid.NewGuid().ToString());
			var api = new DocumentHubApiHelper(native.Connection);
			api.SharePointConfigurations.Create(source);
			api.SharePointConfigurations.Create(other);
			var probe = new SecretProbe();
			var request = Encode(new SharePointScriptRequest { Operation = "Has", SourceId = other.Identifier });
			SharePointScriptHost.Execute(native.Connection, probe.Get, request).Should().Contain("True");
			probe.Requested.Should().Be(new SharePointCredentialReference(other).Name);
		}

		[TestMethod]
		public void DialogFailure_ReportsNativeErrorCodeWithoutEchoingTheMessage()
		{
			var cause = new DataMinerException($"Show UI Failed: 0x80004005 ({NativeCredentialConnectionFake.UnsafeDetail})");
			var error = new InvalidOperationException("Synthetic wrapper", cause);
			CredentialFailure.Describe(error).Should().Contain("Show UI failed (0x80004005)")
				.And.NotContain(NativeCredentialConnectionFake.UnsafeDetail);
		}

		[TestMethod]
		[DataRow("Upload")]
		[DataRow("Download")]
		public void Worker_LocalFilesOutsideStagingRootsRequireAdministratorBeforeRetrieval(string operation)
		{
			var native = new NativeCredentialConnectionFake { Administrator = false };
			var source = CredentialPlatformFake.Source();
			var bucket = Bucket(source.Identifier);
			var api = new DocumentHubApiHelper(native.Connection);
			api.SharePointConfigurations.Create(source);
			api.DocumentBuckets.Create(bucket);
			var probe = new SecretProbe();
			var request = Encode(new SharePointScriptRequest { Operation = operation, SourceId = source.Identifier, BucketId = bucket.Identifier, LocalPath = @"C:\outside-documenthub\file.dat" });
			Action action = () => SharePointScriptHost.Execute(native.Connection, probe.Get, request);
			action.Should().Throw<UnauthorizedAccessException>();
			probe.Retrieved.Should().BeFalse();
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(201)]
		public void Worker_InvalidPageSizeIsRejectedBeforeCredentialRetrieval(int pageSize)
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var bucket = Bucket(source.Identifier);
			var api = new DocumentHubApiHelper(native.Connection);
			api.SharePointConfigurations.Create(source);
			api.DocumentBuckets.Create(bucket);
			var probe = new SecretProbe();
			var request = Encode(new SharePointScriptRequest { Operation = "Read", SourceId = source.Identifier, BucketId = bucket.Identifier, PageSize = pageSize });
			Action action = () => SharePointScriptHost.Execute(native.Connection, probe.Get, request);
			action.Should().Throw<ArgumentException>();
			probe.Retrieved.Should().BeFalse();
		}

		[TestMethod]
		[DataRow("root2/file")]
		[DataRow("root/../other")]
		[DataRow("root/%2e%2e/other")]
		public void Worker_UploadQualifierCannotEscapeBucket(string path)
		{
			Action action = () => SharePointScriptHost.ValidateUploadPath("root", path);
			action.Should().Throw<UnauthorizedAccessException>();
		}

		[TestMethod]
		[DataRow("../other/file.pdf")]
		[DataRow("%2e%2e/other/file.pdf")]
		public void Worker_ExistenceProbeCannotEscapeBucketThroughFileName(string name)
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var bucket = Bucket(source.Identifier);
			var api = new DocumentHubApiHelper(native.Connection);
			api.SharePointConfigurations.Create(source);
			api.DocumentBuckets.Create(bucket);
			var probe = new SecretProbe();
			var request = Encode(new SharePointScriptRequest { Operation = "Exists", SourceId = source.Identifier, BucketId = bucket.Identifier, UploadPath = bucket.UploadPath, Name = name });
			Action action = () => SharePointScriptHost.Execute(native.Connection, probe.Get, request);
			action.Should().Throw<UnauthorizedAccessException>();
			probe.Retrieved.Should().BeFalse();
		}

		[TestMethod]
		[DataRow("Read", null, false)]
		[DataRow("Read", "filetype:pdf", true)]
		[DataRow("Search", "filetype:pdf", true)]
		public void Worker_FilteredReadsAndSearchUseSearchPaging(string operation, string filter, bool search)
		{
			var handler = new Mock<IStorageHandler>(MockBehavior.Strict);
			var searchable = handler.As<ISearchableStorageHandler>();
			var files = new List<IDocHubFile>
			{
				new DriveItemAdapter { DriveItem = new DriveItem { Id = "item", Name = "file.pdf", WebUrl = "https://contoso.sharepoint.com/sites/test/file.pdf" } },
			};
			if (search)
			{
				searchable.Setup(h => h.SearchFiles(It.Is<SearchData>(d => d.Context is SharePointSearchPageData && d.Query == filter)))
					.Returns((SearchData d) =>
					{
						d.Context.NativeHasMore = false;
						return files;
					});
			}
			else
			{
				handler.Setup(h => h.ReadFiles(It.Is<ReadData>(d => d.Context is SharePointPageData)))
					.Returns((ReadData d) =>
					{
						d.Context.NativeHasMore = false;
						return files;
					});
			}

			var request = new SharePointScriptRequest { Operation = operation, Filter = filter, Region = "EMEA", Offset = 0, PageSize = 2 };
			var result = SharePointScriptHost.ReadPage(handler.Object, Bucket(CredentialPlatformFake.Source().Identifier), request);
			result.Files.Should().ContainSingle().Which.Id.Should().Be("item");
			result.NextOffset.Should().Be(1);
			result.HasMore.Should().BeFalse();
			handler.VerifyAll();
		}

		[TestMethod]
		public void DomCredentials_UseNativeUsernamePasswordWithoutAutomationAssignments()
		{
			var native = new NativeCredentialConnectionFake();
			var secrets = new DocHubClient(native.Connection).Secrets;
			var sourceId = Guid.NewGuid().ToString();
			var id = secrets.SaveDomSourceCredential(sourceId, null, "synthetic-user", "synthetic-password");
			secrets.SaveDomSourceCredential(sourceId, id.ToString(), "updated-user", null).Should().Be(id);
			native.Credentials[id].Should().BeOfType<UserNameAndPasswordCredential>();
			native.Credentials[id].Info.Groups.Should().BeEmpty();
			native.Passwords[id].Should().Be("synthetic-password");
			native.Credentials[id].dirtyProperties.Should().NotContain(nameof(PasswordCredential.Password));
			native.RejectUpdate = true;
			secrets.SaveDomSourceCredential(sourceId, id.ToString(), null, null).Should().Be(id);
			native.Messages.OfType<UpdateLibraryCredentialMessage>().Should().HaveCount(2);
			native.RejectUpdate = false;
			native.Messages.OfType<SaveAutomationScriptMessage>().Should().BeEmpty();
			Action foreign = () => secrets.SaveDomSourceCredential(Guid.NewGuid().ToString(), id.ToString(), "foreign", "foreign");
			foreign.Should().Throw<InvalidOperationException>();
		}

		[TestMethod]
		public void DomCredentials_OfANamedUserGetOwnGroupsAndKeepThemOnUpdate()
		{
			var native = new NativeCredentialConnectionFake { Administrator = false };
			var secrets = new DocHubClient(native.Connection).Secrets;
			var sourceId = Guid.NewGuid().ToString();
			var id = secrets.SaveDomSourceCredential(sourceId, null, "synthetic-user", "synthetic-password");
			native.CallerGroups = new[] { 1, 3 };
			secrets.SaveDomSourceCredential(sourceId, id.ToString(), "updated-user", null).Should().Be(id);
			native.Credentials[id].Info.Groups.Should().Equal(1);
		}

		[TestMethod]
		public void NativeNullGroups_AreNoAccessLikeEmptyGroupsAndAreNormalizedOnSave()
		{
			var native = new NativeCredentialConnectionFake();
			var source = CredentialPlatformFake.Source();
			var reference = new SharePointCredentialReference(source);
			native.Credentials[reference.Id] = new PasswordCredential(reference.Id, reference.Name, null);
			native.Passwords[reference.Id] = "synthetic-password";
			new SharePointSources(native.Connection).Save(source, null);
			native.Credentials[reference.Id].Info.Groups.Should().NotBeNull().And.BeEmpty();
			native.Passwords[reference.Id].Should().Be("synthetic-password");
		}

		private static DocumentBucket Bucket(string source) => new DocumentBucket
		{
			Identifier = Guid.NewGuid().ToString(),
			Name = "Synthetic bucket",
			UploadPath = "root",
			StorageType = StorageType.SharePoint,
			SharePointConfiguration = new SdmObjectReference<SharePointConfiguration>(source),
		};

		private static string Encode(SharePointScriptRequest request) =>
			Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)));

		private sealed class SecretProbe
		{
			public bool Retrieved { get; private set; }

			public string? Requested { get; private set; }

			public string Get(string name)
			{
				Retrieved = true;
				Requested = name;
				return "synthetic-secret";
			}
		}
	}
}
