namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Outcome of a secret operation distributed to all agents of the DataMiner System.
	/// </summary>
	public sealed class ClusterSecretResult
	{
		internal ClusterSecretResult(IReadOnlyList<AgentSecretResult> agents)
		{
			Agents = agents;
		}

		/// <summary>
		/// Gets the result per agent.
		/// </summary>
		public IReadOnlyList<AgentSecretResult> Agents { get; }

		/// <summary>
		/// Gets a value indicating whether the operation succeeded on every agent.
		/// </summary>
		public bool Success => Agents.All(a => a.Status == AgentSecretStatus.Succeeded);

		/// <summary>
		/// Gets the agents on which the operation did not succeed.
		/// </summary>
		public IEnumerable<AgentSecretResult> Failed => Agents.Where(a => a.Status != AgentSecretStatus.Succeeded);
	}

	/// <summary>
	/// Outcome of a secret operation on a single agent.
	/// </summary>
	public sealed class AgentSecretResult
	{
		internal AgentSecretResult(int agentId, string agentName, AgentSecretStatus status, string error = null)
		{
			AgentId = agentId;
			AgentName = agentName;
			Status = status;
			Error = error;
		}

		/// <summary>
		/// Gets the DataMiner ID of the agent.
		/// </summary>
		public int AgentId { get; }

		/// <summary>
		/// Gets the name of the agent.
		/// </summary>
		public string AgentName { get; }

		/// <summary>
		/// Gets the status of the operation on the agent.
		/// </summary>
		public AgentSecretStatus Status { get; }

		/// <summary>
		/// Gets the error description, or <c>null</c> on success. Never contains the secret.
		/// </summary>
		public string Error { get; }
	}
}
