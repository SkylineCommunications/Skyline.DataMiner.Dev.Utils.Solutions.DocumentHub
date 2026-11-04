namespace DevPack.Tests.API
{
	using System;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;
	using Script = SLC_DH_AS_ManageSharePointSecret.Script;

	/// <summary>
	/// Tests the per-agent logic of <c>SLC-DH-AS-ManageSharePointSecret</c>, which is compiled in from the script source.
	/// </summary>
	[TestClass]
	public class ManageSharePointSecretScript_Tests
	{
		private const string TenantId = "aaaaaaaa-0000-0000-0000-000000000001";
		private const string ClientId = "bbbbbbbb-0000-0000-0000-000000000002";

		[TestMethod]
		public void Apply_Save_StoresUnderTenantAndClient()
		{
			var store = new InMemorySecretStore();

			Script.Apply(store, Script.SaveAction, TenantId, ClientId, "my-secret");

			store.Read(SharePointSecrets.ClientSecret(TenantId, ClientId)).Should().Be("my-secret");
		}

		[TestMethod]
		public void Apply_EmptyAction_DefaultsToSave()
		{
			var store = new InMemorySecretStore();

			Script.Apply(store, null, TenantId, ClientId, "my-secret");

			store.Exists(SharePointSecrets.ClientSecret(TenantId, ClientId)).Should().BeTrue();
		}

		[TestMethod]
		public void Apply_Delete_RemovesSecret()
		{
			var store = new InMemorySecretStore();
			Script.Apply(store, Script.SaveAction, TenantId, ClientId, "my-secret");

			Script.Apply(store, Script.DeleteAction, TenantId, ClientId, null);

			store.Exists(SharePointSecrets.ClientSecret(TenantId, ClientId)).Should().BeFalse();
		}

		[TestMethod]
		public void Apply_InvalidInput_Throws()
		{
			var store = new InMemorySecretStore();

			((Action)(() => Script.Apply(store, Script.SaveAction, TenantId, ClientId, string.Empty))).Should().Throw<ArgumentException>();
			((Action)(() => Script.Apply(store, "Rotate", TenantId, ClientId, "s"))).Should().Throw<ArgumentException>();
			((Action)(() => Script.Apply(store, Script.SaveAction, "x", ClientId, "s"))).Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void ActionConstants_MatchDevPackContract()
		{
			Script.SaveAction.Should().Be(Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Secrets.SaveAction);
			Script.DeleteAction.Should().Be(Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Secrets.DeleteAction);
		}
	}
}
