namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
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
			: this(connection, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DocHubClient"/> class for an Automation script.
		/// </summary>
		/// <param name="connection">An active DataMiner connection used to communicate with the system.</param>
		/// <param name="scriptCredentials">
		/// Returns the secret of a credential the running script declares under the given name, or <c>null</c>.
		/// SharePoint is then read by the script itself; without a declaration, DocumentHub's own script is called.
		/// </param>
		internal DocHubClient(IConnection connection, Func<string, string> scriptCredentials)
		{
			Files = new Files(connection, scriptCredentials);
			Secrets = new Secrets(connection, scriptCredentials);
		}

		/// <summary>
		/// Gets access to write-only secret operations, such as saving the SharePoint client secret.
		/// Saved secrets can never be read back.
		/// </summary>
		public Secrets Secrets { get; }

		/// <summary>
		/// Gets or sets access to document file operations such as upload, download, and read.
		/// </summary>
		public Files Files { get; set; }
	}
}