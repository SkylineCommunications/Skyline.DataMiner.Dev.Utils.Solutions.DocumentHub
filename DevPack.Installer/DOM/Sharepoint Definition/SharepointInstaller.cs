namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM.Sharepoint_Definition
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.Installer.DOM;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal class SharepointInstaller : BaseMigrator
	{
		public SharepointInstaller(IConnection connection, Action<string> logMethod = null)
			: base(
				connection,
				SharePointConfigurationDomMapper.ModuleId,
				new Dictionary<Shared.Version, DomMigration>
				{
					[new Shared.Version(1, 0, 0)] = new Sharepoint_V1_0_0(connection, logMethod),
				},
				logMethod)
		{
		}
	}
}