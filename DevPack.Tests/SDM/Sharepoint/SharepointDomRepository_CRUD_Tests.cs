namespace DevPack.Tests.SDM.Sharepoint
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
	public class SharepointDomRepository_CRUD_Tests
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
				var all = helper.SharePointConfigurations.Read(new TRUEFilterElement<SharePointConfiguration>());
				all.Count().Should().Be(DemoData.SharePointConfigurations.Count);

				foreach (var demoConfig in DemoData.SharePointConfigurations)
				{
					var created = all.SingleOrDefault(c => c.Identifier == demoConfig.Identifier);
					created.Should().NotBeNull();
					created.Name.Should().Be(demoConfig.Name);
					created.ClientID.Should().Be(demoConfig.ClientID);
					created.TenantID.Should().Be(demoConfig.TenantID);
					created.SiteURL.Should().Be(demoConfig.SiteURL);
					created.DocumentLibraryName.Should().Be(demoConfig.DocumentLibraryName);
				}
			}
		}

		[TestMethod]
		public void EmptyDom_Update()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var tenantIdToFind = DemoData.SharePointConfigurations[0].TenantID;
			var newClientID = "client-updated-999";
			var newName = "Documentation";
			var newSiteURL = "https://updated.sharepoint.com/sites/NewSite";
			var newDocumentLibraryName = "Updated Library";

			// Act
			CreateAll(helper);

			var configToUpdate = helper.SharePointConfigurations.Read(SharePointConfigurationExposers.TenantID.Equal(tenantIdToFind)).SingleOrDefault();
			configToUpdate.ClientID = newClientID;
			configToUpdate.SiteURL = newSiteURL;
			configToUpdate.Name = newName;
			configToUpdate.DocumentLibraryName = newDocumentLibraryName;

			helper.SharePointConfigurations.Update(configToUpdate);

			// Assert
			using (new AssertionScope())
			{
				var updated = helper.SharePointConfigurations.Read(SharePointConfigurationExposers.TenantID.Equal(tenantIdToFind)).SingleOrDefault();
				updated.Should().NotBeNull();
				updated.ClientID.Should().Be(newClientID);
				updated.SiteURL.Should().Be(newSiteURL);
				updated.Name.Should().Be(newName);
				updated.DocumentLibraryName.Should().Be(newDocumentLibraryName);
			}
		}

		[TestMethod]
		public void EmptyDom_ReadPaged()
		{
			// Arrange
			const int pageCount = 2;
			var helper = Helper.GetHelper();

			// Act
			CreateAll(helper);

			FilterElement<SharePointConfiguration> allFilter = new TRUEFilterElement<SharePointConfiguration>();
			var pagedResult = helper.SharePointConfigurations.ReadPaged(allFilter, pageCount);
			var count = helper.SharePointConfigurations.Count(allFilter);

			var numberOfPages = (int)Math.Ceiling(count / (double)pageCount);

			// Assert
			using (new AssertionScope())
			{
				pagedResult.Should().NotBeNull();
				pagedResult.Should().HaveCount(numberOfPages);
				pagedResult.Should().AllSatisfy(page => page.Should().HaveCountLessThanOrEqualTo(pageCount));
			}
		}

		[TestMethod]
		public void DeleteSingle()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var tenantIdToDelete = DemoData.SharePointConfigurations[0].TenantID;

			// Act
			CreateAll(helper);

			var filter = SharePointConfigurationExposers.TenantID.Equal(tenantIdToDelete);
			var configToDelete = helper.SharePointConfigurations.Read(filter).SingleOrDefault();

			helper.SharePointConfigurations.Delete(configToDelete);

			// Assert
			using (new AssertionScope())
			{
				helper.SharePointConfigurations.Count(new TRUEFilterElement<SharePointConfiguration>()).Should().Be(DemoData.SharePointConfigurations.Count - 1);
				helper.SharePointConfigurations.Count(SharePointConfigurationExposers.TenantID.Equal(tenantIdToDelete)).Should().Be(0);
			}
		}

		[TestMethod]
		public void DeleteBulk()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var siteUrlToDelete = "https://contoso.sharepoint.com/sites/Engineering";

			// Act
			CreateAll(helper);

			var filter = SharePointConfigurationExposers.SiteURL.Equal(siteUrlToDelete);
			var configsToDelete = helper.SharePointConfigurations.Read(filter);

			foreach (var config in configsToDelete)
			{
				helper.SharePointConfigurations.Delete(config);
			}

			// Assert
			using (new AssertionScope())
			{
				helper.SharePointConfigurations.Count(new TRUEFilterElement<SharePointConfiguration>()).Should().Be(DemoData.SharePointConfigurations.Count - 3);
				helper.SharePointConfigurations.Count(SharePointConfigurationExposers.SiteURL.Equal(siteUrlToDelete)).Should().Be(0);
			}
		}

		[TestMethod]
		public void Count_ReturnsAll()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new TRUEFilterElement<SharePointConfiguration>();

			// Act
			CreateAll(helper);

			var retrieved = helper.SharePointConfigurations.Read(filter).ToArray();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(5);
			}
		}

		private static void CreateAll(IDocumentHubApiHelper helper)
		{
			foreach (var config in DemoData.SharePointConfigurations)
			{
				helper.SharePointConfigurations.Create(config);
			}
		}
	}
}