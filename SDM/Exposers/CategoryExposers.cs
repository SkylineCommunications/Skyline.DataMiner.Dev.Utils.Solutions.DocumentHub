namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
	using System;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    /// <summary>
    /// Provides strongly-typed property accessors (exposers) for <see cref="Models.DocumentCategory"/> 
    /// properties that can be used when building <see cref="FilterElement{T}"/> queries.
    /// </summary>
    /// <remarks>
    /// Each exposer represents a single property of <see cref="Models.DocumentCategory"/> and can be 
    /// used in combination with filter elements (e.g., <see cref="ORFilterElement{T}"/>, <see cref="ANDFilterElement{T}"/>) 
    /// to create complex queries in a type-safe manner.
    /// </remarks>
    public static class CategoryExposers
	{
		/// <summary>
		/// Exposes the <see cref="Models.DocumentCategory.ID"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.DocumentCategory, Guid> Id =
			new Exposer<Models.DocumentCategory, Guid>(
				obj => obj.ID,
				String.Join(".", nameof(CategoryExposers), nameof(Id)));

		/// <summary>
		/// Exposes the <see cref="Models.DocumentCategory.Name"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.DocumentCategory, string> Name =
			new Exposer<Models.DocumentCategory, string>(
				obj => obj.Name,
				String.Join(".", nameof(Models.DocumentCategory), nameof(Name)));

		/// <summary>
		/// Exposes the <see cref="Models.DocumentCategory.StorageType"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.DocumentCategory, int> StorageType =
			new Exposer<Models.DocumentCategory, int>(
				obj => (int)obj.StorageType,
				String.Join(".", nameof(Models.DocumentCategory), nameof(StorageType)));

	}
}
