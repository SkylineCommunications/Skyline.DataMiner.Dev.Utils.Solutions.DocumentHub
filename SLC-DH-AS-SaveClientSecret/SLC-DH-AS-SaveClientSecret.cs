/*
***********************************************
*  Copyright (c), Skyline Communications NV.  *
***********************************************
*/

namespace SLC_DH_AS_SaveClientSecret
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

	/// <summary>
	/// Saves or deletes a SharePoint client secret on the agent this script runs on.
	/// Executed on every agent by <see cref="Secrets.SaveSharePointClientSecret"/> and <see cref="Secrets.DeleteSharePointClientSecret"/>.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Provides access to the Automation engine.</param>
		public void Run(IEngine engine)
		{
			try
			{
				new DocHubClient(engine.GetUserConnection()).Secrets.ApplyOnThisAgent(
					engine.GetScriptParam("Action")?.Value,
					engine.GetScriptParam("TenantId")?.Value,
					engine.GetScriptParam("ClientId")?.Value,
					engine.GetScriptParam("Secret")?.Value);
			}
			catch (Exception e)
			{
				// Only the exception message: never include parameter values, they contain the secret.
				engine.ExitFail($"Failed to apply the client secret on this agent: {e.GetType().Name}: {e.Message}");
			}
		}
	}
}
