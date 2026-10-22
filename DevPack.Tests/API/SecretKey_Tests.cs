namespace DevPack.Tests.API
{
	using System;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	[TestClass]
	public class SecretKey_Tests
	{
		private static readonly Guid ConfigId = new Guid("11111111-2222-3333-4444-555555555555");

		[TestMethod]
		public void RelativePath_WithScope_UsesStorageScopeAndName()
		{
			var key = new SecretKey(StorageType.SharePoint, ConfigId, "clientsecret");

			key.RelativePath.Should().Be(@"sharepoint\11111111-2222-3333-4444-555555555555\clientsecret.dat");
		}

		[TestMethod]
		public void RelativePath_GlobalScope_OmitsScopeFolder()
		{
			var key = new SecretKey(StorageType.SharePoint, Guid.Empty, "globalsettings");

			key.RelativePath.Should().Be(@"sharepoint\globalsettings.dat");
		}

		[TestMethod]
		public void Entropy_DiffersPerScopeAndName()
		{
			var a = new SecretKey(StorageType.SharePoint, ConfigId, "clientsecret");
			var otherScope = new SecretKey(StorageType.SharePoint, Guid.NewGuid(), "clientsecret");
			var otherName = new SecretKey(StorageType.SharePoint, ConfigId, "clientsecret2");

			a.Entropy.Should().NotBe(otherScope.Entropy);
			a.Entropy.Should().NotBe(otherName.Entropy);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("..")]
		[DataRow("..\\evil")]
		[DataRow("a/b")]
		[DataRow("a\\b")]
		[DataRow("Upper")]
		[DataRow("with space")]
		[DataRow("name.dat")]
		public void Constructor_InvalidName_Throws(string name)
		{
			Action act = () => new SecretKey(StorageType.SharePoint, ConfigId, name);

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void SharePointSecrets_ClientSecret_BuildsExpectedKey()
		{
			var key = SharePointSecrets.ClientSecret(ConfigId);

			key.Storage.Should().Be(StorageType.SharePoint);
			key.Scope.Should().Be(ConfigId);
			key.Name.Should().Be("clientsecret");
		}
	}
}
