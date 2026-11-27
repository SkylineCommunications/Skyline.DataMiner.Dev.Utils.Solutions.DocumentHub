/*
***********************************************
*  Copyright (c), Skyline Communications NV.  *
***********************************************
*/

namespace SLC_DH_AS_ManageSharePointSecret
{
	using System;
	using System.Globalization;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

	/// <summary>
	/// Runs SharePoint operations for callers that cannot declare a credential, and the explicit agent-local migration.
	/// No secret is accepted as a script parameter.
	/// </summary>
	public class Script
	{
		/// <summary>Runs the requested action with the credentials this script declares.</summary>
		/// <param name="engine">The current user's Automation engine.</param>
		public void Run(IEngine engine)
		{
			var failure = "Local credential migration is incomplete.";
			try
			{
				var action = engine.GetScriptParam("Action")?.Value;
				if (action == "Execute")
				{
					// Each source's credential is declared under its library name, which selects it among several.
					failure = "DocumentHub credential operation failed.";
					var result = SharePointScriptHost.Execute(
						engine.GetUserConnection(),
						name => engine.GetCredential(name)?.GetToken(),
						engine.GetScriptParam("Request")?.Value);
					engine.AddScriptOutput("Result", result);
					return;
				}

				if (action == "RestoreAssignments")
				{
					// A package upgrade replaces the DocumentHub scripts and with them their credential declarations.
					failure = "The credential declarations were not restored.";
					var sources = new SharePointSources(engine.GetUserConnection()).RestoreAssignments();
					engine.AddScriptOutput("RestoredSources", sources.ToString(CultureInfo.InvariantCulture));
					return;
				}

				if (action != "MigrateLocal" && action != "MigrateLocalWithGlobal")
					engine.ExitFail("Only Execute, RestoreAssignments and the explicit migration actions are supported. Secret Save and Delete actions have been removed.");

				var count = new DocHubClient(engine.GetUserConnection()).Secrets.MigrateLocalSharePointSecrets(action == "MigrateLocalWithGlobal");
				engine.AddScriptOutput("MigratedLocalFiles", count.ToString(CultureInfo.InvariantCulture));
			}
			catch (ScriptAbortException)
			{
				throw;
			}
			catch (ScriptForceAbortException)
			{
				throw;
			}
			catch (ScriptTimeoutException)
			{
				throw;
			}
			catch (Exception error)
			{
				engine.ExitFail($"{failure} {CredentialFailure.Describe(error)}");
			}
		}
	}
}
