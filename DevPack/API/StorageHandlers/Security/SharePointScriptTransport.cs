namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	internal static class SharePointScriptTransport
	{
		// Runs the operation in the DocumentHub script that declares the source's credential, on behalf of a caller
		// that cannot declare one itself: a GQI data source, a connector or a script of another solution.
		internal static SharePointScriptResult Execute(IConnection connection, SharePointScriptRequest request)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)));
			const string stage = "The source's credential-consuming script failed.";
			DMSMessage message;
			try
			{
				message = connection.HandleSingleResponseMessage(new ExecuteScriptMessage(Secrets.ScriptName)
				{
					Options = new SA { Sa = new[] { "DEFER:FALSE", "OPTIONS:0", "PARAMETER:2:Execute", "PARAMETER:3:" + encoded } },
				});
			}
			catch (Exception error)
			{
				throw CredentialFailure.At(stage, error);
			}

			if (message is ExceptionMessage failure)
				throw CredentialFailure.At(stage, failure.Exception);
			if (!(message is ExecuteScriptResponseMessage response) || response.HadError
				|| response.ScriptOutput == null || !response.ScriptOutput.TryGetValue("Result", out var output))
			{
				throw CredentialFailure.At(stage);
			}

			SharePointScriptResult result;
			try
			{
				result = SecureNewtonsoftDeserialization.DeserializeObject<SharePointScriptResult>(output);
			}
			catch (JsonException error)
			{
				throw CredentialFailure.At("The credential-consuming script returned an invalid result.", error);
			}

			return result ?? throw CredentialFailure.At("The credential-consuming script returned no result.");
		}
	}

	internal sealed class SharePointScriptRequest
	{
		public string Operation { get; set; }

		public string SourceId { get; set; }

		public string BucketId { get; set; }

		public string Filter { get; set; }

		public string Region { get; set; }

		public int Offset { get; set; }

		public int PageSize { get; set; } = 100;

		public string Name { get; set; }

		public string FileId { get; set; }

		public string LocalPath { get; set; }

		public string UploadPath { get; set; }
	}

	internal sealed class SharePointScriptResult
	{
		public string Value { get; set; }

		public bool HasMore { get; set; }

		public int NextOffset { get; set; }

		public List<SharePointFileInfo> Files { get; set; } = new List<SharePointFileInfo>();
	}
}
