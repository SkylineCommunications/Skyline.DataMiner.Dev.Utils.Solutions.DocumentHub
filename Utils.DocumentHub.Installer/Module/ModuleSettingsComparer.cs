namespace Skyline.DataMiner.Solutions.DocumentHub.Installer.Module
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Net.Apps.Modules;

	internal class ModuleSettingsComparer : IEqualityComparer<ModuleSettings>
	{
		public bool Equals(ModuleSettings x, ModuleSettings y)
		{
			if (x is null && y is null)
			{
				return true;
			}

			if (x is null || y is null)
			{
				return false;
			}

			if (ReferenceEquals(x, y))
			{
				return true;
			}

			return x.ModuleId == y.ModuleId &&
				x.DomManagerSettings.InformationEventSettings.Enable == y.DomManagerSettings.InformationEventSettings.Enable &&
				x.DomManagerSettings.DomInstanceHistorySettings.StorageBehavior == y.DomManagerSettings.DomInstanceHistorySettings.StorageBehavior;
		}

		public int GetHashCode(ModuleSettings obj)
		{
			if (obj is null)
			{
				return 0;
			}

			return obj.GetHashCode();
		}
	}
}