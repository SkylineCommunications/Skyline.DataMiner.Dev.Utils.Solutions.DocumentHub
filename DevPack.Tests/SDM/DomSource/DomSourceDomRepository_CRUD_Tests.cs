namespace DevPack.Tests.SDM.DomSource
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
	public class DomSourceDomRepository_CRUD_Tests
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
				var all = helper.DomSources.Read(new TRUEFilterElement<DomSource>());
				all.Count().Should().Be(DemoData.DomSources.Count);

				foreach (var demo in DemoData.DomSources)
				{
					var created = all.SingleOrDefault(d => d.Name == demo.Name);
					created.Should().NotBeNull();
					created.Module.Should().Be(demo.Module);
					created.NetworkSharePath.Should().Be(demo.NetworkSharePath);
					created.Credential.Should().Be(demo.Credential);
				}
			}
		}

		[TestMethod]
		public void EmptyDom_Update()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToFind = DemoData.DomSources[0].Name;
			var newModule = "updated-module";
			var newNetworkSharePath = @"\\updated-server\share";
			var newCredential = "cred-updated";

			// Act
			CreateAll(helper);

			var itemToUpdate = helper.DomSources.Read(DomSourceExposers.Name.Equal(nameToFind)).SingleOrDefault();
			itemToUpdate.Module = newModule;
			itemToUpdate.NetworkSharePath = newNetworkSharePath;
			itemToUpdate.Credential = newCredential;

			helper.DomSources.Update(itemToUpdate);

			// Assert
			using (new AssertionScope())
			{
				var updated = helper.DomSources.Read(DomSourceExposers.Name.Equal(nameToFind)).SingleOrDefault();
				updated.Should().NotBeNull();
				updated.Module.Should().Be(newModule);
				updated.NetworkSharePath.Should().Be(newNetworkSharePath);
				updated.Credential.Should().Be(newCredential);
			}
		}

		[TestMethod]
		public void EmptyDom_ReadPaged()
		{
			// Arrange
			const int pageCount = 3;
			var helper = Helper.GetHelper();

			// Act
			CreateAll(helper);

			FilterElement<DomSource> allFilter = new TRUEFilterElement<DomSource>();
			var pagedResult = helper.DomSources.ReadPaged(allFilter, pageCount);
			var count = helper.DomSources.Count(allFilter);

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
			var nameToDelete = DemoData.DomSources[0].Name;

			// Act
			CreateAll(helper);

			var filter = DomSourceExposers.Name.Equal(nameToDelete);
			var itemToDelete = helper.DomSources.Read(filter).SingleOrDefault();

			helper.DomSources.Delete(itemToDelete);

			// Assert
			using (new AssertionScope())
			{
				helper.DomSources.Count(new TRUEFilterElement<DomSource>()).Should().Be(DemoData.DomSources.Count - 1);
				helper.DomSources.Count(DomSourceExposers.Name.Equal(nameToDelete)).Should().Be(0);
			}
		}

		[TestMethod]
		public void DeleteBulk()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var moduleToDelete = "inventory";

			// Act
			CreateAll(helper);

			var filter = DomSourceExposers.Module.Equal(moduleToDelete);
			var itemsToDelete = helper.DomSources.Read(filter);

			foreach (var item in itemsToDelete)
			{
				helper.DomSources.Delete(item);
			}

			// Assert
			using (new AssertionScope())
			{
				helper.DomSources.Count(new TRUEFilterElement<DomSource>()).Should().Be(DemoData.DomSources.Count - 2);
				helper.DomSources.Count(DomSourceExposers.Module.Equal(moduleToDelete)).Should().Be(0);
			}
		}

		[TestMethod]
		public void Count_ReturnsAll()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new TRUEFilterElement<DomSource>();

			// Act
			CreateAll(helper);

			var count = helper.DomSources.Count(filter);

			// Assert
			count.Should().Be(5);
		}

		internal static void CreateAll(IDocumentHubApiHelper helper)
		{
			foreach (var item in DemoData.DomSources)
			{
				helper.DomSources.Create(item);
			}
		}
	}
}