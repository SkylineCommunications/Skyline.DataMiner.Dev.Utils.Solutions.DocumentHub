namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using DomHelpers.SlcDocumenthub;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal class DataHelperDomSource : DataHelper<DomSource>
	{
		internal DataHelperDomSource(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.Domsource)
		{
		}

		internal override Guid CreateOrUpdate(DomSource item)
		{
			var instance = new DomsourceInstance(New(Guid.Parse(item.Identifier)));
			instance.DOMSourceInfo.Name = item.Name;
			instance.DOMSourceInfo.Module = item.Module;
			instance.DOMSourceInfo.NetworkSharePath = item.NetworkSharePath;
			instance.DOMSourceInfo.Credential = item.Credential;

			return CreateOrUpdateInstance(instance);
		}

		internal override bool TryDelete(IEnumerable<DomSource> items)
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

			bool b = TryDelete(lst.Where(i => i != null));

			return b;
		}

		internal override List<DomSource> Read(IEnumerable<DomInstance> domInstances)
		{
			var instances = domInstances.Select(x => new DomsourceInstance(x)).ToList();
			if (instances.Count < 1)
			{
				return new List<DomSource>();
			}

			return instances
				.Select(x =>
				{
					return new DomSource
					{
						Identifier = x.ID.Id.ToString(),
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
