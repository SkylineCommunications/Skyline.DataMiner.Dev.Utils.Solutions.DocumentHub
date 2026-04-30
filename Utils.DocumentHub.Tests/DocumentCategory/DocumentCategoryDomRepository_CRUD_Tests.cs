namespace Skyline.DataMiner.Solutions.DocumentHub.Tests.DocumentCategory
{
	using System.Linq;

	using FluentAssertions;
	using FluentAssertions.Execution;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DocumentCategoryDomRepository_CRUD_Tests
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
				var all = helper.DocumentCategories.Read(new TRUEFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>());
				all.Count().Should().Be(DemoData.DocumentCategories.Count);

				foreach (var demo in DemoData.DocumentCategories)
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
			var nameToFind = DemoData.DocumentCategories[0].Name;
			var newDescription = "Updated description";
			var newUploadPath = "/docs/updated";
			var newExtensions = "txt,csv";
			var newSizeLimit = 51200;

			// Act
			CreateAll(helper);

			var itemToUpdate = helper.DocumentCategories.Read(DocumentCategoryExposers.Name.Equal(nameToFind)).SingleOrDefault();
			itemToUpdate.Description = newDescription;
			itemToUpdate.UploadPath = newUploadPath;
			itemToUpdate.Extensions = newExtensions;
			itemToUpdate.SizeLimit = newSizeLimit;

			helper.DocumentCategories.Update(itemToUpdate);

			// Assert
			using (new AssertionScope())
			{
				var updated = helper.DocumentCategories.Read(DocumentCategoryExposers.Name.Equal(nameToFind)).SingleOrDefault();
				updated.Should().NotBeNull();
				updated.Description.Should().Be(newDescription);
				updated.UploadPath.Should().Be(newUploadPath);
				updated.Extensions.Should().Be(newExtensions);
				updated.SizeLimit.Should().Be(newSizeLimit);
			}
		}

		[TestMethod]
		public void EmptyDom_ReadPaged()
		{
			// Arrange
			const int pageCount = 1;
			var helper = Helper.GetHelper();

			// Act
			CreateAll(helper);

			FilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory> allFilter = new TRUEFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>();
			var pagedResult = helper.DocumentCategories.ReadPaged(allFilter, pageCount);
			var count = helper.DocumentCategories.Count(allFilter);

			// Assert
			using (new AssertionScope())
			{
				pagedResult.Should().NotBeNull();
				pagedResult.Should().HaveCount((int)(count / pageCount));
				pagedResult.Should().AllSatisfy(page => page.Should().HaveCount(pageCount));
			}
		}

		[TestMethod]
		public void DeleteSingle()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToDelete = DemoData.DocumentCategories[0].Name;

			// Act
			CreateAll(helper);

			var filter = DocumentCategoryExposers.Name.Equal(nameToDelete);
			var itemToDelete = helper.DocumentCategories.Read(filter).SingleOrDefault();

			helper.DocumentCategories.Delete(itemToDelete);

			// Assert
			using (new AssertionScope())
			{
				helper.DocumentCategories.Count(new TRUEFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>()).Should().Be(DemoData.DocumentCategories.Count - 1);
				helper.DocumentCategories.Count(DocumentCategoryExposers.Name.Equal(nameToDelete)).Should().Be(0);
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

			var filter = DocumentCategoryExposers.Extensions.Contains(extensionToDelete, System.StringComparison.OrdinalIgnoreCase);
			var itemsToDelete = helper.DocumentCategories.Read(filter);

			helper.DocumentCategories.Delete(itemsToDelete);

			// Assert
			using (new AssertionScope())
			{
				helper.DocumentCategories.Count(new TRUEFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>()).Should().Be(DemoData.DocumentCategories.Count - 4);
				helper.DocumentCategories.Count(DocumentCategoryExposers.Extensions.Contains(extensionToDelete, System.StringComparison.OrdinalIgnoreCase)).Should().Be(0);
			}
		}

		private static void CreateAll(Solutions.DocumentHub.SDM.Helpers.IDocumentHubApiHelper helper)
		{
			foreach (var item in DemoData.DocumentCategories)
			{
				helper.DocumentCategories.Create(item);
			}
		}
	}
}