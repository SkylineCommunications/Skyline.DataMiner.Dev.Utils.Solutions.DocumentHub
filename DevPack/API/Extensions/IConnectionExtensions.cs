namespace Skyline.DataMiner.Solutions.DocumentHub.API.Extensions
{
	using System;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

	/// <summary>
	/// Provides extension methods for the <see cref="IConnection"/> interface to work with DocumentHub API client.
	/// </summary>
	public static class IConnectionExtensions
	{
		/// <summary>
		/// Creates a new <see cref="DocHubClient"/> instance for the specified <see cref="IConnection"/>.
		/// </summary>
		/// <param name="connection">The connection for which the DocumentHub API client should be created.</param>
		/// <returns>An <see cref="DocHubClient"/> instance associated with the specified connection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is <c>null</c>.</exception>
		public static DocHubClient GetDocHubClient(this IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return new DocHubClient(connection);
		}
	}
}