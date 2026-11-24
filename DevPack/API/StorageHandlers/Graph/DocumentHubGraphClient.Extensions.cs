namespace Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Microsoft.Kiota.Abstractions;
	using Microsoft.Kiota.Abstractions.Authentication;
	using Microsoft.Kiota.Http.HttpClientLibrary;
	using Microsoft.Kiota.Serialization.Json;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Drives.Item.Items.Item;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Drives.Item.Items.Item.Children;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Drives.Item.Items.Item.Content;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Sites.Item;

	/// <summary>Graph path aliases and metadata batching omitted from the general Kiota runtime.</summary>
	internal partial class DocumentHubGraphClient : IDisposable
	{
		private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
		private const int BatchSize = 20;
		private static readonly Uri GraphBaseUri = new Uri(GraphBaseUrl + "/");

		// Adapters borrow the process-wide connection pool; they never own this HttpClient.
		private static readonly Lazy<HttpClient> SharedHttpClient = new Lazy<HttpClient>(() => KiotaClientFactory.Create());

		/// <inheritdoc />
		public void Dispose()
		{
			(this.RequestAdapter as IDisposable)?.Dispose();
		}

		/// <summary>Creates a Graph transport using the existing authentication provider and borrowed HTTP connections.</summary>
		internal static DocumentHubGraphClient Create(IAuthenticationProvider authentication, HttpClient httpClient = null)
		{
			var adapter = new HttpClientRequestAdapter(
				authentication,
				new JsonParseNodeFactory(),
				new JsonSerializationWriterFactory(),
				httpClient ?? SharedHttpClient.Value)
			{
				BaseUrl = GraphBaseUrl,
			};
			return new DocumentHubGraphClient(adapter);
		}

		/// <summary>Resolves a SharePoint URL through Graph's hostname/path alias.</summary>
		internal SiteItemRequestBuilder SiteByPath(Uri siteUri)
		{
			if (siteUri == null)
			{
				throw new ArgumentNullException(nameof(siteUri));
			}

			var path = EncodePath(siteUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
			return new SiteItemRequestBuilder(GraphBaseUrl + "/sites/" + Uri.EscapeDataString(siteUri.Host) + ":/" + path, this.RequestAdapter);
		}

		/// <summary>Builds a metadata request for a path relative to the library root.</summary>
		internal DriveItemItemRequestBuilder ItemByPath(string driveId, string path)
		{
			return new DriveItemItemRequestBuilder(ItemPathUrl(driveId, path), this.RequestAdapter);
		}

		/// <summary>Builds an upload request without escaping directory separators into an item identifier.</summary>
		internal ContentRequestBuilder ContentByPath(string driveId, string path)
		{
			return new ContentRequestBuilder(ItemPathUrl(driveId, path) + "/content", this.RequestAdapter);
		}

		/// <summary>Builds a folder-creation request for a parent addressed by path.</summary>
		internal ChildrenRequestBuilder ChildrenByPath(string driveId, string path)
		{
			return new ChildrenRequestBuilder(ItemPathUrl(driveId, path) + "/children", this.RequestAdapter);
		}

		/// <summary>Resolves metadata URLs in bounded batches, surfacing individual failures to the paging caller.</summary>
		internal async Task ResolveFileUrlsAsync(IList<DriveItem> items, Func<DriveItem, RequestInformation> createRequest)
		{
			for (var offset = 0; offset < items.Count; offset += BatchSize)
			{
				var requested = new Dictionary<string, DriveItem>(StringComparer.OrdinalIgnoreCase);
				var body = new BatchRequestBody { Requests = new List<BatchRequest>() };
				for (var index = offset; index < Math.Min(offset + BatchSize, items.Count); index++)
				{
					var request = createRequest(items[index]);
					if (request.HttpMethod != Method.GET || !GraphBaseUri.IsBaseOf(request.URI))
					{
						throw new InvalidOperationException("Metadata batches require GET requests to the Graph v1.0 endpoint.");
					}

					var id = index.ToString(CultureInfo.InvariantCulture);
					requested.Add(id, items[index]);
					body.Requests.Add(new BatchRequest
					{
						Id = id,
						Method = "GET",
						Url = "/" + GraphBaseUri.MakeRelativeUri(request.URI).OriginalString,
					});
				}

				var response = await this.Batch.PostAsync(body).ConfigureAwait(false);
				if (response?.Responses == null)
				{
					throw new IOException("SharePoint returned no metadata batch responses.");
				}

				var received = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var item in response.Responses)
				{
					if (item?.Id == null || !requested.TryGetValue(item.Id, out var file) || !received.Add(item.Id))
					{
						throw new IOException("SharePoint returned an unexpected or duplicate metadata batch response.");
					}

					if (!item.Status.HasValue || item.Status < 100 || item.Status > 599)
					{
						throw new IOException("SharePoint returned a metadata batch response without a valid status.");
					}

					if (item.Status < 200 || item.Status >= 300)
					{
						throw new ODataError
						{
							ResponseStatusCode = item.Status.Value,
							ResponseHeaders = ReadHeaders(item.Headers),
							Error = item.Body?.Error ?? new ErrorDetail
							{
								Code = "metadataBatchFailed",
								Message = $"SharePoint metadata lookup failed with HTTP {item.Status.Value}.",
							},
						};
					}

					if (string.IsNullOrWhiteSpace(item.Body?.WebUrl))
					{
						throw new IOException($"SharePoint returned no file URL for search result '{file.Name}'.");
					}

					file.WebUrl = item.Body.WebUrl;
				}

				if (received.Count != requested.Count)
				{
					throw new IOException("SharePoint omitted a requested metadata batch response.");
				}
			}
		}

		private static IDictionary<string, IEnumerable<string>> ReadHeaders(BatchResponse_headers headers)
		{
			var result = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
			if (headers != null)
			{
				foreach (var header in headers.AdditionalData)
				{
					if (header.Value is string value)
					{
						result[header.Key] = new[] { value };
					}
				}
			}

			return result;
		}

		private static string ItemPathUrl(string driveId, string path)
		{
			if (string.IsNullOrWhiteSpace(driveId))
			{
				throw new ArgumentException("A document library identifier is required.", nameof(driveId));
			}
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var root = GraphBaseUrl + "/drives/" + Uri.EscapeDataString(driveId) + "/root";
			var encodedPath = EncodePath(path.Trim('/'));
			return encodedPath.Length == 0 ? root : root + ":/" + encodedPath + ":";
		}

		private static string EncodePath(string path)
		{
			return string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
		}
	}
}
