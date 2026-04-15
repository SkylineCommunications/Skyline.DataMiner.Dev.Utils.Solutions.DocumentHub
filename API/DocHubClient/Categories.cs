namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
	using System;
	using System.Collections.Generic;
    using Skyline.DataMiner.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    /// <summary>
    /// Provides document category management operations.
    /// </summary>
    public class Categories
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="Categories"/> class.
        /// </summary>
        /// <param name="helpers"></param>
        internal Categories(DataHelpersDocumentHub helpers)
		{
			Helpers = helpers;
		}

		internal DataHelpersDocumentHub Helpers { get; set; }

		/// <summary>
		/// Creates a new document category or updates an existing one.
		/// </summary>
		/// <param name="category">
		/// The document category to create or update.
		/// </param>
		/// <returns>
		/// The GUID ID of the created or updated category.
		/// </returns>
		public Guid CreateOrUpdateCategory(DocumentCategory category)
		{
			return Helpers.DocumentCategories.CreateOrUpdate(category);
		}

		/// <summary>
		/// Retrieves all document categories.
		/// </summary>
		/// <returns>
		/// A list of document categories.
		/// </returns>
		public List<DocumentCategory> GetCategories()
		{
			return Helpers.DocumentCategories.Read();
		}

		/// <summary>
		/// Retrieves document categories based on a custom list of identifiers.
		/// </summary>
		/// <param name="ids">
		/// A collection of <see cref="Guid"/> values representing the IDs of the document categories to retrieve.
		/// </param>
		/// <returns>
		/// An <see cref="IEnumerable{T}"/> of <see cref="Models.DocumentCategory"/> containing all categories 
		/// that match the provided identifiers. The sequence may be empty if no matches are found.
		/// </returns>
		/// <remarks>
		/// The filter used internally is constructed using the <see cref="CategoryExposers"/> class, 
		/// allowing the creation of conditions on category properties such as <c>Id</c>.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="ids"/> is null.
		/// </exception>
		public IEnumerable<DocumentCategory> GetCategories(IEnumerable<Guid> ids)
		{
			if (ids == null)
				throw new ArgumentNullException(nameof(ids));

			FilterElement<DocumentCategory> filter = new ORFilterElement<DocumentCategory>();
			foreach (var id in ids)
			{
				filter = filter.OR(DocumentCategoryExposers.Identifier.Equal(id.ToString()));
			}

			return Helpers.DocumentCategories.Read(filter);
		}

		/// <summary>
		/// Retrieves document categories based on a custom filter.
		/// </summary>
		/// <param name="filter">
		/// A <see cref="FilterElement{T}"/> used to query document categories.
		/// Filters can be composed using the <see cref="CategoryExposers"/> class,
		/// which exposes properties such as <c>Id</c> and <c>Name</c>.
		/// </param>
		/// <returns>
		/// A <see cref="List{T}"/> of <see cref="DocumentCategory"/> containing all categories
		/// that match the provided filter criteria. The list may be empty if no matches are found.
		/// </returns>
		/// <remarks>
		/// This method allows you to create complex queries by combining multiple filter elements
		/// using logical operators such as AND and OR, leveraging the exposed category properties.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="filter"/> is null.
		/// </exception>
		public List<DocumentCategory> GetCategories(FilterElement<DocumentCategory> filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return Helpers.DocumentCategories.Read(filter);
		}

		/// <summary>
		/// Attempts to delete the given document categories.
		/// </summary>
		/// <param name="items">
		/// The categories to delete.
		/// </param>
		/// <returns>
		/// True if all categories were deleted successfully, otherwise false.
		/// </returns>
		public bool TryDeleteCategory(IEnumerable<DocumentCategory> items)
		{
			return Helpers.DocumentCategories.TryDelete(items);
		}
	}
}
