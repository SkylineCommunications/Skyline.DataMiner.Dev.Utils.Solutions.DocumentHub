namespace Skyline.DataMiner.Utils.DocumentHub.SDM
{
	using System;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    /// <summary>
    /// Provides strongly-typed property accessors (exposers) for <see cref="Models.Sources.DOMSource"/> 
    /// properties that can be used when building <see cref="FilterElement{T}"/> queries.
    /// </summary>
    /// <remarks>
    /// Each exposer represents a single property of <see cref="Models.Sources.DOMSource"/> and can be 
    /// used in combination with filter elements (e.g., <see cref="ORFilterElement{T}"/>, <see cref="ANDFilterElement{T}"/>) 
    /// to create complex queries in a type-safe manner.
    /// </remarks>
    public static class DOMSourceExposers
	{
		/// <summary>
		/// Exposes the <see cref="Models.Sources.DOMSource.ID"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.Sources.DOMSource, Guid> Id =
			new Exposer<Models.Sources.DOMSource, Guid>(
				obj => obj.ID,
				String.Join(".", nameof(DOMSourceExposers), nameof(Id)));

		/// <summary>
		/// Exposes the <see cref="Models.Sources.DOMSource.Name"/> property for filtering or querying.
		/// </summary>
		public static readonly Exposer<Models.Sources.DOMSource, string> Name =
			new Exposer<Models.Sources.DOMSource, string>(
				obj => obj.Name,
				String.Join(".", nameof(DOMSourceExposers), nameof(Name)));
	}

}
