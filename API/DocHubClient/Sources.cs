namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
	using System;
	using System.Collections.Generic;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    /// <summary>
    /// Provides operations for managing document sources.
    /// </summary>
    public class Sources
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="Sources"/> class.
        /// </summary>
        /// <param name="helpers">
        /// The internal helper layer for DataMiner DOM and storage operations.
		/// </param>
        internal Sources(DataHelpersDocumentHub helpers)
		{
			Helpers = helpers;
		}
        
		/// <summary>
        /// Gets or sets internal helper layer for DataMiner DOM and storage operations.
        /// </summary>
        internal DataHelpersDocumentHub Helpers { get; set; }

		/// <summary>
		/// Retrieves all DOM sources.
		/// </summary>
		/// <returns>
		/// A list of DOM sources.
		/// </returns>
		public List<DomSource> GetDomSources()
		{
			return Helpers.DOMSources.Read();
		}

		/// <summary>
		/// Retrieves DOM sources based on a collection of IDs.
		/// </summary>
		/// <param name="ids">
		/// A collection of <see cref="Guid"/> values representing the IDs of the DOM sources to retrieve.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{T}"/> of <see cref="DomSource"/> containing
		/// all DOM sources that match the provided IDs. The returned collection may be empty if no matches are found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="ids"/> is null.
		/// </exception>
		public IEnumerable<DomSource> GetDomSources(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));

			FilterElement<DomSource> filter = new ORFilterElement<DomSource>();
			foreach (var id in ids)
			{
				filter = filter.OR(DomSourceExposers.Identifier.Equal(id.ToString()));
			}

			return Helpers.DOMSources.Read(filter);
		}

		/// <summary>
		/// Retrieves DOM sources based on a custom filter.
		/// </summary>
		/// <param name="filter">
		/// A <see cref="FilterElement{T}"/> used to specify the criteria for retrieving DOM sources.
		/// The filter can be composed using the <see cref="DomSourceExposers"/> class to create conditions
		/// on DOM source properties, such as ID and Name.
		/// </param>
		/// <returns>
		/// A <see cref="List{T}"/> of <see cref="DomSource"/> containing all DOM sources
		/// that match the provided filter criteria. The list may be empty if no matches are found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="filter"/> is null.
		/// </exception>
		public List<DomSource> GetDomSources(FilterElement<DomSource> filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return Helpers.DOMSources.Read(filter);
		}
	}
}