using DomHelpers.SlcDocumenthub;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
    internal class DataHelperCategory : DataHelper<Models.DocumentCategory>
    {
        internal DataHelperCategory(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.DocumentCategory)
        {
        }

        internal override Guid CreateOrUpdate(Models.DocumentCategory item)
        {
            var instance = new DocumentCategoryInstance(New(item.ID));
            instance.Info.Name = item.Name;
            instance.Info.Description = item.Description;
            instance.Info.Uploadpath = item.UploadPath;
            instance.Info.Storagetype = item.StorageType;
            instance.Info.Extensions = item.Extensions;
            instance.Info.Isdefault = item.IsDefault;

            return CreateOrUpdateInstance(instance);
        }

        internal override bool TryDelete(IEnumerable<Models.DocumentCategory> items)
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

        internal override List<Models.DocumentCategory> Read(IEnumerable<DomInstance> domInstances)
        {
            var instances = domInstances.Select(x => new DocumentCategoryInstance(x)).ToList();
            if (instances.Count < 1)
            {
                return new List<Models.DocumentCategory>();
            }

            return instances.Select(x => new Models.DocumentCategory
            {
                ID = x.ID.Id,
                Name = x.Info.Name,
                Description = x.Info.Description,
                UploadPath = x.Info.Uploadpath,
                StorageType = x.Info.Storagetype.Value,
                Extensions = x.Info.Extensions,
                IsDefault = x.Info.Isdefault.Value
            }).ToList();
        }
    }
}
