namespace Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers
{
    using DomHelpers.SlcDocumenthub;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class DataHelperCategory : DataHelper<DocumentCategory>
	{
		internal DataHelperCategory(IConnection connection) : base(connection, SlcDocumenthubIds.Definitions.DocumentCategory)
		{
		}

		internal override Guid CreateOrUpdate(DocumentCategory item)
		{
			var instance = new DocumentCategoryInstance(New(item.ID));
			instance.CategoryInfo.Name = item.Name;
			instance.CategoryInfo.Description = item.Description;
			instance.CategoryInfo.Uploadpath = item.UploadPath;
			instance.CategoryInfo.Storagetype = item.StorageType;
			instance.CategoryInfo.Extensions = item.Extensions;
			instance.CategoryInfo.Isdefault = item.IsDefault;
			instance.CategoryInfo.Definition = item.Definition;

			if (item.DOMSource != null)
			{
				var domSourceHelper = new DataHelperDomSource(_connection);
				//instance.CategoryInfo.Domsource = domSourceHelper.CreateOrUpdate(item.DOMSource);
			}

			return CreateOrUpdateInstance(instance);
		}

		internal override bool TryDelete(IEnumerable<DocumentCategory> items)
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

		internal override List<DocumentCategory> Read(IEnumerable<DomInstance> domInstances)
		{
			var instances = domInstances.Select(x => new DocumentCategoryInstance(x)).ToList();
			if (instances.Count < 1)
			{
				return new List<DocumentCategory>();
			}

			List<DomSource> domSources = GetRequiredDomSources(instances);

			return instances.Select(
				x =>
				{
					DomSource domSource = null;
					if (x.CategoryInfo.Domsource != null && x.CategoryInfo.Domsource != Guid.Empty)
					{
						domSource = domSources.Find(o => o.Identifier == x.CategoryInfo.Domsource.ToString());
					}

					return new DocumentCategory
					{
						ID = x.ID.Id,
						Name = x.CategoryInfo.Name,
						Description = x.CategoryInfo.Description,
						UploadPath = x.CategoryInfo.Uploadpath,
						StorageType = x.CategoryInfo.Storagetype.Value,
						Extensions = x.CategoryInfo.Extensions,
						IsDefault = x.CategoryInfo.Isdefault.Value,
						DOMSource = domSource,
						Definition = x.CategoryInfo.Definition,
					};
				})
				.ToList();
		}

		private List<DomSource> GetRequiredDomSources(IEnumerable<DocumentCategoryInstance> instances)
		{
			var guids = instances
				.Select(i => i.CategoryInfo.Domsource)
				.Where(g => g.HasValue)
				.Select(g => g.Value);

			if (!guids.Any())
				return new List<DomSource>();

			var filter = guids
				.Select(g => DomSourceExposers.Identifier.Equal(g.ToString()))
				.Aggregate(
					(FilterElement<DomSource>)null,
					(f, e) => f == null ? e : f.OR(e));

			return new DataHelperDomSource(_connection).Read(filter);
		}

	}
}
