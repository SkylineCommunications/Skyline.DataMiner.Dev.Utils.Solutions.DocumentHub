namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Linq;
	using Azure.Core;
	using Azure.Identity;
	using Microsoft.Graph;
	using Microsoft.Graph.Models.ODataErrors;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	/// <summary>
	/// Checks whether a SharePoint configuration can authenticate and reach its document library.
	/// </summary>
	internal static class SharePointConnectionTester
	{
		/// <summary>
		/// Authenticates with <paramref name="credential"/> and checks that the site and document library of <paramref name="sharePoint"/> are accessible.
		/// </summary>
		/// <param name="sharePoint">The SharePoint configuration to check.</param>
		/// <param name="credential">The credential to authenticate with.</param>
		/// <returns><c>true</c> if authentication succeeds and the document library exists; otherwise, <c>false</c>.</returns>
		internal static bool IsConnected(SharePointConfiguration sharePoint, TokenCredential credential)
		{
			if (sharePoint == null)
			{
				throw new ArgumentNullException(nameof(sharePoint));
			}

			try
			{
				var graphClient = new GraphServiceClient(credential);

				var uri = new UriBuilder("https://" + sharePoint.SiteURL).Uri;
				var site = graphClient.Sites[$"{uri.Host}:{uri.AbsolutePath}"]
					.GetAsync()
					.GetAwaiter()
					.GetResult();

				var drives = graphClient.Sites[site.Id]
					.Drives
					.GetAsync()
					.GetAwaiter()
					.GetResult();

				return drives?.Value != null
					&& drives.Value.Any(d => d.Name != null && d.Name.Equals(sharePoint.DocumentLibraryName, StringComparison.OrdinalIgnoreCase));
			}
			catch (AuthenticationFailedException)
			{
				return false;
			}
			catch (ODataError)
			{
				return false;
			}
			catch (UriFormatException)
			{
				return false;
			}
		}
	}
}
