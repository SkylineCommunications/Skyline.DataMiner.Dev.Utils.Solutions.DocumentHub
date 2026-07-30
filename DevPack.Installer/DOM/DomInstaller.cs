namespace DevPack.Installer.DOM
{
	using System;
	using System.Linq;
	using DevPack.Installer.DOM.DocumentBucket_Definition;
	using DevPack.Installer.DOM.DomSource_Definition;
	using DevPack.Installer.DOM.Sharepoint_Definition;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Utils.DOM.Builders;

	public class DomInstaller
	{
		private static readonly Shared.Version[] _versions = new[]
		{
			new Shared.Version(1, 0, 0),
		};

		private readonly IConnection _connection;
		private readonly Action<string> _logMethod;

		public DomInstaller(IConnection connection, Action<string> logMethod = null)
		{
			this._connection = connection;
			_logMethod = logMethod;
		}

		public void InstallDefaultContent()
		{
			Log("Installation for SDM DocumentHub started...");

			var moduleHelper = new ModuleSettingsHelper(_connection.HandleMessages);
			var existingSettings = moduleHelper.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal(DocumentBucketDomMapper.ModuleId)).SingleOrDefault();

			// Always (re)apply the module settings so the DOM definition stays current on every deploy.
			Log("Installing Module Settings...");
			var module = new DomModuleBuilder()
					.WithModuleId(DocumentBucketDomMapper.ModuleId)
					.WithInformationEvents(false)
					.WithHistory(true)
					.Build();

			// Preserve the admin-configured network attachment settings (share path + credential)
			// from the existing module so they are not cleared when we update the settings.
			if (existingSettings?.DomManagerSettings?.DomInstanceNetworkAttachmentSettings != null)
			{
				module.DomManagerSettings.DomInstanceNetworkAttachmentSettings = existingSettings.DomManagerSettings.DomInstanceNetworkAttachmentSettings;
				Log("Preserved existing network attachment settings (share path and credential).");
			}

			if (existingSettings == null)
			{
				moduleHelper.ModuleSettings.Create(module);
			}
			else
			{
				moduleHelper.ModuleSettings.Update(module);
			}

			Log("Installed Module Settings");

			var documentBucketInstaller = new DocumentBucketInstaller(_connection, _logMethod);
			var sharepointInstaller = new SharepointInstaller(_connection, _logMethod);
			var domSourceInstaller = new DomSourceInstaller(_connection, _logMethod);

			documentBucketInstaller.RunMigration(_versions[0]);
			sharepointInstaller.RunMigration(_versions[0]);
			domSourceInstaller.RunMigration(_versions[0]);
		}

		internal void Log(string message)
		{
			_logMethod?.Invoke($"|DomInstaller|DevPack.Installer: {message}");
		}
	}
}