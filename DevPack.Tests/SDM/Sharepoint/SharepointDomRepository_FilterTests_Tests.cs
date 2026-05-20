namespace DevPack.Tests.SDM.Sharepoint
{
	using System;
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
	public class SharepointDomRepository_FilterTests_Tests
	{
		[TestMethod]
		public void ReadFilter_TenantID_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var tenantIdToTest = DemoData.SharePointConfigurations[0].TenantID;
			var filter = SharePointConfigurationExposers.TenantID.Equal(tenantIdToTest);

			// Act
			CreateAll(helper);

			var expected = DemoData.SharePointConfigurations.Single(c => c.TenantID.Equals(tenantIdToTest));
			var retrieved = helper.SharePointConfigurations.Read(filter).SingleOrDefault();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.TenantID.Should().Be(expected.TenantID);
				retrieved.ClientID.Should().Be(expected.ClientID);
				retrieved.ClientSecret.Should().Be(expected.ClientSecret);
				retrieved.SiteURL.Should().Be(expected.SiteURL);
				retrieved.DocumentLibraryName.Should().Be(expected.DocumentLibraryName);
			}
		}

		[TestMethod]
		public void ReadFilter_ClientID_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var clientIdToTest = "client-aaa-111";
			var filter = SharePointConfigurationExposers.ClientID.Equal(clientIdToTest);

			// Act
			CreateAll(helper);

			var retrieved = helper.SharePointConfigurations.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void ReadFilter_SiteURL_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var siteUrlToTest = "contoso.sharepoint.com";
			var filter = SharePointConfigurationExposers.SiteURL.Contains(siteUrlToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.SharePointConfigurations.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(3);
			}
		}

		[TestMethod]
		public void Count_TRUEFilter_ReturnsAll()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new TRUEFilterElement<SharePointConfiguration>();

			// Act
			CreateAll(helper);

			var count = helper.SharePointConfigurations.Count(filter);

			// Assert
			count.Should().Be(5);
		}

		[TestMethod]
		public void ReadFilter_DocumentLibraryName_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var libraryNameToTest = "Shared Documents";
			var filter = SharePointConfigurationExposers.DocumentLibraryName.Contains(libraryNameToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.SharePointConfigurations.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void ReadFilter_AND_Filter()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new ANDFilterElement<SharePointConfiguration>(
				SharePointConfigurationExposers.SiteURL.Contains("contoso", StringComparison.OrdinalIgnoreCase),
				SharePointConfigurationExposers.DocumentLibraryName.Equal("Shared Documents"));

			// Act
			CreateAll(helper);

			var retrieved = helper.SharePointConfigurations.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(1);
			}
		}

		[TestMethod]
		public void ReadFilter_OR_Filter()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new ORFilterElement<SharePointConfiguration>(
				SharePointConfigurationExposers.DocumentLibraryName.Equal("Proposals"),
				SharePointConfigurationExposers.DocumentLibraryName.Equal("Technical Manuals"));

			// Act
			CreateAll(helper);

			var retrieved = helper.SharePointConfigurations.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		internal static void CreateAll(IDocumentHubApiHelper helper)
		{
			foreach (var config in DemoData.SharePointConfigurations)
			{
				helper.SharePointConfigurations.Create(config);
			}
		}
	}
}