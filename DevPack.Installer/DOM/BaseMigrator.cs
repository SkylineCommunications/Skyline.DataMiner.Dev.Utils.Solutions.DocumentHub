namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	internal abstract class BaseMigrator
	{
		private readonly DomHelper _helper;
		private readonly Action<string> _logMethod;
		private readonly IDictionary<Shared.Version, DomMigration> _migrations;

		protected BaseMigrator(
			IConnection connection,
			string moduleId,
			IDictionary<Shared.Version, DomMigration> migrations = null,
			Action<string> logMethod = null)
		{
			_helper = new DomHelper(connection.HandleMessages, moduleId);
			_logMethod = logMethod;
			_migrations = migrations ?? new Dictionary<Shared.Version, DomMigration>();
		}

		public DomHelper Helper { get => _helper; }

		public void RunMigration(Shared.Version version)
		{
			if (!_migrations.ContainsKey(version))
			{
				return;
			}

			var migration = _migrations[version];
			migration.Migrate();
		}

		////protected Shared.Version GetCurrentVersion()
		////{
		////	var currentVersion = Shared.Version.FromString(
		////		Helper.DomInstances
		////		.Read(DomInstanceExposers.Id.Equal(Constants.Solution.Guid))
		////		.FirstOrDefault()?
		////		.GetFieldValue<string>(
		////			SolutionRegistrationDomMapper.SolutionRegistrationProperties.SectionDefinitionId,
		////			SolutionRegistrationDomMapper.SolutionRegistrationProperties.Version)
		////		.GetValue() ?? "0.0.0");

		////	return currentVersion;
		////}

		protected void Log(string message)
		{
			_logMethod?.Invoke($"|BaseMigrator|DevPack.Installer: {message}");
		}
	}
}