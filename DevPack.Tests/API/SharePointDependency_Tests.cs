namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Microsoft.Graph;
	using Microsoft.Identity.Client;
	using Microsoft.Kiota.Abstractions;
	using Microsoft.Kiota.Abstractions.Authentication;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Security;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class SharePointDependency_Tests
	{
		private const string TenantId = "aaaaaaaa-0000-0000-0000-000000000001";
		private const string ClientId = "bbbbbbbb-0000-0000-0000-000000000002";
		private OAuthHandler handler = null!;
		private HttpClient httpClient = null!;
		private SharePointAccessTokenProvider provider = null!;

		[TestInitialize]
		public void Setup()
		{
			this.handler = new OAuthHandler();
			this.httpClient = new HttpClient(this.handler);
			var application = ConfidentialClientApplicationBuilder.Create(ClientId)
				.WithAuthority("https://login.microsoftonline.com/" + TenantId, false)
				.WithClientSecret("synthetic-test-secret")
				.WithClientCapabilities(new[] { "cp1" })
				.WithInstanceDiscovery(false)
				.WithHttpClientFactory(new TestHttpClientFactory(this.httpClient))
				.Build();
			this.provider = new SharePointAccessTokenProvider(application);
		}

		[TestCleanup]
		public void Cleanup()
		{
			this.httpClient.Dispose();
		}

		[TestMethod]
		public void DevPack_DoesNotReferenceAzureCredentialAssemblies()
		{
			typeof(SharePointCredentialProvider).Assembly.GetReferencedAssemblies()
				.Should().NotContain(a => a.Name == "Azure.Core" || a.Name == "Azure.Identity");
		}

		[TestMethod]
		public void CandidateSecret_CreatesTaskBasedAuthenticationProvider()
		{
			var authentication = SharePointCredentialProvider.CreateCredential(TenantId, ClientId, "synthetic-test-secret");
			authentication.Should().BeOfType<BaseBearerTokenAuthenticationProvider>();
			((BaseBearerTokenAuthenticationProvider)authentication).AccessTokenProvider.Should().BeOfType<SharePointAccessTokenProvider>();
		}

		[TestMethod]
		public void SavedSecret_CreatesTheSameAuthenticationProvider()
		{
			var store = new InMemorySecretStore();
			store.Save(SharePointSecrets.ClientSecret(TenantId, ClientId), "synthetic-test-secret");
			var authentication = new SharePointCredentialProvider(store).CreateCredential(TenantId, ClientId);
			((BaseBearerTokenAuthenticationProvider)authentication).AccessTokenProvider.Should().BeOfType<SharePointAccessTokenProvider>();
		}

		[TestMethod]
		public async Task Authentication_AcquiresAppTokenAndReusesMsalCache()
		{
			var authentication = new BaseBearerTokenAuthenticationProvider(this.provider);
			var first = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/sites") };
			var second = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/drives") };
			await authentication.AuthenticateRequestAsync(first);
			await authentication.AuthenticateRequestAsync(second);

			first.Headers["Authorization"].Single().Should().Be("Bearer synthetic-test-token");
			second.Headers["Authorization"].Single().Should().Be("Bearer synthetic-test-token");
			this.handler.TokenRequests.Should().Be(1);
			this.handler.LastTokenBody.Should().Contain("grant_type=client_credentials");
			Uri.UnescapeDataString(this.handler.LastTokenBody).Should().Contain("https://graph.microsoft.com/.default");
		}

		[TestMethod]
		public async Task GraphClient_UsesMsalProviderForRequests()
		{
			using (var graph = new GraphServiceClient(this.httpClient, new BaseBearerTokenAuthenticationProvider(this.provider)))
			{
				var first = await graph.Sites["synthetic-site"].GetAsync();
				var second = await graph.Sites["synthetic-site"].GetAsync();
				first!.Id.Should().Be("synthetic-site");
				second!.Id.Should().Be("synthetic-site");
			}

			this.handler.GraphRequests.Should().Be(2);
			this.handler.TokenRequests.Should().Be(1);
		}

		[TestMethod]
		public async Task ClaimsChallenge_AcquiresAnotherTokenWithDecodedClaims()
		{
			var authentication = new BaseBearerTokenAuthenticationProvider(this.provider);
			var request = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/sites") };
			await authentication.AuthenticateRequestAsync(request);
			const string claims = "{\"access_token\":{\"test\":{\"essential\":true}}}";

			await authentication.AuthenticateRequestAsync(request, new Dictionary<string, object>
			{
				{ "claims", Convert.ToBase64String(Encoding.UTF8.GetBytes(claims)) },
			});

			this.handler.TokenRequests.Should().Be(2);
			Uri.UnescapeDataString(this.handler.LastTokenBody).Should().Contain("\"test\":{\"essential\":true}");
			request.Headers["Authorization"].Single().Should().Be("Bearer synthetic-test-token");
		}

		[TestMethod]
		[DataRow("https://example.com/file")]
		[DataRow("https://graph.microsoft.com.example.com/file")]
		[DataRow("https://tenant.sharepoint.com/download")]
		public async Task UntrustedHost_DoesNotAcquireOrAttachToken(string uri)
		{
			var request = new RequestInformation { URI = new Uri(uri) };
			await new BaseBearerTokenAuthenticationProvider(this.provider).AuthenticateRequestAsync(request);

			request.Headers.ContainsKey("Authorization").Should().BeFalse();
			this.handler.Requests.Should().Be(0);
		}

		[TestMethod]
		public async Task InsecureGraphUrl_IsRejectedWithoutAcquiringToken()
		{
			Func<Task> action = () => this.provider.GetAuthorizationTokenAsync(new Uri("http://graph.microsoft.com/v1.0/sites"));
			await action.Should().ThrowAsync<ArgumentException>();
			this.handler.Requests.Should().Be(0);
		}

		[TestMethod]
		public async Task Cancellation_PropagatesToTokenAcquisition()
		{
			using (var cancellation = new CancellationTokenSource())
			{
				cancellation.Cancel();
				Func<Task> action = () => this.provider.GetAuthorizationTokenAsync(
					new Uri("https://graph.microsoft.com/v1.0/sites"), cancellationToken: cancellation.Token);
				await action.Should().ThrowAsync<OperationCanceledException>();
			}
		}

		[TestMethod]
		public async Task InvalidClient_PropagatesAuthenticationFailure()
		{
			this.handler.RejectClient = true;
			Func<Task> action = () => this.provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/sites"));
			await action.Should().ThrowAsync<MsalServiceException>();
		}

		private sealed class TestHttpClientFactory : IMsalHttpClientFactory
		{
			private readonly HttpClient client;

			internal TestHttpClientFactory(HttpClient client)
			{
				this.client = client;
			}

			public HttpClient GetHttpClient()
			{
				return this.client;
			}
		}

		private sealed class OAuthHandler : HttpMessageHandler
		{
			internal int Requests { get; private set; }

			internal int TokenRequests { get; private set; }

			internal int GraphRequests { get; private set; }

			internal string LastTokenBody { get; private set; } = string.Empty;

			internal bool RejectClient { get; set; }

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				this.Requests++;
				if (request.RequestUri!.Host == "graph.microsoft.com")
				{
					request.Headers.Authorization!.Scheme.Should().Be("Bearer");
					request.Headers.Authorization.Parameter.Should().Be("synthetic-test-token");
					this.GraphRequests++;
					return Json(HttpStatusCode.OK, "{\"id\":\"synthetic-site\"}");
				}

				if (request.Method == HttpMethod.Get)
				{
					var metadata = "{\"token_endpoint\":\"https://login.microsoftonline.com/" + TenantId
						+ "/oauth2/v2.0/token\",\"authorization_endpoint\":\"https://login.microsoftonline.com/" + TenantId
						+ "/oauth2/v2.0/authorize\",\"issuer\":\"https://login.microsoftonline.com/" + TenantId + "/v2.0\"}";
					return Json(HttpStatusCode.OK, metadata);
				}

				request.RequestUri!.AbsolutePath.Should().EndWith("/oauth2/v2.0/token");
				this.TokenRequests++;
				this.LastTokenBody = await request.Content!.ReadAsStringAsync();
				return this.RejectClient
					? Json(HttpStatusCode.Unauthorized, "{\"error\":\"invalid_client\",\"error_description\":\"Synthetic rejected credential\"}")
					: Json(HttpStatusCode.OK, "{\"token_type\":\"Bearer\",\"expires_in\":3600,\"access_token\":\"synthetic-test-token\"}");
			}

			private static HttpResponseMessage Json(HttpStatusCode status, string content)
			{
				return new HttpResponseMessage(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
			}
		}
	}
}
