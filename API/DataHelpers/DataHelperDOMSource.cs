namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using DomHelpers.SlcDocumenthub;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	internal class DataHelperDOMSource : DataHelper<Models.Sources.DOMSource>
	{
		internal DataHelperDOMSource(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.Domsource)
		{
		}

		internal override Guid CreateOrUpdate(Models.Sources.DOMSource item)
		{
			var instance = new DomsourceInstance(New(item.ID));
			instance.DOMSourceInfo.Name = item.Name;
			instance.DOMSourceInfo.Module = item.Module;
			instance.DOMSourceInfo.NetworkSharePath = item.NetworkSharePath;
			instance.DOMSourceInfo.Credential = item.Credential;

			return CreateOrUpdateInstance(instance);
		}

		internal override bool TryDelete(IEnumerable<Models.Sources.DOMSource> items)
		{
			if (items == null)
			{
				return true;
			}

			var lst = items.ToList();
			if (lst.Count < 1)
			{
				return true;
			}

			bool b = TryDelete(lst.Where(i => i != null).Select(i => i.ID));

			return b;
		}

		internal override List<Models.Sources.DOMSource> Read(IEnumerable<DomInstance> domInstances)
		{
			var instances = domInstances.Select(x => new DomsourceInstance(x)).ToList();
			if (instances.Count < 1)
			{
				return new List<Models.Sources.DOMSource>();
			}

			return instances
				.Select(x =>
				{
					return new Models.Sources.DOMSource
					{
						ID = x.ID.Id,
						Name = x.DOMSourceInfo.Name,
						Module = x.DOMSourceInfo.Module,
						NetworkSharePath = x.DOMSourceInfo.NetworkSharePath,
						Credential = x.DOMSourceInfo.Credential,
					};
				})
				.ToList();
		}
	}
}
