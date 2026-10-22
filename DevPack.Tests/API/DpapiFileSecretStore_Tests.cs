namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;

	[TestClass]
	public class DpapiFileSecretStore_Tests
	{
		private string _root;
		private DpapiFileSecretStore _store;

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
		public void SaveThenRead_ReturnsOriginalSecret()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());

			_store.Save(key, "my-secret");

			_store.Read(key).Should().Be("my-secret");
		}

		[TestMethod]
		public void Save_DoesNotStorePlainText()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());

			_store.Save(key, "my-secret");

			var content = File.ReadAllText(Path.Combine(_root, key.RelativePath));
			content.Should().NotContain("my-secret");
		}

		[TestMethod]
		public void Save_ExistingSecret_ReplacesValueAndLeavesNoTempFile()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());
			_store.Save(key, "old");

			_store.Save(key, "new");

			_store.Read(key).Should().Be("new");
			File.Exists(Path.Combine(_root, key.RelativePath) + ".tmp").Should().BeFalse();
		}

		[TestMethod]
		public void Save_EmptyValue_Throws()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());

			Action act = () => _store.Save(key, string.Empty);

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void Read_MissingSecret_ThrowsFileNotFound()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());

			Action act = () => _store.Read(key);

			act.Should().Throw<FileNotFoundException>();
		}

		[TestMethod]
		public void Exists_ReflectsSaveAndDelete()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());
			_store.Exists(key).Should().BeFalse();

			_store.Save(key, "my-secret");
			_store.Exists(key).Should().BeTrue();

			_store.Delete(key);
			_store.Exists(key).Should().BeFalse();
		}

		[TestMethod]
		public void Delete_MissingSecret_DoesNotThrow()
		{
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());

			Action act = () => _store.Delete(key);

			act.Should().NotThrow();
		}

		[TestMethod]
		public void Secrets_AreIsolatedPerScope()
		{
			var keyA = SharePointSecrets.ClientSecret(Guid.NewGuid());
			var keyB = SharePointSecrets.ClientSecret(Guid.NewGuid());

			_store.Save(keyA, "secret-a");
			_store.Save(keyB, "secret-b");

			_store.Read(keyA).Should().Be("secret-a");
			_store.Read(keyB).Should().Be("secret-b");
		}

		[TestMethod]
		public void Read_FileCopiedToAnotherKey_FailsToDecrypt()
		{
			var keyA = SharePointSecrets.ClientSecret(Guid.NewGuid());
			var keyB = SharePointSecrets.ClientSecret(Guid.NewGuid());
			_store.Save(keyA, "secret-a");

			var pathB = Path.Combine(_root, keyB.RelativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(pathB));
			File.Copy(Path.Combine(_root, keyA.RelativePath), pathB);

			Action act = () => _store.Read(keyB);

			act.Should().Throw<CryptographicException>();
		}

		[TestMethod]
		public void NullKey_Throws()
		{
			Action save = () => _store.Save(null, "x");
			Action read = () => _store.Read(null);
			Action exists = () => _store.Exists(null);
			Action delete = () => _store.Delete(null);

			save.Should().Throw<ArgumentNullException>();
			read.Should().Throw<ArgumentNullException>();
			exists.Should().Throw<ArgumentNullException>();
			delete.Should().Throw<ArgumentNullException>();
		}
	}
}
