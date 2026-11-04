/*
***********************************************
*  Copyright (c), Skyline Communications NV.  *
***********************************************
*/

namespace SLC_DH_AS_ManageSharePointSecret
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;

	/// <summary>
	/// Saves or deletes a SharePoint client secret on the agent this script runs on.
	/// Executed on every agent by the DevPack (<c>DocHubClient.Secrets</c>), because secrets are DPAPI-encrypted per machine.
	/// </summary>
	/// <remarks>
	/// The script has no dependency on the DevPack DLL: it compiles the shared secret-store sources directly, so it works
	/// on an agent where the DevPack has not been deployed yet.
	/// </remarks>
	public class Script
	{
		/// <summary>Action value that saves the secret.</summary>
		public const string SaveAction = "Save";

		/// <summary>Action value that deletes the secret.</summary>
		public const string DeleteAction = "Delete";

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Provides access to the Automation engine.</param>
		public void Run(IEngine engine)
		{
			try
			{
				Apply(
					new DpapiFileSecretStore(),
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

		internal static void Apply(ISecretStore store, string action, string tenantId, string clientId, string clientSecret)
		{
			var key = SharePointSecrets.ClientSecret(tenantId, clientId);

			if (String.Equals(action, DeleteAction, StringComparison.OrdinalIgnoreCase))
			{
				store.Delete(key);
			}
			else if (String.IsNullOrEmpty(action) || String.Equals(action, SaveAction, StringComparison.OrdinalIgnoreCase))
			{
				if (String.IsNullOrEmpty(clientSecret))
				{
					throw new ArgumentException("The client secret cannot be null or empty.", nameof(clientSecret));
				}

				store.Save(key, clientSecret);
			}
			else
			{
				throw new ArgumentException($"Unknown action '{action}'. Expected '{SaveAction}' or '{DeleteAction}'.", nameof(action));
			}
		}
	}
}
