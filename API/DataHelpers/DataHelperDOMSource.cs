namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using DomHelpers.SlcDocumenthub;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	internal class DataHelperDomSource : DataHelper<Models.Sources.DomSource>
	{
		internal DataHelperDomSource(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.Domsource)
		{
		}

		internal override Guid CreateOrUpdate(Models.Sources.DomSource item)
		{
			var instance = new DomsourceInstance(New(item.ID));
			instance.DOMSourceInfo.Name = item.Name;
			instance.DOMSourceInfo.Module = item.Module;
			instance.DOMSourceInfo.NetworkSharePath = item.NetworkSharePath;
			instance.DOMSourceInfo.Credential = item.Credential;

			return CreateOrUpdateInstance(instance);
		}

		internal override bool TryDelete(IEnumerable<Models.Sources.DomSource> items)
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

		internal override List<Models.Sources.DomSource> Read(IEnumerable<DomInstance> domInstances)
		{
			var instances = domInstances.Select(x => new DomsourceInstance(x)).ToList();
			if (instances.Count < 1)
			{
				return new List<Models.Sources.DomSource>();
			}

			return instances
				.Select(x =>
				{
					return new Models.Sources.DomSource
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
