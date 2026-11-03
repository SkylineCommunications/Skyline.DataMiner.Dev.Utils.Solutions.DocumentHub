namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

	/// <summary>
	/// Runs a secret operation on every agent of the DataMiner System.
	/// </summary>
	internal interface IAgentSecretDistributor
	{
		ClusterSecretResult Execute(string action, string tenantId, string clientId, string secret);
	}

	/// <summary>
	/// Executes the secret Automation script on every running agent, so each agent encrypts the secret with its own machine key.
	/// </summary>
	internal sealed class AgentSecretDistributor : IAgentSecretDistributor
	{
		private readonly IConnection _connection;

		public AgentSecretDistributor(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public ClusterSecretResult Execute(string action, string tenantId, string clientId, string secret)
		{
			IDms dms = _connection.GetDms();
			if (!dms.ScriptExists(Secrets.ScriptName))
			{
				throw new InvalidOperationException($"Required script '{Secrets.ScriptName}' does not exist on this DataMiner System.");
			}

			var script = dms.GetScript(Secrets.ScriptName);
			var parameters = new[]
			{
				new DmsAutomationScriptParamValue("Action", action),
				new DmsAutomationScriptParamValue("TenantId", tenantId),
				new DmsAutomationScriptParamValue("ClientId", clientId),
				new DmsAutomationScriptParamValue("Secret", secret ?? String.Empty),
			};

			var results = new List<AgentSecretResult>();
			foreach (var agent in dms.GetAgents().OrderBy(a => a.Id))
			{
				if (agent.State != AgentState.Running)
				{
					results.Add(new AgentSecretResult(agent.Id, agent.Name, AgentSecretStatus.SkippedNotRunning, $"Agent state is {agent.State}."));
					continue;
				}

				try
				{
					var result = script.Execute(parameters, Array.Empty<DmsAutomationScriptDummyValue>(), new DmsAutomationScriptRunOptions { AgentId = agent.Id });
					results.Add(result.HadError
						? new AgentSecretResult(agent.Id, agent.Name, AgentSecretStatus.Failed, String.Join("; ", result.ErrorMessages ?? new string[0]))
						: new AgentSecretResult(agent.Id, agent.Name, AgentSecretStatus.Succeeded));
				}
				catch (Exception ex)
				{
					results.Add(new AgentSecretResult(agent.Id, agent.Name, AgentSecretStatus.Failed, ex.GetType().Name + ": " + ex.Message));
				}
			}

			return new ClusterSecretResult(results);
		}
	}
}
