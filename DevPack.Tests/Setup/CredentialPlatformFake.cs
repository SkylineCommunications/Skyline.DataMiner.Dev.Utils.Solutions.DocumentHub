namespace Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	internal sealed class CredentialPlatformFake : ICredentialPlatform
	{
		internal ConcurrentDictionary<Guid, ALibraryCredential> Credentials { get; } = new ConcurrentDictionary<Guid, ALibraryCredential>();

		internal ConcurrentDictionary<Guid, string> Passwords { get; } = new ConcurrentDictionary<Guid, string>();

		internal ConcurrentDictionary<string, Guid> Assignments { get; } = new ConcurrentDictionary<string, Guid>();

		internal ConcurrentQueue<ALibraryCredential> Writes { get; } = new ConcurrentQueue<ALibraryCredential>();

		internal string? FailureAt { get; set; }

		internal Barrier? ReadBarrier { get; set; }

		public IReadOnlyList<ALibraryCredential> Read()
		{
			var result = Credentials.Values.Select(c => c.GetType() == typeof(UserNameAndPasswordCredential)
				? (ALibraryCredential)new UserNameAndPasswordCredential(c.Info.GUID, c.Info.Name, c.Info.Groups.ToList())
				: new PasswordCredential(c.Info.GUID, c.Info.Name, c.Info.Groups.ToList())).ToList();
			if (ReadBarrier != null && !ReadBarrier.SignalAndWait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("Concurrent test did not reach the barrier.");
			return result;
		}

		public List<int> CallerGroups() => new List<int> { 1 };

		public void Write(ALibraryCredential credential)
		{
			Fail("Write");
			Writes.Enqueue(credential);
			Credentials[credential.Info.GUID] = credential;
			if (credential.dirtyProperties.Contains(nameof(PasswordCredential.Password)))
				Passwords[credential.Info.GUID] = ((PasswordCredential)credential).Password;
		}

		public void Assign(SharePointCredentialReference reference)
		{
			Fail("Assign");
			Assignments[reference.Name] = reference.Id;
		}

		internal static SharePointConfiguration Source(string identifier = "cccccccc-0000-0000-0000-000000000003") => new SharePointConfiguration
		{
			Identifier = identifier,
			Name = "Synthetic source",
			TenantID = "aaaaaaaa-0000-0000-0000-000000000001",
			ClientID = "bbbbbbbb-0000-0000-0000-000000000002",
			SiteURL = "contoso.sharepoint.com/sites/test",
			DocumentLibraryName = "Documents",
		};

		private void Fail(string stage)
		{
			if (FailureAt == stage)
				throw new InvalidOperationException("Synthetic failure at " + stage);
		}
	}
}
