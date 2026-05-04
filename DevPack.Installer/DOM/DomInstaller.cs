namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM.DocumentCategory_Definition;
	using Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM.DomSource_Definition;
	using Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM.Sharepoint_Definition;
	using Skyline.DataMiner.Solutions.DocumentHub.Installer.Module;
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
			var moduleComparer = new ModuleSettingsComparer();
			var moduleSettings = moduleHelper.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal(DocumentCategoryDomMapper.ModuleId)).SingleOrDefault();
			var module = new DomModuleBuilder()
					.WithModuleId(DocumentCategoryDomMapper.ModuleId)
					.WithInformationEvents(false)
					.WithHistory(true)
					.Build();

			// If the module settings differ import it
			// The comparer is not exhaustive it only checks for the properties we care about
			if (moduleSettings == null || moduleComparer.Equals(moduleSettings, module))
			{
				Log("Installing Module Settings...");
				Import(moduleHelper.ModuleSettings, ModuleSettingsExposers.ModuleId.Equal(DocumentCategoryDomMapper.ModuleId), module);
				Log("Installed Module Settings");
			}

			var documentCategoryInstaller = new DocumentCategoryInstaller(_connection, _logMethod);
			var sharepointInstaller = new SharepointInstaller(_connection, _logMethod);
			var domSourceInstaller = new DomSourceInstaller(_connection, _logMethod);

			documentCategoryInstaller.RunMigration(_versions[0]);
			sharepointInstaller.RunMigration(_versions[0]);
			domSourceInstaller.RunMigration(_versions[0]);
		}

		internal void Log(string message)
		{
			_logMethod?.Invoke($"|DomInstaller|DevPack.Installer: {message}");
		}

		private void Import<T>(ICrudHelperComponent<T> crudHelperComponent, FilterElement<T> equalityFilter, T dataType)
			where T : DataType
		{
			bool exists = crudHelperComponent.Read(equalityFilter).Any();

			if (exists)
			{
				crudHelperComponent.Update(dataType);
			}
			else
			{
				crudHelperComponent.Create(dataType);
			}
		}
	}
}