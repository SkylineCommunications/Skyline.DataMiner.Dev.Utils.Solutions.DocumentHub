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
		/// The DataMiner connection used for communication with the system.
		/// </summary>
		private readonly IConnection _connection;

		/// <summary>
		/// Initializes a new instance of the <see cref="DocHubClient"/> class.
		/// Initializes a new DocumentHub client using an active DataMiner connection.
		/// </summary>
		/// <param name="connection">
		/// An active DataMiner connection used to communicate with the system.
		/// </param>
		public DocHubClient(IConnection connection)
		{
			_connection = connection;
			Helpers = new DataHelpersDocumentHub(_connection);

			Categories = new Categories(Helpers, _connection);
			Files = new Files(Helpers, _connection);
			Sources = new Sources(Helpers, _connection);
		}

		/// <summary>
		/// Gets or sets provides access to document category management.
		/// </summary>
		public Categories Categories { get; set; }

		/// <summary>
		/// Gets or sets provides access to document file operations such as upload and read.
		/// </summary>
		public Files Files { get; set; }

		/// <summary>
		/// Gets or sets provides access to source management operations.
		/// </summary>
		public Sources Sources { get; set; }

		/// <summary>
		/// Gets or sets internal helper layer for DataMiner DOM and storage operations.
		/// </summary>
		internal DataHelpersDocumentHub Helpers { get; set; }
	}
}
