namespace DevPack.Tests.SDM.DocumentBucket
{
	using System.Linq;
	using FluentAssertions;
	using FluentAssertions.Execution;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DocumentBucketDomRepository_CRUD_Tests
	{
		[TestMethod]
		public void EmptyDom_Create()
		{
			// Arrange
			var helper = Helper.GetHelper();

			// Act
			CreateAll(helper);

			// Assert
			using (new AssertionScope())
			{
				var all = helper.DocumentBuckets.Read(new TRUEFilterElement<DocumentBucket>());
				all.Count().Should().Be(DemoData.DocumentBuckets.Count);

				foreach (var demo in DemoData.DocumentBuckets)
				{
					var created = all.SingleOrDefault(c => c.Name == demo.Name);
					created.Should().NotBeNull();
					created.Description.Should().Be(demo.Description);
					created.UploadPath.Should().Be(demo.UploadPath);
					created.StorageType.Should().Be(demo.StorageType);
					created.Extensions.Should().Be(demo.Extensions);
					created.IsDefault.Should().Be(demo.IsDefault);
					created.Definition.Should().Be(demo.Definition);
					created.SizeLimit.Should().Be(demo.SizeLimit);
				}
			}
		}

		[TestMethod]
		public void EmptyDom_Update()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToFind = DemoData.DocumentBuckets[0].Name;
			var newDescription = "Updated description";
			var newUploadPath = "docs/updated";
			var newExtensions = "txt,csv";
			var newSizeLimit = 51200;

			// Act
			CreateAll(helper);

			var itemToUpdate = helper.DocumentBuckets.Read(DocumentBucketExposers.Name.Equal(nameToFind)).SingleOrDefault();
			itemToUpdate.Description = newDescription;
			itemToUpdate.UploadPath = newUploadPath;
			itemToUpdate.Extensions = newExtensions;
			itemToUpdate.SizeLimit = newSizeLimit;

			helper.DocumentBuckets.Update(itemToUpdate);

			// Assert
			using (new AssertionScope())
			{
				var updated = helper.DocumentBuckets.Read(DocumentBucketExposers.Name.Equal(nameToFind)).SingleOrDefault();
				updated.Should().NotBeNull();
				updated.Description.Should().Be(newDescription);
				updated.UploadPath.Should().Be(newUploadPath);
				updated.Extensions.Should().Be(newExtensions);
				updated.SizeLimit.Should().Be(newSizeLimit);
			}
		}

		[TestMethod]
		public void CreateOrUpdate_EmptyDom()
		{
			// Arrange
			var helper = Helper.GetHelper();

			helper.DocumentBuckets.CreateOrUpdate(DemoData.DocumentBuckets);

			// Assert
			using (new AssertionScope())
			{
				var all = helper.DocumentBuckets.Read(new TRUEFilterElement<DocumentBucket>());
				all.Count().Should().Be(DemoData.DocumentBuckets.Count);

				foreach (var demo in DemoData.DocumentBuckets)
				{
					var created = all.SingleOrDefault(c => c.Name == demo.Name);
					created.Should().NotBeNull();
					created.Description.Should().Be(demo.Description);
					created.UploadPath.Should().Be(demo.UploadPath);
					created.StorageType.Should().Be(demo.StorageType);
					created.Extensions.Should().Be(demo.Extensions);
					created.IsDefault.Should().Be(demo.IsDefault);
					created.Definition.Should().Be(demo.Definition);
					created.SizeLimit.Should().Be(demo.SizeLimit);
				}
			}
		}

		[TestMethod]
		public void CreateOrUpdate_ExistingBucket()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToFind = DemoData.DocumentBuckets[0].Name;
			var newDescription = "CreateOrUpdate updated description";
			var newUploadPath = "/docs/createorupdate";
			var newExtensions = "txt,xml";
			var newSizeLimit = 99999;

			// Act
			CreateAll(helper);

			var itemToUpdate = helper.DocumentBuckets.Read(DocumentBucketExposers.Name.Equal(nameToFind)).SingleOrDefault();
			itemToUpdate.Description = newDescription;
			itemToUpdate.UploadPath = newUploadPath;
			itemToUpdate.Extensions = newExtensions;
			itemToUpdate.SizeLimit = newSizeLimit;

			helper.DocumentBuckets.CreateOrUpdate([itemToUpdate]);

			// Assert
			using (new AssertionScope())
			{
				var all = helper.DocumentBuckets.Read(new TRUEFilterElement<DocumentBucket>());
				all.Count().Should().Be(DemoData.DocumentBuckets.Count);

				var updated = helper.DocumentBuckets.Read(DocumentBucketExposers.Name.Equal(nameToFind)).SingleOrDefault();
				updated.Should().NotBeNull();
				updated.Description.Should().Be(newDescription);
				updated.UploadPath.Should().Be(newUploadPath);
				updated.Extensions.Should().Be(newExtensions);
				updated.SizeLimit.Should().Be(newSizeLimit);
			}
		}

		[DataTestMethod]
		[DataRow("/ground_plans", "ground_plans")]
		[DataRow("\\ground_plans", "ground_plans")]
		[DataRow("///ground_plans", "ground_plans")]
		[DataRow("ground_plans", "ground_plans")]
		[DataRow("/docs/network", "docs/network")]
		public void Create_SanitizesUploadPath(string input, string expected)
		{
			// Arrange
			var helper = Helper.GetHelper();
			var bucket = new DocumentBucket
			{
				Identifier = System.Guid.NewGuid().ToString(),
				Name = "Path Sanitization Test",
				UploadPath = input,
				StorageType = StorageType.Local,
			};

			// Act
			var created = helper.DocumentBuckets.Create(bucket);

			// Assert
			created.UploadPath.Should().Be(expected);
		}

		[DataTestMethod]
		[DataRow("/updated/path", "updated/path")]
		[DataRow("\\updated\\path", "updated\\path")]
		[DataRow("updated/path", "updated/path")]
		public void Update_SanitizesUploadPath(string input, string expected)
		{
			// Arrange
			var helper = Helper.GetHelper();
			var bucket = new DocumentBucket
			{
				Identifier = System.Guid.NewGuid().ToString(),
				Name = "Path Sanitization Update Test",
				UploadPath = "original/path",
				StorageType = StorageType.Local,
			};

			helper.DocumentBuckets.Create(bucket);

			// Act
			bucket.UploadPath = input;
			var updated = helper.DocumentBuckets.Update(bucket);

			// Assert
			updated.UploadPath.Should().Be(expected);
		}

		[TestMethod]
		public void EmptyDom_ReadPaged()
		{
			// Arrange
			const int pageCount = 4;
			var helper = Helper.GetHelper();

			// Act
			CreateAll(helper);

			var allFilter = new TRUEFilterElement<DocumentBucket>();
			var pagedResult = helper.DocumentBuckets.ReadPaged(allFilter, pageCount);
			var count = helper.DocumentBuckets.Count(allFilter);

			var numberOfPages = (int)Math.Ceiling(count / (double)pageCount);

			// Assert
			using (new AssertionScope())
			{
				pagedResult.Should().NotBeNull();
				pagedResult.Should().HaveCount(numberOfPages);

				// some pages may have less items, but never more than the page count
				pagedResult.Should().AllSatisfy(page => page.Should().HaveCountLessThanOrEqualTo(pageCount));
			}
		}

		[TestMethod]
		public void DeleteSingle()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToDelete = DemoData.DocumentBuckets[0].Name;

			// Act
			CreateAll(helper);

			var filter = DocumentBucketExposers.Name.Equal(nameToDelete);
			var itemToDelete = helper.DocumentBuckets.Read(filter).SingleOrDefault();

			helper.DocumentBuckets.Delete(itemToDelete);

			// Assert
			using (new AssertionScope())
			{
				helper.DocumentBuckets.Count(new TRUEFilterElement<DocumentBucket>()).Should().Be(DemoData.DocumentBuckets.Count - 1);
				helper.DocumentBuckets.Count(DocumentBucketExposers.Name.Equal(nameToDelete)).Should().Be(0);
			}
		}

		[TestMethod]
		public void DeleteBulk()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var extensionToDelete = "pdf";

			// Act
			CreateAll(helper);

			var filter = DocumentBucketExposers.Extensions.Contains(extensionToDelete, System.StringComparison.OrdinalIgnoreCase);
			var itemsToDelete = helper.DocumentBuckets.Read(filter);

			helper.DocumentBuckets.Delete(itemsToDelete);

			// Assert
			using (new AssertionScope())
			{
				helper.DocumentBuckets.Count(new TRUEFilterElement<DocumentBucket>()).Should().Be(DemoData.DocumentBuckets.Count - 4);
				helper.DocumentBuckets.Count(DocumentBucketExposers.Extensions.Contains(extensionToDelete, System.StringComparison.OrdinalIgnoreCase)).Should().Be(0);
			}
		}

		[TestMethod]
		public void Count_ReturnsAll()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new TRUEFilterElement<DocumentBucket>();

			// Act
			CreateAll(helper);

			var count = helper.DocumentBuckets.Count(filter);

			// Assert
			count.Should().Be(5);
		}

		private static void CreateAll(IDocumentHubApiHelper helper)
		{
			helper.DocumentBuckets.Create(DemoData.DocumentBuckets);
		}
	}
}