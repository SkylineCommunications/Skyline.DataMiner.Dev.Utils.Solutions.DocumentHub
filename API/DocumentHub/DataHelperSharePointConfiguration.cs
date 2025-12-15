using DomHelpers.SlcDocumenthub;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
    internal class DataHelperSharePointConfiguration : DataHelper<Models.SharePointConfiguration>
    {
        internal DataHelperSharePointConfiguration(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.DocumentCategory)
        {
        }

        internal override Guid CreateOrUpdate(Models.SharePointConfiguration item)
        {
            var instance = new SharepointInstance(New(item.ID));
            instance.Configuration.SiteURL = item.SiteURL;
            instance.Configuration.TenantID = item.TenantID;
            instance.Configuration.ClientID = item.ClientID;
            instance.Configuration.ClientSecret = item.ClientSecret;
            instance.Configuration.DocumentLibraryName = item.DocumentLibraryName;

            return CreateOrUpdateInstance(instance);
        }

        internal override List<Models.SharePointConfiguration> Read(IEnumerable<DomInstance> domInstances)
        {
            var instances = domInstances.Select(x => new SharepointInstance(x)).ToList();
            if (instances.Count < 1)
            {
                return new List<Models.SharePointConfiguration>();
            }

            return instances.Select(x => new Models.SharePointConfiguration
            {
                ID = x.ID.Id,
                SiteURL = x.Configuration.SiteURL,
                TenantID = x.Configuration.TenantID,
                ClientID = x.Configuration.ClientID,
                ClientSecret = x.Configuration.ClientSecret,
                DocumentLibraryName = x.Configuration.DocumentLibraryName
            }).ToList();
        }

        internal override bool TryDelete(IEnumerable<Models.SharePointConfiguration> items)
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
