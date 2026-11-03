namespace DevPack.Tests.API
{
	using System;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;

	[TestClass]
	public class SecretKey_Tests
	{
		private static readonly Guid ConfigId = new Guid("11111111-2222-3333-4444-555555555555");

		[TestMethod]
		public void RelativePath_WithScope_UsesStorageScopeAndName()
		{
			var key = new SecretKey("sharepoint", "clientsecret", ConfigId.ToString("D"));

			key.RelativePath.Should().Be(@"sharepoint\11111111-2222-3333-4444-555555555555\clientsecret.dat");
		}

		[TestMethod]
		public void RelativePath_GlobalScope_OmitsScopeFolder()
		{
			var key = new SecretKey("sharepoint", "globalsettings");

			key.RelativePath.Should().Be(@"sharepoint\globalsettings.dat");
		}

		[TestMethod]
		public void Entropy_DiffersPerScopeAndName()
		{
			var a = new SecretKey("sharepoint", "clientsecret", ConfigId.ToString("D"));
			var otherScope = new SecretKey("sharepoint", "clientsecret", Guid.NewGuid().ToString("D"));
			var otherName = new SecretKey("sharepoint", "clientsecret2", ConfigId.ToString("D"));

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
			Action act = () => new SecretKey("sharepoint", name, ConfigId.ToString("D"));

			act.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void RelativePath_MultipleScopes_NestsFolders()
		{
			var key = new SecretKey("github", "token", "org", "repo");

			key.RelativePath.Should().Be(@"github\org\repo\token.dat");
			key.Entropy.Should().Be("DocumentHub|github|org|repo|token");
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("..")]
		[DataRow("SharePoint")]
		public void Constructor_InvalidSourceOrScope_Throws(string segment)
		{
			((Action)(() => new SecretKey(segment, "token"))).Should().Throw<ArgumentException>();
			((Action)(() => new SecretKey("github", "token", segment))).Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void SharePointSecrets_ClientSecret_BuildsTenantClientPath()
		{
			var tenant = new Guid("aaaaaaaa-0000-0000-0000-000000000001");
			var client = new Guid("bbbbbbbb-0000-0000-0000-000000000002");

			var key = SharePointSecrets.ClientSecret(tenant.ToString(), client.ToString().ToUpperInvariant());

			key.Source.Should().Be("sharepoint");
			key.Scopes.Should().Equal(tenant.ToString("D"));
			key.RelativePath.Should().Be(@"sharepoint\aaaaaaaa-0000-0000-0000-000000000001\bbbbbbbb-0000-0000-0000-000000000002.dat");
		}

		[TestMethod]
		[DataRow("not-a-guid", "bbbbbbbb-0000-0000-0000-000000000002")]
		[DataRow("aaaaaaaa-0000-0000-0000-000000000001", "..\\evil")]
		[DataRow("00000000-0000-0000-0000-000000000000", "bbbbbbbb-0000-0000-0000-000000000002")]
		[DataRow(null, "bbbbbbbb-0000-0000-0000-000000000002")]
		public void SharePointSecrets_ClientSecret_InvalidIds_Throws(string tenantId, string clientId)
		{
			Action act = () => SharePointSecrets.ClientSecret(tenantId, clientId);

			act.Should().Throw<ArgumentException>();
		}
	}
}
