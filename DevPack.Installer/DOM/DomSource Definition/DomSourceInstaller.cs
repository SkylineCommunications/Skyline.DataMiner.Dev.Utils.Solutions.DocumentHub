namespace DevPack.Installer.DOM.DomSource_Definition
{
	using System;
	using System.Collections.Generic;
	using DevPack.Installer.DOM;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal class DomSourceInstaller : BaseMigrator
	{
		public DomSourceInstaller(IConnection connection, Action<string> logMethod = null)
			: base(
				connection,
				DomSourceDomMapper.ModuleId,
				new Dictionary<Shared.Version, DomMigration>
				{
					[new Shared.Version(1, 0, 0)] = new DomSource_V1_0_0(connection, logMethod),
				},
				logMethod)
		{
		}
	}
}