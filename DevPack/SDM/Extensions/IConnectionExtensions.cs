namespace Skyline.DataMiner.Solutions.DocumentHub.SDM.Extensions
{
	using System;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;

	/// <summary>
	/// Provides extension methods for the <see cref="IConnection"/> interface to work with DocumentHub API helper.
	/// </summary>
	public static class IConnectionExtensions
	{
		/// <summary>
		/// Creates a new <see cref="IDocumentHubApiHelper"/> instance for the specified <see cref="IConnection"/>.
		/// </summary>
		/// <param name="connection">The connection for which the DocumentHub API helper should be created.</param>
		/// <returns>An <see cref="IDocumentHubApiHelper"/> instance associated with the specified connection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is <c>null</c>.</exception>
		public static IDocumentHubApiHelper GetDocumentHubApiHelper(this IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return new DocumentHubApiHelper(connection);
		}
	}
}