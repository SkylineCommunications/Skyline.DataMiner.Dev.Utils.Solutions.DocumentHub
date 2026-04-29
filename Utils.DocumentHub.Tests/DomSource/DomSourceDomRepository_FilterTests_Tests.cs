namespace Skyline.DataMiner.Utils.DocumentHub.Tests.DomSource
{
	using System;
	using System.Linq;

	using FluentAssertions;
	using FluentAssertions.Execution;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM;
	using Skyline.DataMiner.Utils.DocumentHub.Tests.Setup;

	[TestClass]
	public class DomSourceDomRepository_FilterTests_Tests
	{
		[TestMethod]
		public void ReadFilter_Name_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var nameToTest = DemoData.DomSources[0].Name;
			var filter = DomSourceExposers.Name.Equal(nameToTest);

			// Act
			CreateAll(helper);

			var expected = DemoData.DomSources.Single(d => d.Name.Equals(nameToTest));
			var retrieved = helper.DomSources.Read(filter).SingleOrDefault();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Name.Should().Be(expected.Name);
				retrieved.Module.Should().Be(expected.Module);
				retrieved.NetworkSharePath.Should().Be(expected.NetworkSharePath);
				retrieved.Credential.Should().Be(expected.Credential);
			}
		}

		[TestMethod]
		public void ReadFilter_Module_Equals()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var moduleToTest = "inventory";
			var filter = DomSourceExposers.Module.Equal(moduleToTest);

			// Act
			CreateAll(helper);

			var retrieved = helper.DomSources.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void ReadFilter_Credential_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var credentialToTest = "cred-inventory";
			var filter = DomSourceExposers.Credential.Contains(credentialToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DomSources.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void ReadFilter_NetworkSharePath_Contains()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var pathToTest = "server01";
			var filter = DomSourceExposers.NetworkSharePath.Contains(pathToTest, StringComparison.OrdinalIgnoreCase);

			// Act
			CreateAll(helper);

			var retrieved = helper.DomSources.Read(filter);

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
			var filter = new ANDFilterElement<Solutions.DocumentHub.SDM.Models.DomSource>(
				DomSourceExposers.Module.Equal("inventory"),
				DomSourceExposers.Credential.Equal("cred-inventory-001"));

			// Act
			CreateAll(helper);

			var retrieved = helper.DomSources.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void ReadFilter_OR_Filter()
		{
			// Arrange
			var helper = Helper.GetHelper();
			var filter = new ORFilterElement<Solutions.DocumentHub.SDM.Models.DomSource>(
				DomSourceExposers.Module.Equal("network"),
				DomSourceExposers.Module.Equal("audit"));

			// Act
			CreateAll(helper);

			var retrieved = helper.DomSources.Read(filter);

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
			var filter = new TRUEFilterElement<Solutions.DocumentHub.SDM.Models.DomSource>();

			// Act
			CreateAll(helper);

			var retrieved = helper.DomSources.Read(filter).ToArray();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(5);
			}
		}

		private static void CreateAll(Solutions.DocumentHub.SDM.Helpers.IDocumentHubApiHelper helper)
		{
			foreach (var item in DemoData.DomSources)
			{
				helper.DomSources.Create(item);
			}
		}
	}
}