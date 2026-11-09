namespace Skyline.DataMiner.Solutions.DocumentHub.API.Security
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Identity.Client;
	using Microsoft.Kiota.Abstractions.Authentication;

	/// <summary>
	/// Acquires Graph app-only tokens without the Azure.Core ValueTask contract used by Azure.Identity.
	/// </summary>
	internal sealed class SharePointAccessTokenProvider : IAccessTokenProvider
	{
		private static readonly string[] Scopes = { "https://graph.microsoft.com/.default" };
		private readonly IConfidentialClientApplication application;

		/// <summary>Initializes a new instance of the <see cref="SharePointAccessTokenProvider"/> class.</summary>
		/// <param name="application">The confidential client whose app token cache is reused for this provider.</param>
		internal SharePointAccessTokenProvider(IConfidentialClientApplication application)
		{
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.AllowedHostsValidator = new AllowedHostsValidator(new[] { "graph.microsoft.com" });
		}

		/// <inheritdoc />
		public AllowedHostsValidator AllowedHostsValidator { get; }

		/// <inheritdoc />
		public async Task<string> GetAuthorizationTokenAsync(
			Uri uri,
			Dictionary<string, object> additionalAuthenticationContext = null,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (uri == null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			// Preauthenticated download/upload URLs must never receive the Graph bearer token.
			if (!this.AllowedHostsValidator.IsUrlHostValid(uri))
			{
				return string.Empty;
			}

			if (uri.Scheme != Uri.UriSchemeHttps)
			{
				throw new ArgumentException("Graph authentication requires HTTPS.", nameof(uri));
			}

			var request = this.application.AcquireTokenForClient(Scopes);
			if (additionalAuthenticationContext != null
				&& additionalAuthenticationContext.TryGetValue("claims", out object value)
				&& value is string claims)
			{
				request = request.WithClaims(Encoding.UTF8.GetString(Convert.FromBase64String(claims)));
			}

			var result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			return result.AccessToken;
		}
	}
}
