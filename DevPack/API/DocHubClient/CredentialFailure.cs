namespace Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	/// <summary>Formats failures from credential operations without exposing request data.</summary>
	public static class CredentialFailure
	{
		/// <summary>Reports trusted messages and the types and error codes of other nested causes.</summary>
		/// <param name="error">The failure to describe.</param>
		/// <returns>A description that contains no request data.</returns>
		public static string Describe(Exception error)
		{
			var causes = new List<string>();
			for (var current = error; current != null && causes.Count < 8; current = current.InnerException)
			{
				causes.Add(DescribeCause(current));
			}

			return string.Join(" -> ", causes);
		}

		internal static InvalidOperationException At(string stage, Exception cause = null)
		{
			// Do not retain the original exception: transports and SDKs may include a secret in it.
			return new ReportedFailure(cause == null ? stage : $"{stage} {Describe(cause)}");
		}

		private static string DescribeCause(Exception error)
		{
			if (IsSafe(error))
				return error.Message;

			// ShowUI embeds the native error code in the message, not in HResult.
			const string uiPrefix = "Show UI Failed: 0x";
			var text = error.Message;
			if (text.StartsWith(uiPrefix, StringComparison.Ordinal) && text.Length >= uiPrefix.Length + 8
				&& uint.TryParse(text.Substring(uiPrefix.Length, 8), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
				return $"Show UI failed (0x{code:X8}).";

			return $"{error.GetType().Name} (0x{error.HResult:X8})";
		}

		private static bool IsSafe(Exception error)
		{
			if (error is ReportedFailure)
				return true;

			// SLNet's group refusal names only the calling user.
			if (error.Message.StartsWith("User '", StringComparison.Ordinal)
				&& error.Message.EndsWith("' is not a member of any group authorized to edit the credential.", StringComparison.Ordinal))
				return true;

			switch (error.Message)
			{
				case "No permission to add library credentials.":
				case "No permission to edit library credentials.":
				case "Not authorized to add/edit automation scripts":
				case "Not authorized to execute automation scripts":
				case "The DMA is not licensed for Automation":
				case "Max credential limit reached (1000).":
				case "This agent-local operation requires an unrestricted DataMiner administrator.":
				case "Source, organization and client identifiers must be non-empty GUIDs.":
				case "DataMiner did not return the DOM module list.":
					return true;
				default:
					return false;
			}
		}

		private sealed class ReportedFailure : InvalidOperationException
		{
			internal ReportedFailure(string message)
				: base(message)
			{
			}
		}
	}
}
