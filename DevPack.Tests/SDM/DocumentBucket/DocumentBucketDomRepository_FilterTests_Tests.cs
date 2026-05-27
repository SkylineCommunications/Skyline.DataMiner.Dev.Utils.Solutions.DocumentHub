namespace DevPack.Tests.SDM.DocumentBucket
{
	using System;
	using System.Linq;
	using DevPack.Tests.SDM.DomSource;
	using DevPack.Tests.SDM.Sharepoint;
	using FluentAssertions;
	using FluentAssertions.Execution;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DocumentBucketDomRepository_FilterTests_Tests
	{
		[TestMethod]
		public void ReadFilter_Name_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToTest = DemoData.DocumentBuckets[0].Name;
			var filter = DocumentBucketExposers.Name.Equal(nameToTest);

			// Act
			CreateAll(helper);

			var expected = DemoData.DocumentBuckets.Single(c => c.Name.Equals(nameToTest));
			var retrieved = helper.DocumentBuckets.Read(filter).SingleOrDefault();

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
			var uploadPathToTest = "docs/";
			var filter = DocumentBucketExposers.UploadPath.Contains(uploadPathToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter);

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
			var filter = DocumentBucketExposers.Description.Contains(descriptionToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter);

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
			var filter = DocumentBucketExposers.Extensions.Contains(extensionToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter);

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
			var filter = DocumentBucketExposers.SizeLimit.Equal(sizeLimitToTest);

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(3);
			}
		}

		[TestMethod]
		public void ReadFilter_SharePointConfigurationReference_Equal()
		{
			// Arrange
			var helper = Helper.GetHelper();
			SharepointDomRepository_FilterTests_Tests.CreateAll(helper);
			CreateAll(helper);

			// Act
			var filter = DocumentBucketExposers.SharePointConfiguration.Equal(new SdmObjectReference<SharePointConfiguration>(DemoData.SharePointConfigurations[1].Identifier));
			var control = DemoData.DocumentBuckets.Where(db => db.SharePointConfiguration != null && db.SharePointConfiguration.Identifier == DemoData.SharePointConfigurations[1].Identifier);
			var retrieved = helper.DocumentBuckets.Read(filter).ToArray();

			// Assert
			control.Should().BeEquivalentTo(retrieved);
		}

		[TestMethod]
		public void ReadFilter_DomSourceReference_Equal()
		{
			// Arrange
			var helper = Helper.GetHelper();
			DomSourceDomRepository_CRUD_Tests.CreateAll(helper);
			CreateAll(helper);

			// Act
			var filter = DocumentBucketExposers.DOMSource.Equal(new SdmObjectReference<DomSource>(DemoData.DomSources[2].Identifier));
			var control = DemoData.DocumentBuckets.Where(db => db.DOMSource != null && db.DOMSource.Identifier == DemoData.DomSources[2].Identifier);
			var retrieved = helper.DocumentBuckets.Read(filter).ToArray();

			// Assert
			control.Should().BeEquivalentTo(retrieved);
		}

		[TestMethod]
		public void ReadFilter_AND_Filter()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new ANDFilterElement<DocumentBucket>(
				DocumentBucketExposers.Extensions.Contains("pdf", StringComparison.OrdinalIgnoreCase),
				DocumentBucketExposers.SizeLimit.Equal(10240));

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter);

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
			var filter = new ORFilterElement<DocumentBucket>(
				DocumentBucketExposers.Definition.Equal("ConfigBak"),
				DocumentBucketExposers.Definition.Equal("NetDiag"));

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter);

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
			var filter = new TRUEFilterElement<DocumentBucket>();

			// Act
			CreateAll(helper);

			var retrieved = helper.DocumentBuckets.Read(filter).ToArray();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(5);
			}
		}

		private static void CreateAll(IDocumentHubApiHelper helper)
		{
			foreach (var item in DemoData.DocumentBuckets)
			{
				helper.DocumentBuckets.Create(item);
			}
		}
	}
}