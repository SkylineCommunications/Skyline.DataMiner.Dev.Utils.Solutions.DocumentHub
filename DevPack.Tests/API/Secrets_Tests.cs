namespace DevPack.Tests.API
{
	using System;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class Secrets_Tests
	{
		[TestMethod]
		public void Save_CreatesOnlySourceScopedTokenAndConsumer()
		{
			var platform = new CredentialPlatformFake();
			var configuration = CredentialPlatformFake.Source();
			var reference = new SharePointCredentialReference(configuration);
			new CredentialProvisioner(platform).Save(configuration, "synthetic-secret");
			platform.Credentials.Should().ContainSingle();
			platform.Credentials[reference.Id].Should().BeOfType<PasswordCredential>();
			platform.Credentials[reference.Id].Info.Name.Should().Be(reference.Name);
			platform.Credentials[reference.Id].Info.Groups.Should().Equal(1);
			platform.Assignments.Should().ContainSingle().Which.Key.Should().Be(reference.Name);
		}

		[TestMethod]
		public void Rotation_KeepsTheGroupsOfAnExistingCredential()
		{
			var platform = new CredentialPlatformFake();
			var source = CredentialPlatformFake.Source();
			var reference = new SharePointCredentialReference(source);
			platform.Credentials[reference.Id] = new PasswordCredential(reference.Id, reference.Name, new List<int> { 2 });
			new CredentialProvisioner(platform).Save(source, "replacement");
			platform.Credentials[reference.Id].Info.Groups.Should().Equal(2);
			platform.Passwords[reference.Id].Should().Be("replacement");
		}

		[TestMethod]
		public void RepeatSave_PreservesSecretWithoutCopyingRedactedValue()
		{
			var platform = new CredentialPlatformFake();
			var configuration = CredentialPlatformFake.Source();
			var provisioner = new CredentialProvisioner(platform);
			provisioner.Save(configuration, "original");
			platform.Assignments.Clear(); // A metadata-only save must still repair the script declarations.
			provisioner.Save(configuration, null);
			platform.Credentials.Should().ContainSingle();
			platform.Passwords[new SharePointCredentialReference(configuration).Id].Should().Be("original");
			platform.Writes.Should().ContainSingle();
			platform.Assignments.Should().ContainSingle();
		}

		[TestMethod]
		public void Rotation_KeepsImmutableReferenceAndUpdatesSecret()
		{
			var platform = new CredentialPlatformFake();
			var source = CredentialPlatformFake.Source();
			var provisioner = new CredentialProvisioner(platform);
			provisioner.Save(source, "original");
			var id = new SharePointCredentialReference(source).Id;
			source.ClientID = Guid.NewGuid().ToString();
			provisioner.Invoking(p => p.Save(source, null)).Should().Throw<InvalidOperationException>();
			provisioner.Save(source, "replacement");
			platform.Credentials.Should().ContainSingle();
			new SharePointCredentialReference(source).Id.Should().Be(id);
			platform.Passwords[id].Should().Be("replacement");
		}

		[TestMethod]
		public void ConcurrentCreation_UsesOneGuidAndOneScript()
		{
			var platform = new CredentialPlatformFake();
			using (var barrier = new Barrier(2))
			{
				platform.ReadBarrier = barrier;
				Parallel.Invoke(
					() => new CredentialProvisioner(platform).Save(CredentialPlatformFake.Source(), "same-secret"),
					() => new CredentialProvisioner(platform).Save(CredentialPlatformFake.Source(), "same-secret"));
			}

			platform.Credentials.Should().ContainSingle();
			platform.Assignments.Should().ContainSingle();
		}

		[TestMethod]
		public void Naming_NormalizesGuidSyntaxAndSeparatesSourcesAndOrganizations()
		{
			var source = CredentialPlatformFake.Source();
			var expected = new SharePointCredentialReference(source);
			source.TenantID = Guid.Parse(source.TenantID).ToString("B").ToUpperInvariant();
			source.ClientID = Guid.Parse(source.ClientID).ToString("N").ToUpperInvariant();
			var normalized = new SharePointCredentialReference(source);
			normalized.Name.Should().Be(expected.Name).And.HaveLength(110);
			normalized.Id.Should().Be(expected.Id);
			source.Identifier = Guid.NewGuid().ToString();
			new SharePointCredentialReference(source).Id.Should().NotBe(expected.Id);
			new SharePointCredentialReference(source).Name.Should().NotBe(expected.Name);
			source.TenantID = Guid.NewGuid().ToString();
			new SharePointCredentialReference(source).Name.Should().StartWith(Guid.Parse(source.TenantID).ToString("D") + "_");
		}

		[TestMethod]
		public void CollidingNameGuidOrCredentialType_IsRejectedBeforeMutation()
		{
			var source = CredentialPlatformFake.Source();
			var reference = new SharePointCredentialReference(source);
			foreach (var collision in new ALibraryCredential[]
			{
				new PasswordCredential(Guid.NewGuid(), reference.Name.ToUpperInvariant(), new List<int>()),
				new PasswordCredential(reference.Id, "unrelated", new List<int>()),
				new UserNameAndPasswordCredential(reference.Id, reference.Name, new List<int>()),
			})
			{
				var platform = new CredentialPlatformFake();
				platform.Credentials[collision.Info.GUID] = collision;
				new CredentialProvisioner(platform).Invoking(p => p.Save(source, "synthetic-secret")).Should().Throw<InvalidOperationException>();
				platform.Writes.Should().BeEmpty();
				platform.Assignments.Should().BeEmpty();
			}
		}

		[TestMethod]
		[DataRow("Write")]
		[DataRow("Assign")]
		public void ProvisioningStageFailure_IsNotSuccess(string stage)
		{
			var platform = new CredentialPlatformFake { FailureAt = stage };
			new CredentialProvisioner(platform).Invoking(p => p.Save(CredentialPlatformFake.Source(), "synthetic-secret"))
				.Should().Throw<InvalidOperationException>();
		}

		[TestMethod]
		[DataRow("")]
		[DataRow("not-a-guid")]
		[DataRow("00000000-0000-0000-0000-000000000000")]
		public void InvalidSourceIdentifier_IsRejected(string value)
		{
			var source = CredentialPlatformFake.Source();
			source.Identifier = value;
			Action action = () => new SharePointCredentialReference(source);
			action.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void PublicApi_HasNoMemberReturningASecret()
		{
			var publicMethods = typeof(Secrets).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

			publicMethods.Should().NotContain(m => m.ReturnType == typeof(string));
			typeof(Secrets).GetProperties(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
		}

		[TestMethod]
		public void PublicApi_DoesNotExposeSecretStoreTypes()
		{
			var assembly = typeof(Secrets).Assembly;
			var internalTypes = new[] { typeof(ISecretStore), typeof(ISecretReader), typeof(DpapiFileSecretStore), typeof(SecretKey) };

			internalTypes.Should().OnlyContain(t => !t.IsPublic);
			assembly.GetExportedTypes().Should().NotContain(t => t.Namespace == typeof(SecretKey).Namespace);
		}
	}
}
