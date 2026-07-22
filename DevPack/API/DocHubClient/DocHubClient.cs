namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using Skyline.DataMiner.Net;

	/// <summary>
	/// Entry point for interacting with the DocumentHub API.
	/// Provides access to document buckets and file operations.
	/// </summary>
	public class DocHubClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocHubClient"/> class.
		/// Initializes a new DocumentHub client using an active DataMiner connection.
		/// </summary>
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
		public DocHubClient(IConnection connection)
		{
			Files = new Files(connection);
		}

		/// <summary>
		/// Gets or sets provides access to document file operations such as upload and read.
		/// </summary>
		public Files Files { get; set; }
	}
}