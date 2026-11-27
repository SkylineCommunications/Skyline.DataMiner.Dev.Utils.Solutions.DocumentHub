namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Microsoft.Kiota.Abstractions;
	using Microsoft.Kiota.Abstractions.Authentication;
	using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
	using Newtonsoft.Json.Linq;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;

	[TestClass]
	public class KiotaClient_Tests
	{
		private const string GraphUrl = "https://graph.microsoft.com/v1.0";
		private const string FirstSearchPage = @"{
			""value"": [{ ""hitsContainers"": [{
				""moreResultsAvailable"": true,
				""hits"": [
					{ ""hitId"": ""file-1"", ""resource"": {
						""@odata.type"": ""#microsoft.graph.driveItem"",
						""id"": ""file-1"", ""name"": ""first.pdf"", ""size"": 9876543210,
						""createdDateTime"": ""2026-01-02T03:04:05Z"",
						""createdBy"": { ""user"": { ""displayName"": ""Document author"" } },
						""file"": { ""mimeType"": ""application/pdf"" },
						""parentReference"": { ""id"": ""folder"", ""driveId"": ""drive"", ""siteId"": ""site"" },
						""sharepointIds"": { ""listId"": ""list"", ""listItemId"": ""1"" }
					}},
					{ ""hitId"": ""file-2"", ""resource"": {
						""@odata.type"": ""#microsoft.graph.driveItem"",
						""name"": ""second.pdf"", ""file"": {},
						""parentReference"": { ""id"": ""folder"", ""driveId"": ""drive"" }
					}}
				]
			}]}]
		}";
		private const string LastSearchPage = @"{
			""value"": [{ ""hitsContainers"": [{
				""moreResultsAvailable"": false,
				""hits"": [{ ""hitId"": ""file-3"", ""resource"": {
					""@odata.type"": ""#microsoft.graph.driveItem"",
					""id"": ""file-3"", ""name"": ""third.pdf"", ""file"": {},
					""parentReference"": { ""id"": ""folder"", ""driveId"": ""drive"" }
				}}]
			}]}]
		}";

		[TestMethod]
		[DataRow("", "/drives/drive/root", "file #%.bin", "/drives/drive/root:/file%20%23%25.bin:")]
		[DataRow("parent #%", "/drives/drive/root:/parent%20%23%25:", "parent #%/file #%.bin", "/drives/drive/root:/parent%20%23%25/file%20%23%25.bin:")]
		public async Task PathOperations_EncodeUrlsAndPreserveRequestBodies(
			string parentPath, string parentUrl, string filePath, string fileUrl)
		{
			var bytes = new byte[] { 0, 255, 128, 13, 10, 42 };
			var methods = new List<HttpMethod>();
			using var http = new HttpClient(new RecordingHandler(async request =>
			{
				methods.Add(request.Method);
				if (request.Method == HttpMethod.Get)
				{
					request.RequestUri!.AbsoluteUri.Should().Be(GraphUrl + fileUrl);
				}
				else if (request.Method == HttpMethod.Put)
				{
					request.RequestUri!.AbsoluteUri.Should().Be(GraphUrl + fileUrl + "/content");
					request.Content!.Headers.ContentType!.MediaType.Should().Be("application/octet-stream");
					(await request.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
				}
				else
				{
					request.Method.Should().Be(HttpMethod.Post);
					request.RequestUri!.AbsoluteUri.Should().Be(GraphUrl + parentUrl + "/children");
					request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
					var body = JObject.Parse(await request.Content.ReadAsStringAsync());
					body.Value<string>("name").Should().Be("new #% folder");
					body["folder"]!.Type.Should().Be(JTokenType.Object);
				}

				var response = Json(@"{""id"":""item"",""webUrl"":""https://tenant.sharepoint.com/file""}");
				if (request.Method == HttpMethod.Post)
				{
					response.StatusCode = HttpStatusCode.Created;
				}

				return response;
			}));
			using var client = DocumentHubGraphClient.Create(new AnonymousAuthenticationProvider(), http);

			(await client.ItemByPath("drive", filePath).GetAsync()).Id.Should().Be("item");
			using var stream = new MemoryStream(bytes);
			(await client.ContentByPath("drive", filePath).PutAsync(stream)).WebUrl
				.Should().Be("https://tenant.sharepoint.com/file");
			await client.ChildrenByPath("drive", parentPath).PostAsync(new DriveItem
			{
				Name = "new #% folder",
				Folder = new Folder(),
			});

			methods.Should().Equal(HttpMethod.Get, HttpMethod.Put, HttpMethod.Post);
		}

		[TestMethod]
		public async Task Search_DeserializesDriveItemsAndResolutionMetadata()
		{
			using var http = new HttpClient(new RecordingHandler(async request =>
			{
				request.Method.Should().Be(HttpMethod.Post);
				request.RequestUri!.AbsoluteUri.Should().Be(GraphUrl + "/search/query");
				var body = JObject.Parse(await request.Content!.ReadAsStringAsync());
				var query = body["requests"]!.Single();
				query["entityTypes"]!.Values<string>().Should().Equal("driveItem");
				query["query"]!.Value<string>("queryString").Should().Be("filetype:pdf");
				query.Value<int>("from").Should().Be(0);
				query.Value<int>("size").Should().Be(2);
				query.Value<string>("region").Should().Be("EMEA");
				return Json(FirstSearchPage);
			}));
			using var client = DocumentHubGraphClient.Create(new AnonymousAuthenticationProvider(), http);

			var result = await client.Search.Query.PostAsync(new SearchRequestBody
			{
				Requests = new List<SearchRequest>
				{
					new SearchRequest
					{
						EntityTypes = new List<EntityType?> { EntityType.DriveItem },
						Query = new SearchQuery { QueryString = "filetype:pdf" },
						From = 0,
						Size = 2,
						Region = "EMEA",
					},
				},
			});

			var container = result.Value.Single().HitsContainers.Single();
			container.MoreResultsAvailable.Should().BeTrue();
			container.Hits.Should().HaveCount(2);
			var file = container.Hits[0].Resource.Should().BeOfType<DriveItem>().Subject;
			file.Id.Should().Be("file-1");
			file.Name.Should().Be("first.pdf");
			file.Size.Should().Be(9876543210L);
			file.CreatedDateTime.Should().Be(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
			file.CreatedBy.User.DisplayName.Should().Be("Document author");
			file.File.Should().NotBeNull();
			file.Folder.Should().BeNull();
			file.ParentReference.Id.Should().Be("folder");
			file.ParentReference.DriveId.Should().Be("drive");
			file.ParentReference.SiteId.Should().Be("site");
			file.SharepointIds.ListId.Should().Be("list");
			file.SharepointIds.ListItemId.Should().Be("1");
			container.Hits[1].HitId.Should().Be("file-2");
			container.Hits[1].Resource.Should().BeOfType<DriveItem>();
		}

		[TestMethod]
		public async Task MetadataBatch_SplitsTwentyOneItemsAndCorrelatesReversedResponses()
		{
			var batchSizes = new List<int>();
			var items = Enumerable.Range(0, 21).Select(i => new DriveItem { Id = "file-" + i }).ToList();
			var urls = items.ToDictionary(
				item => "/drives/drive/items/" + item.Id + "?$select=webUrl",
				item => "https://tenant.sharepoint.com/" + item.Id);
			using var http = new HttpClient(new RecordingHandler(async request =>
			{
				request.Method.Should().Be(HttpMethod.Post);
				request.RequestUri!.AbsoluteUri.Should().Be(GraphUrl + "/$batch");
				var requests = (JArray)JObject.Parse(await request.Content!.ReadAsStringAsync())["requests"]!;
				batchSizes.Add(requests.Count);
				requests.Select(r => r.Value<string>("id")).Should().OnlyHaveUniqueItems();
				foreach (var step in requests)
				{
					step.Value<string>("method").Should().Be("GET");
					urls.Should().ContainKey(Uri.UnescapeDataString(step.Value<string>("url")!));
				}

				return Json(new JObject
				{
					["responses"] = new JArray(requests.Reverse().Select(step => SuccessfulMetadata(
						step.Value<string>("id")!,
						urls[Uri.UnescapeDataString(step.Value<string>("url")!)]))),
				}.ToString());
			}));
			using var client = DocumentHubGraphClient.Create(new AnonymousAuthenticationProvider(), http);

			await client.ResolveFileUrlsAsync(items, item =>
				client.Drives["drive"].Items[item.Id].ToGetRequestInformation(
					config => config.QueryParameters.Select = new[] { "webUrl" }));

			batchSizes.Should().Equal(20, 1);
			items.Select(item => item.WebUrl).Should().Equal(
				Enumerable.Range(0, 21).Select(i => "https://tenant.sharepoint.com/file-" + i));
		}

		[TestMethod]
		[DataRow(403)]
		[DataRow(429)]
		public async Task SearchPaging_RetainsFailedBatchUntilRetryWithoutSkippingFiles(int failureStatus)
		{
			var searchOffsets = new List<int>();
			var batches = new List<string[]>();
			const string select = "?$select=webUrl";
			var urls = new Dictionary<string, string>
			{
				["/sites/site/lists/list/items/1/driveItem" + select] = "https://tenant.sharepoint.com/first.pdf",
				["/drives/drive/items/file-2" + select] = "https://tenant.sharepoint.com/second.pdf",
				["/drives/drive/items/file-3" + select] = "https://tenant.sharepoint.com/third.pdf",
			};
			using var http = new HttpClient(new RecordingHandler(async request =>
			{
				request.Method.Should().Be(HttpMethod.Post);
				var body = JObject.Parse(await request.Content!.ReadAsStringAsync());
				if (request.RequestUri!.AbsolutePath == "/v1.0/search/query")
				{
					var offset = body["requests"]!.Single().Value<int>("from");
					searchOffsets.Add(offset);
					return Json(offset == 0 ? FirstSearchPage : LastSearchPage);
				}

				request.RequestUri.AbsoluteUri.Should().Be(GraphUrl + "/$batch");
				var requests = (JArray)body["requests"]!;
				batches.Add(requests.Select(step => Uri.UnescapeDataString(step.Value<string>("url")!)).ToArray());
				var responses = new JArray(requests.Select(step =>
					SuccessfulMetadata(step.Value<string>("id")!, urls[Uri.UnescapeDataString(step.Value<string>("url")!)])));
				if (batches.Count == 1)
				{
					responses[1] = new JObject
					{
						["id"] = requests[1]!.Value<string>("id"),
						["status"] = failureStatus,
						["headers"] = new JObject { ["Retry-After"] = "1" },
						["body"] = new JObject { ["error"] = new JObject { ["code"] = "lookupFailed", ["message"] = "Metadata unavailable" } },
					};
				}

				return Json(new JObject { ["responses"] = responses }.ToString());
			}));
			using var client = DocumentHubGraphClient.Create(new AnonymousAuthenticationProvider(), http);
			var handler = new SharePointHandler(
				client, new SharePointConfiguration { SiteURL = "tenant.sharepoint.com/sites/docs" },
				new Site { Id = "site" }, new Drive { Id = "drive" });
			var context = new SharePointSearchPageData { PageSize = 2 };
			var args = new WebFileSearchData
			{
				Bucket = new DocumentBucket { StorageType = StorageType.SharePoint, UploadPath = "" },
				Query = "filetype:pdf",
				Region = "EMEA",
				Context = context,
			};

			Func<Task> firstAttempt = () => handler.SearchFilesAsync(args);
			var failure = (await firstAttempt.Should().ThrowAsync<ODataError>()).Which;
			failure.ResponseStatusCode.Should().Be(failureStatus);
			failure.Error.Code.Should().Be("lookupFailed");
			failure.Message.Should().Be("Metadata unavailable");
			failure.ResponseHeaders["Retry-After"].Should().Equal("1");
			context.HasNextPage().Should().BeTrue();

			var firstPage = await handler.SearchFilesAsync(args);
			firstPage.Select(file => file.GetFile()).Should().Equal("first.pdf", "second.pdf");
			searchOffsets.Should().Equal(0);
			batches[1].Should().Equal(batches[0]);
			var lastPage = await handler.SearchFilesAsync(args);
			lastPage.Select(file => file.GetFile()).Should().Equal("third.pdf");
			firstPage.Concat(lastPage).Select(file => file.GetWebPath()).Should().Equal(
				"https://tenant.sharepoint.com/first.pdf",
				"https://tenant.sharepoint.com/second.pdf",
				"https://tenant.sharepoint.com/third.pdf");
			context.HasNextPage().Should().BeFalse();
			(await handler.SearchFilesAsync(args)).Should().BeEmpty();
			searchOffsets.Should().Equal(0, 2);
			batches.Should().HaveCount(3);
		}

		[TestMethod]
		public async Task Download_RedirectDoesNotForwardBearerTokenAndPreservesBytes()
		{
			var bytes = new byte[] { 0, 255, 128, 13, 10, 42 };
			var visited = new List<string>();
			const string downloadUrl = "https://tenant.sharepoint.com/download?token=synthetic";
			var terminal = new RecordingHandler(request =>
			{
				request.Method.Should().Be(HttpMethod.Get);
				visited.Add(request.RequestUri!.AbsoluteUri);
				if (visited.Count == 1)
				{
					request.RequestUri.AbsoluteUri.Should().Be(GraphUrl + "/drives/drive/items/file/content");
					request.Headers.Authorization!.Scheme.Should().Be("Bearer");
					request.Headers.Authorization.Parameter.Should().Be("synthetic-graph-token");
					var redirect = new HttpResponseMessage(HttpStatusCode.Found);
					redirect.Headers.Location = new Uri(downloadUrl);
					return Task.FromResult(redirect);
				}

				request.RequestUri.AbsoluteUri.Should().Be(downloadUrl);
				request.Headers.Authorization.Should().BeNull();
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
			});
			using var http = new HttpClient(new RedirectHandler { InnerHandler = terminal });
			using var client = DocumentHubGraphClient.Create(new SyntheticBearerProvider(), http);

			using var content = await client.Drives["drive"].Items["file"].Content.GetAsync();
			using var destination = new MemoryStream();
			await content.CopyToAsync(destination);

			destination.ToArray().Should().Equal(bytes);
			visited.Should().Equal(GraphUrl + "/drives/drive/items/file/content", downloadUrl);
		}

		private static JObject SuccessfulMetadata(string id, string url)
		{
			return new JObject { ["id"] = id, ["status"] = 200, ["body"] = new JObject { ["webUrl"] = url } };
		}

		private static HttpResponseMessage Json(string body)
		{
			return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
		}

		private sealed class RecordingHandler : HttpMessageHandler
		{
			private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> respond;

			internal RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
			{
				this.respond = respond;
			}

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var response = await this.respond(request);
				response.RequestMessage = request;
				return response;
			}
		}

		private sealed class SyntheticBearerProvider : IAuthenticationProvider
		{
			public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
			{
				request.URI.Host.Should().Be("graph.microsoft.com");
				request.Headers.Add("Authorization", "Bearer synthetic-graph-token");
				return Task.CompletedTask;
			}
		}
	}
}
