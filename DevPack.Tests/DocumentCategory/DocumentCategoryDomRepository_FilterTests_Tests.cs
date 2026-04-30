namespace Skyline.DataMiner.Solutions.DocumentHub.Tests.DocumentCategory
{
	using System;
	using System.Linq;

	using FluentAssertions;
	using FluentAssertions.Execution;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DocumentCategoryDomRepository_FilterTests_Tests
	{
		[TestMethod]
		public void ReadFilter_Name_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToTest = DemoData.DocumentCategories[0].Name;
			var filter = DocumentCategoryExposers.Name.Equal(nameToTest);

			// Act
			CreateAll(helper);

			var expected = DemoData.DocumentCategories.Single(c => c.Name.Equals(nameToTest));
			var retrieved = helper.DocumentCategories.Read(filter).SingleOrDefault();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Name.Should().Be(expected.Name);
				retrieved.Description.Should().Be(expected.Description);
				retrieved.UploadPath.Should().Be(expected.UploadPath);
				retrieved.StorageType.Should().Be(expected.StorageType);
				retrieved.Extensions.Should().Be(expected.Extensions);
				retrieved.IsDefault.Should().Be(expected.IsDefault);
				retrieved.Definition.Should().Be(expected.Definition);
				retrieved.SizeLimit.Should().Be(expected.SizeLimit);
			}
		}

		[TestMethod]
		public void ReadFilter_UploadPath_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var uploadPathToTest = "/docs/";
			var filter = DocumentCategoryExposers.UploadPath.Contains(uploadPathToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(5);
			}
		}

		[TestMethod]
		public void ReadFilter_Description_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var descriptionToTest = "maintenance";
			var filter = DocumentCategoryExposers.Description.Contains(descriptionToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(1);
			}
		}

		[TestMethod]
		public void ReadFilter_Extensions_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var extensionToTest = "pdf";
			var filter = DocumentCategoryExposers.Extensions.Contains(extensionToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(4);
			}
		}

		[TestMethod]
		public void ReadFilter_SizeLimit_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var sizeLimitToTest = 10240;
			var filter = DocumentCategoryExposers.SizeLimit.Equal(sizeLimitToTest);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(3);
			}
		}

		[TestMethod]
		public void ReadFilter_AND_Filter()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new ANDFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>(
				DocumentCategoryExposers.Extensions.Contains("pdf", StringComparison.OrdinalIgnoreCase),
				DocumentCategoryExposers.SizeLimit.Equal(10240));

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(3);
			}
		}

		[TestMethod]
		public void ReadFilter_OR_Filter()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new ORFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>(
				DocumentCategoryExposers.Definition.Equal("ConfigBak"),
				DocumentCategoryExposers.Definition.Equal("NetDiag"));

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void ReadFilter_TRUEFilter_ReturnsAll()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new TRUEFilterElement<Solutions.DocumentHub.SDM.Models.DocumentCategory>();

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentCategories.Read(filter).ToArray();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(5);
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