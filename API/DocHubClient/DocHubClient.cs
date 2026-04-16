namespace Skyline.DataMiner.Utils.DocumentHub.API.DocumentHub
{
	using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Utils.DocumentHub.API.DataHelpers;

    /// <summary>
    /// Entry point for interacting with the DocumentHub API.
    /// Provides access to document categories and file operations.
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
			Helpers = new DataHelpersDocumentHub(connection);

			//Categories = new Categories(Helpers);
			Files = new Files(Helpers, connection);
			//Sources = new Sources(Helpers);
		}

		/// <summary>
		/// Gets or sets provides access to document file operations such as upload and read.
		/// </summary>
		public Files Files { get; set; }


		/// <summary>
		/// Gets or sets internal helper layer for DataMiner DOM and storage operations.
		/// </summary>
		internal DataHelpersDocumentHub Helpers { get; set; }
	}
}
