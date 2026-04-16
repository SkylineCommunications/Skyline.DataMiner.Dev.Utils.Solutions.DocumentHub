namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using DomHelpers.SlcDocumenthub;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Utils.DocumentHub.SDM.Models;

    internal class DataHelperSharePointConfiguration : DataHelper<SharePointConfiguration>
    {
        internal DataHelperSharePointConfiguration(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.Sharepoint)
        {
        }

        internal override Guid CreateOrUpdate(SharePointConfiguration item)
        {
            var instance = new SharepointInstance(New(Guid.Parse(item.Identifier)));
            instance.Configuration.SiteURL = item.SiteURL;
            instance.Configuration.TenantID = item.TenantID;
            instance.Configuration.ClientID = item.ClientID;
            instance.Configuration.ClientSecret = item.ClientSecret;
            instance.Configuration.DocumentLibraryName = item.DocumentLibraryName;

            return CreateOrUpdateInstance(instance);
        }

        internal override List<SharePointConfiguration> Read(IEnumerable<DomInstance> domInstances)
        {
            var instances = domInstances.Select(x => new SharepointInstance(x)).ToList();
            if (instances.Count < 1)
            {
                return new List<SharePointConfiguration>();
            }

            return instances.Select(x => new SharePointConfiguration
            {
                Identifier = x.ID.Id.ToString(),
                SiteURL = x.Configuration.SiteURL,
                TenantID = x.Configuration.TenantID,
                ClientID = x.Configuration.ClientID,
                ClientSecret = x.Configuration.ClientSecret,
                DocumentLibraryName = x.Configuration.DocumentLibraryName,
            }).ToList();
        }

        internal override bool TryDelete(IEnumerable<SharePointConfiguration> items)
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
    }
}
