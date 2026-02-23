namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using DomHelpers.SlcDocumenthub;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	internal class DataHelperSharePointConfiguration : DataHelper<Models.Sources.SharePointConfiguration>
    {
        internal DataHelperSharePointConfiguration(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.Sharepoint)
        {
        }

        internal override Guid CreateOrUpdate(Models.Sources.SharePointConfiguration item)
        {
            var instance = new SharepointInstance(New(item.ID));
            instance.Configuration.SiteURL = item.SiteURL;
            instance.Configuration.TenantID = item.TenantID;
            instance.Configuration.ClientID = item.ClientID;
            instance.Configuration.ClientSecret = item.ClientSecret;
            instance.Configuration.DocumentLibraryName = item.DocumentLibraryName;

            return CreateOrUpdateInstance(instance);
        }

        internal override List<Models.Sources.SharePointConfiguration> Read(IEnumerable<DomInstance> domInstances)
        {
            var instances = domInstances.Select(x => new SharepointInstance(x)).ToList();
            if (instances.Count < 1)
            {
                return new List<Models.Sources.SharePointConfiguration>();
            }

            return instances.Select(x => new Models.Sources.SharePointConfiguration
            {
                ID = x.ID.Id,
                SiteURL = x.Configuration.SiteURL,
                TenantID = x.Configuration.TenantID,
                ClientID = x.Configuration.ClientID,
                ClientSecret = x.Configuration.ClientSecret,
                DocumentLibraryName = x.Configuration.DocumentLibraryName,
            }).ToList();
        }

        internal override bool TryDelete(IEnumerable<Models.Sources.SharePointConfiguration> items)
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
    }
}
