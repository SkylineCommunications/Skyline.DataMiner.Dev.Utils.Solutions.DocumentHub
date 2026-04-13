namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
	using System;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    /// <summary>
    /// Provides strongly-typed property accessors (exposers) for <see cref="Models.Sources.DomSource"/> 
    /// properties that can be used when building <see cref="FilterElement{T}"/> queries.
    /// </summary>
    /// <remarks>
    /// Each exposer represents a single property of <see cref="Models.Sources.DomSource"/> and can be 
    /// used in combination with filter elements (e.g., <see cref="ORFilterElement{T}"/>, <see cref="ANDFilterElement{T}"/>) 
    /// to create complex queries in a type-safe manner.
    /// </remarks>
    public static class DomSourceExposers
	{
		/// <summary>
		/// Exposes the <see cref="Models.Sources.DomSource.ID"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.Sources.DomSource, Guid> Id =
			new Exposer<Models.Sources.DomSource, Guid>(
				obj => obj.ID,
				String.Join(".", nameof(DomSourceExposers), nameof(Id)));

		/// <summary>
		/// Exposes the <see cref="Models.Sources.DomSource.Name"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.Sources.DomSource, string> Name =
			new Exposer<Models.Sources.DomSource, string>(
				obj => obj.Name,
				String.Join(".", nameof(DomSourceExposers), nameof(Name)));
	}

}
