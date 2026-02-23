namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using DomHelpers.SlcDocumenthub;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	internal static class FilterTranslator
	{
		private static readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[CategoryExposers.Id.fieldName] = HandleGuid,
			[CategoryExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcDocumenthubIds.Sections.CategoryInfo.Name), comparer, (string)value),
			[CategoryExposers.StorageType.fieldName] = (comparer, value) => HandleEnum<SlcDocumenthubIds.Enums.Storagetype>(comparer, value),
			[SharePointExposers.Id.fieldName] = HandleGuid,
			[DomSourceExposers.Id.fieldName] = HandleGuid,
			[DomSourceExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcDocumenthubIds.Sections.DOMSourceInfo.Name), comparer, (string)value),
		};

		/// <summary>
		/// Translates a filter element of type <typeparamref name="T"/> into a filter element for <see cref="DomInstance"/>.
		/// </summary>
		/// <typeparam name="T">The type of the filter element to translate. Must be a class.</typeparam>
		/// <param name="filter">The filter element to translate.</param>
		/// <returns>A <see cref="FilterElement{DomInstance}"/> representing the translated filter.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
		/// <exception cref="NotSupportedException">Thrown when the filter type is not supported.</exception>
		internal static FilterElement<DomInstance> TranslateFullFilter<T>(FilterElement<T> filter) where T : class
		{
			if (filter is null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			FilterElement<DomInstance> translated;
			if (filter is ANDFilterElement<T> and)
			{
				translated = new ANDFilterElement<DomInstance>(and.subFilters.Select(TranslateFullFilter).ToArray());
			}
			else if (filter is ORFilterElement<T> or)
			{
				translated = new ORFilterElement<DomInstance>(or.subFilters.Select(TranslateFullFilter).ToArray());
			}
			else if (filter is NOTFilterElement<T> not)
			{
				translated = new NOTFilterElement<DomInstance>(TranslateFullFilter(not));
			}
			else if (filter is TRUEFilterElement<T>)
			{
				translated = new TRUEFilterElement<DomInstance>();
			}
			else if (filter is FALSEFilterElement<T>)
			{
				translated = new FALSEFilterElement<DomInstance>();
			}
			else if (filter is ManagedFilterIdentifier managedFilter)
			{
				translated = TranslateFilter(managedFilter);
			}
			else
			{
				throw new NotSupportedException($"Unsupported filter: {filter}");
			}

			return translated;
		}

		private static FilterElement<DomInstance> TranslateFilter(ManagedFilterIdentifier managedFilter)
		{
			if (managedFilter is null)
			{
				throw new ArgumentNullException(nameof(managedFilter));
			}

			var fieldName = managedFilter.getFieldName().fieldName;
			var comparer = managedFilter.getComparer();
			var value = managedFilter.getValue();
			var translated = CreateFilter(fieldName, comparer, value);
			return translated;
		}

		private static FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			if (!Handlers.ContainsKey(fieldName))
			{
				throw new NotSupportedException(fieldName);
			}

			return Handlers[fieldName].Invoke(comparer, value);
		}

		private static FilterElement<DomInstance> HandleGuid(Comparer comparer, object value)
		{
			return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
		}

		private static FilterElement<DomInstance> HandleEnum<TEnum>(Comparer comparer, object value)
			where TEnum : struct, Enum
		{
			TEnum parsed;

			if (value is string s)
			{
				if (!Enum.TryParse<TEnum>(s, ignoreCase: true, out parsed))
					throw new ArgumentException($"Invalid enum value '{s}' for {typeof(TEnum).Name}");
			}
			else
			{
				parsed = (TEnum)value;
			}

			int intValue = Convert.ToInt32(parsed);

			return FilterElementFactory.Create(
				DomInstanceExposers.FieldValues.DomInstanceField(SlcDocumenthubIds.Sections.CategoryInfo.Storagetype),
				comparer,
				intValue
			);
		}


	}
}
