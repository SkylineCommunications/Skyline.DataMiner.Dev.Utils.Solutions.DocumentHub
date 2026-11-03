namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	/// <summary>
	/// Status of a secret operation on a single agent.
	/// </summary>
	public enum AgentSecretStatus
	{
		/// <summary>
		/// The operation succeeded.
		/// </summary>
		Succeeded,

		/// <summary>
		/// The script reported an error or could not be started.
		/// </summary>
		Failed,

		/// <summary>
		/// The agent was not running; repeat the operation once it is back.
		/// </summary>
		SkippedNotRunning,
	}
}
