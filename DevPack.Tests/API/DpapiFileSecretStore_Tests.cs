namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using System.Text;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DpapiFileSecretStore_Tests
	{
		private string _root = null!;
		private DpapiFileSecretStore _store = null!;

		[TestInitialize]
		public void Setup()
		{
			_root = Path.Combine(Path.GetTempPath(), "DocHubSecretTests_" + Guid.NewGuid().ToString("N"));
			_store = new DpapiFileSecretStore(_root);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_root))
			{
				Directory.Delete(_root, true);
			}
		}

		[TestMethod]
		public void LegacyStore_RejectsAllNewWrites()
		{
			_store.Invoking(s => s.Save(SharePointSecrets.ClientSecret(Guid.NewGuid(), Guid.NewGuid()), "synthetic-secret"))
				.Should().Throw<NotSupportedException>();
			Directory.Exists(_root).Should().BeFalse();
		}

		[TestMethod]
		public void LegacyRead_DecryptsCorrectScopeAndDeletesOnlyReadSnapshot()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid(), Guid.NewGuid());
			WriteFixture(key, "synthetic-legacy-secret");
			_store.Read(key).Should().Be("synthetic-legacy-secret");
			_store.Delete(key);
			_store.Exists(key).Should().BeFalse();
		}

		[TestMethod]
		public void LegacyDelete_WithoutReadOrAfterConcurrentWriteRetainsData()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid(), Guid.NewGuid());
			WriteFixture(key, "before");
			_store.Invoking(s => s.Delete(key)).Should().Throw<InvalidOperationException>();
			_store.Read(key).Should().Be("before");
			WriteFixture(key, "concurrent-change");
			_store.Invoking(s => s.Delete(key)).Should().Throw<IOException>();
			_store.Read(key).Should().Be("concurrent-change");
		}

		[TestMethod]
		public void LegacyRead_CopiedCiphertextCannotBeUsedForAnotherOrganization()
		{
			var first = SharePointSecrets.ClientSecret(Guid.NewGuid(), Guid.NewGuid());
			var second = SharePointSecrets.ClientSecret(Guid.NewGuid(), Guid.NewGuid());
			WriteFixture(first, "synthetic-legacy-secret");
			var target = Path.Combine(_root, second.RelativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			File.Copy(Path.Combine(_root, first.RelativePath), target);
			_store.Invoking(s => s.Read(second)).Should().Throw<CryptographicException>();
		}

		[TestMethod]
		public void LegacyRead_InterruptedWriterMustBeResolvedBeforeMigration()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid(), Guid.NewGuid());
			WriteFixture(key, "synthetic-legacy-secret");
			File.WriteAllText(Path.Combine(_root, key.RelativePath) + ".tmp", "unfinished-ciphertext");
			_store.Invoking(s => s.Read(key)).Should().Throw<IOException>();
			_store.Exists(key).Should().BeTrue();
		}

		[TestMethod]
		public void GlobalLegacyFile_RequiresExplicitMigrationAndAssignedConsumer()
		{
			Directory.CreateDirectory(_root);
			var path = Path.Combine(_root, "graphsecret.dat");
			File.WriteAllText(path, Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes("synthetic-global-secret"), null, DataProtectionScope.LocalMachine)));
			var source = CredentialPlatformFake.Source();
			var platform = new CredentialPlatformFake();
			var migration = new CredentialMigration(platform, _store, c => true);
			migration.Invoking(m => m.Migrate(new[] { source }, false)).Should().Throw<InvalidOperationException>();
			File.Exists(path).Should().BeTrue();
			platform.FailureAt = "Assign";
			migration.Invoking(m => m.Migrate(new[] { source }, true)).Should().Throw<InvalidOperationException>();
			File.Exists(path).Should().BeTrue();
			platform.FailureAt = null;
			migration.Migrate(new[] { source }, true).Should().Be(1);
			File.Exists(path).Should().BeFalse();
		}

		[TestMethod]
		public void GlobalLegacyFile_WithMultipleRegistrationsIsRetained()
		{
			Directory.CreateDirectory(_root);
			var path = Path.Combine(_root, "graphsecret.dat");
			File.WriteAllText(path, "retained-encrypted-content");
			var first = CredentialPlatformFake.Source();
			var second = CredentialPlatformFake.Source(Guid.NewGuid().ToString());
			second.TenantID = Guid.NewGuid().ToString();
			var platform = new CredentialPlatformFake();
			new CredentialProvisioner(platform).Save(first, "first");
			new CredentialProvisioner(platform).Save(second, "second");
			new CredentialMigration(platform, _store, c => true).Invoking(m => m.Migrate(new[] { first, second }, true))
				.Should().Throw<InvalidOperationException>();
			File.ReadAllText(path).Should().Be("retained-encrypted-content");
		}

		private void WriteFixture(SecretKey key, string value)
		{
			var path = Path.Combine(_root, key.RelativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(key.Entropy), DataProtectionScope.LocalMachine);
			File.WriteAllText(path, Convert.ToBase64String(cipher));
		}
	}
}
