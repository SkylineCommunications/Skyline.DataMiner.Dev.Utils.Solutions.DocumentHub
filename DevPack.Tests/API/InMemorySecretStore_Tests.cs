namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;

	[TestClass]
	public class InMemorySecretStore_Tests
	{
		[TestMethod]
		public void SaveReadExistsDelete_BehavesAsExpected()
		{
			var store = new InMemorySecretStore();
			var key = SharePointSecrets.ClientSecret(Guid.NewGuid());

			store.Exists(key).Should().BeFalse();

			store.Save(key, "value");
			store.Exists(key).Should().BeTrue();
			store.Read(key).Should().Be("value");

			store.Delete(key);
			store.Exists(key).Should().BeFalse();
		}

		[TestMethod]
		public void Read_MissingSecret_ThrowsFileNotFound()
		{
			var store = new InMemorySecretStore();

			Action act = () => store.Read(SharePointSecrets.ClientSecret(Guid.NewGuid()));

			act.Should().Throw<FileNotFoundException>();
		}
	}
}
