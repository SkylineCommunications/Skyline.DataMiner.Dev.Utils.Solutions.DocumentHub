namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using FluentAssertions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Settings;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DomAttachmentsHandler_Tests
	{
		private const string ModuleId = "test-module";
		private IConnection connection = null!;
		private ModuleSettingsHelper settingsHelper = null!;
		private DomAttachmentsHandler handler = null!;

		[TestInitialize]
		public void Initialize()
		{
			connection = ConnectionHelper.CreateConnection();
			settingsHelper = new ModuleSettingsHelper(connection.HandleMessages);
			handler = new DomAttachmentsHandler(connection);
		}

		[TestMethod]
		[DataRow(@"\\server\share")]
		[DataRow(@"\\server\share\")]
		[DataRow(@"\\server\share\nested attachments")]
		public void GetAttachmentRootPath_ShouldUseConfiguredNetworkShare(string networkSharePath)
		{
			settingsHelper.ModuleSettings.Create(new ModuleSettings("another-module")
			{
				DomManagerSettings = new DomManagerSettings
				{
					DomInstanceNetworkAttachmentSettings = new DomInstanceNetworkAttachmentSettings
					{
						NetworkSharePath = @"\\different-server\other-share",
						CredentialId = Guid.NewGuid(),
					},
				},
			});
			settingsHelper.ModuleSettings.Create(new ModuleSettings(ModuleId)
			{
				DomManagerSettings = new DomManagerSettings
				{
					DomInstanceNetworkAttachmentSettings = new DomInstanceNetworkAttachmentSettings
					{
						NetworkSharePath = networkSharePath,
						CredentialId = Guid.NewGuid(),
					},
				},
			});

			var root = handler.GetAttachmentRootPath(ModuleId);

			root.Should().Be(Path.Combine(networkSharePath.TrimEnd('\\'), "DOMINSTANCE_ATTACHMENTS", ModuleId));
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		public void GetAttachmentRootPath_WhenNetworkShareIsEmpty_ShouldUseLocalStorage(string? networkSharePath)
		{
			settingsHelper.ModuleSettings.Create(new ModuleSettings(ModuleId)
			{
				DomManagerSettings = new DomManagerSettings
				{
					DomInstanceNetworkAttachmentSettings = new DomInstanceNetworkAttachmentSettings
					{
						NetworkSharePath = networkSharePath,
					},
				},
			});

			var root = handler.GetAttachmentRootPath(ModuleId);

			root.Should().Be(@"C:\Skyline DataMiner\Documents\DOMINSTANCE_ATTACHMENTS\test-module");
		}

		[TestMethod]
		public void GetAttachmentRootPath_WhenNetworkSettingsAreMissing_ShouldUseLocalStorage()
		{
			settingsHelper.ModuleSettings.Create(new ModuleSettings(ModuleId));

			var root = handler.GetAttachmentRootPath(ModuleId);

			root.Should().Be(@"C:\Skyline DataMiner\Documents\DOMINSTANCE_ATTACHMENTS\test-module");
		}

		[TestMethod]
		public void GetAttachmentRootPath_WhenModuleSettingsAreMissing_ShouldThrow()
		{
			Action act = () => handler.GetAttachmentRootPath(ModuleId);

			act.Should().Throw<InvalidOperationException>().WithMessage("*test-module*");
		}
	}
}
