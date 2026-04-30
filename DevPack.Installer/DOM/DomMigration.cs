namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.DocumentHub.Installer.InstallerException;

	public abstract class DomMigration
	{
		private readonly DomHelper _helper;
		private readonly Action<string> _logMethod;

		protected DomMigration(IConnection connection, string moduleId, Action<string> logMethod = null)
		{
			_helper = new DomHelper(connection.HandleMessages, moduleId);
			_logMethod = logMethod;
		}

		public abstract void Migrate();

		protected void Log(string message)
		{
			_logMethod?.Invoke($"SDM.DocumentHub.Installer: {message}");
		}

		protected SectionDefinition Get(FilterElement<SectionDefinition> equalityFilter)
		{
			return Get(_helper.SectionDefinitions, equalityFilter);
		}

		protected DomDefinition Get(FilterElement<DomDefinition> equalityFilter)
		{
			return Get(_helper.DomDefinitions, equalityFilter);
		}

		protected DomInstance Get(FilterElement<DomInstance> equalityFilter)
		{
			return Get(_helper.DomInstances, equalityFilter);
		}

		protected void Import(FilterElement<SectionDefinition> equalityFilter, SectionDefinition dataType)
		{
			Import(_helper.SectionDefinitions, equalityFilter, dataType);
		}

		protected void Import(FilterElement<DomDefinition> equalityFilter, DomDefinition dataType)
		{
			Import(_helper.DomDefinitions, equalityFilter, dataType);
		}

		protected void Import(FilterElement<DomInstance> equalityFilter, DomInstance dataType)
		{
			Import(_helper.DomInstances, equalityFilter, dataType);
		}

		private static T Get<T>(ICrudHelperComponent<T> crudHelperComponent, FilterElement<T> equalityFilter)
			where T : DataType
		{
			var dataTypes = crudHelperComponent.Read(equalityFilter);
			if (dataTypes is null || dataTypes.Count == 0)
			{
				throw new InstallerException($"Could not find {typeof(T).Name} with filter: {equalityFilter}");
			}

			if (dataTypes.Count > 1)
			{
				throw new InstallerException($"Multiple {typeof(T).Name} instances found with filter: {equalityFilter}");
			}

			return dataTypes[0];
		}

		private static void Import<T>(ICrudHelperComponent<T> crudHelperComponent, FilterElement<T> equalityFilter, T dataType)
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