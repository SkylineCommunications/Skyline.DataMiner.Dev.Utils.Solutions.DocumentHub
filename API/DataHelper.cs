namespace Skyline.DataMiner.Utils.DocumentHub.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DocumentHub.SDM;

    /// <summary>
    ///     Provides helper methods for reading, creating, updating, and deleting DOM instances of type
    ///     <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of the DOM instance.</typeparam>
    internal abstract class DataHelper<T> where T : class
	{
		protected readonly IConnection _connection;
		protected readonly DomDefinitionId _defId;
		protected readonly DomHelper _domHelper;

		/// <summary>
		///     Initializes a new instance of the <see cref="DataHelper{T}" /> class.
		/// </summary>
		/// <param name="connection">The connection to use for DOM operations.</param>
		/// <param name="defId">The DOM definition identifier.</param>
		protected DataHelper(IConnection connection, DomDefinitionId defId)
		{
			_connection = connection;
			_defId = defId;
			_domHelper = new DomHelper(connection.HandleMessages, defId.ModuleId);
		}

		/// <summary>
		///     Creates a new DOM instance or updates an existing one based on the provided item.
		/// </summary>
		/// <param name="item">The item to create or update.</param>
		/// <returns>The unique identifier of the created or updated DOM instance.</returns>
		internal abstract Guid CreateOrUpdate(T item);

		/// <summary>
		///     Reads all DOM instances of type <typeparamref name="T" />.
		/// </summary>
		/// <returns>A list of DOM instances of type <typeparamref name="T" />.</returns>
		internal virtual List<T> Read()
		{
			var instances = _domHelper.DomInstances.Read(DomInstanceExposers.DomDefinitionId.Equal(_defId.Id));
			return Read(instances);
		}

		/// <summary>
		///     Reads all DOM instances of type <typeparamref name="T" /> for the given filter <see cref="FilterElement{T}" />.
		/// </summary>
		/// <param name="filter">Filter to limit the items returned.</param>
		/// <returns>A list of DOM instances of type <typeparamref name="T" />.</returns>
		internal virtual List<T> Read(FilterElement<T> filter)
		{
			if (filter is null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return new List<T>();
			}

			var domFilter = FilterTranslator.TranslateFullFilter(filter).AND(DomInstanceExposers.DomDefinitionId.Equal(_defId.Id));
			return Read(_domHelper.DomInstances.Read(domFilter));
		}

		/// <summary>
		///     Attempts to delete the specified DOM instance of type <typeparamref name="T" />.
		/// </summary>
		/// <param name="items">The item(s) to delete.</param>
		/// <returns><c>true</c> if the item was successfully deleted; otherwise, <c>false</c>.</returns>
		internal virtual bool TryDelete(params T[] items)
		{
			return TryDelete(items.ToList());
		}

		/// <summary>
		///     Attempts to delete the specified DOM instance of type <typeparamref name="T" />.
		/// </summary>
		/// <param name="items">The item(s) to delete.</param>
		/// <returns><c>true</c> if the item was successfully deleted; otherwise, <c>false</c>.</returns>
		internal abstract bool TryDelete(IEnumerable<T> items);

		internal Guid CreateOrUpdateInstance(DomInstance instance)
		{
			var result = _domHelper.DomInstances.CreateOrUpdate(new List<DomInstance> { instance });
			return result.SuccessfulIds.FirstOrDefault()?.Id
				   ?? throw new InvalidOperationException($"Failed to create the item due to: {JsonConvert.SerializeObject(result.TraceDataPerItem)}");
		}

		internal DomInstance New(Guid id)
		{
			if (id == Guid.Empty)
			{
				id = Guid.NewGuid();
			}

			return new DomInstance { ID = new DomInstanceId(id), DomDefinitionId = _defId };
		}

		internal bool TryDelete(IEnumerable<Guid> ids)
		{
			var idList = ids?.Where(x => x != Guid.Empty).ToList() ?? new List<Guid>();
			if (!idList.Any())
			{
				return true;
			}

			return _domHelper.DomInstances.TryDelete(
				idList.Select(x => new DomInstance { ID = new DomInstanceId(x) }).ToList(),
				out BulkDeleteResult<DomInstanceId> _);
		}

		internal abstract List<T> Read(IEnumerable<DomInstance> domInstances);
	}
}