namespace DevPack.Tests.API
{
	using System;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class ManageSharePointSecretScript_Tests
	{
		[TestMethod]
		[DataRow("Write")]
		[DataRow("Assign")]
		[DataRow("Connection")]
		public void Migration_FailureRetainsLegacyFile(string stage)
		{
			var source = CredentialPlatformFake.Source();
			var key = SharePointSecrets.ClientSecret(source.TenantID, source.ClientID);
			var legacy = new InMemorySecretStore();
			legacy.Save(key, "legacy-synthetic-secret");
			var platform = new CredentialPlatformFake { FailureAt = stage };
			var migration = new CredentialMigration(platform, legacy, c => stage != "Connection");
			migration.Invoking(m => m.Migrate(new[] { source }, false)).Should().Throw<InvalidOperationException>();
			legacy.Exists(key).Should().BeTrue();
		}

		[TestMethod]
		public void Migration_SharedLegacyPairIsRemovedOnlyAfterEverySourceIsVerified()
		{
			var first = CredentialPlatformFake.Source();
			var second = CredentialPlatformFake.Source(Guid.NewGuid().ToString());
			var key = SharePointSecrets.ClientSecret(first.TenantID, first.ClientID);
			var legacy = new InMemorySecretStore();
			legacy.Save(key, "legacy-synthetic-secret");
			var platform = new CredentialPlatformFake();
			var checkedSources = 0;
			var migration = new CredentialMigration(platform, legacy, c =>
			{
				legacy.Exists(key).Should().BeTrue();
				checkedSources++;
				return c.Identifier != second.Identifier;
			});
			migration.Invoking(m => m.Migrate(new[] { first, second }, false)).Should().Throw<InvalidOperationException>();
			legacy.Exists(key).Should().BeTrue();
			checkedSources.Should().Be(2);
			new CredentialMigration(platform, legacy, c => true).Migrate(new[] { first, second }, false).Should().Be(1);
			legacy.Exists(key).Should().BeFalse();
			platform.Credentials.Should().HaveCount(2);
		}

		[TestMethod]
		public void Migration_ExistingRotatedNativeSecretIsNeverReplacedWithLegacyValue()
		{
			var source = CredentialPlatformFake.Source();
			var platform = new CredentialPlatformFake();
			new CredentialProvisioner(platform).Save(source, "rotated-native-secret");
			var legacy = new InMemorySecretStore();
			var key = SharePointSecrets.ClientSecret(source.TenantID, source.ClientID);
			legacy.Save(key, "stale-legacy-secret");
			new CredentialMigration(platform, legacy, c => true).Migrate(new[] { source }, false).Should().Be(1);
			platform.Passwords[new SharePointCredentialReference(source).Id].Should().Be("rotated-native-secret");
			platform.Writes.Should().ContainSingle();
		}

		[TestMethod]
		public void Migration_MissingLegacyAndNativeSecretIsAnExplicitFailure()
		{
			var migration = new CredentialMigration(new CredentialPlatformFake(), new InMemorySecretStore(), c => true);
			migration.Invoking(m => m.Migrate(new[] { CredentialPlatformFake.Source() }, false)).Should().Throw<InvalidOperationException>();
		}
	}
}
