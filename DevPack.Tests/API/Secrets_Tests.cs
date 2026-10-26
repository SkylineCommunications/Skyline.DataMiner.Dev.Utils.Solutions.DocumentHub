namespace DevPack.Tests.API
{
	using System;
	using System.Reflection;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class Secrets_Tests
	{
		private const string TenantId = "aaaaaaaa-0000-0000-0000-000000000001";
		private const string ClientId = "bbbbbbbb-0000-0000-0000-000000000002";

		[TestMethod]
		public void ApplyOnThisAgent_Save_StoresUnderTenantAndClient()
		{
			var store = new InMemorySecretStore();
			var secrets = new Secrets(store);

			secrets.ApplyOnThisAgent(Secrets.SaveAction, TenantId, ClientId, "my-secret");

			store.Read(SharePointSecrets.ClientSecret(TenantId, ClientId)).Should().Be("my-secret");
			secrets.HasSharePointClientSecret(TenantId, ClientId).Should().BeTrue();
		}

		[TestMethod]
		public void ApplyOnThisAgent_Delete_RemovesSecret()
		{
			var secrets = new Secrets(new InMemorySecretStore());
			secrets.ApplyOnThisAgent(Secrets.SaveAction, TenantId, ClientId, "my-secret");

			secrets.ApplyOnThisAgent(Secrets.DeleteAction, TenantId, ClientId, null);

			secrets.HasSharePointClientSecret(TenantId, ClientId).Should().BeFalse();
		}

		[TestMethod]
		public void SaveAndDeleteSharePointClientSecret_RunOnAllAgents()
		{
			var distributor = new Moq.Mock<IAgentSecretDistributor>();
			var store = new InMemorySecretStore();
			var secrets = new Secrets(store, distributor.Object);

			secrets.SaveSharePointClientSecret(TenantId, ClientId, "my-secret");
			secrets.DeleteSharePointClientSecret(TenantId, ClientId);

			distributor.Verify(d => d.Execute(Secrets.SaveAction, TenantId, ClientId, "my-secret"), Moq.Times.Once());
			distributor.Verify(d => d.Execute(Secrets.DeleteAction, TenantId, ClientId, null), Moq.Times.Once());
			secrets.HasSharePointClientSecret(TenantId, ClientId).Should().BeFalse();
		}

		[TestMethod]
		public void SaveSharePointClientSecret_InvalidInput_Throws()
		{
			var secrets = new Secrets(new InMemorySecretStore(), new Moq.Mock<IAgentSecretDistributor>().Object);

			((Action)(() => secrets.SaveSharePointClientSecret("x", ClientId, "s"))).Should().Throw<ArgumentException>();
			((Action)(() => secrets.SaveSharePointClientSecret(TenantId, ClientId, string.Empty))).Should().Throw<ArgumentException>();
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
