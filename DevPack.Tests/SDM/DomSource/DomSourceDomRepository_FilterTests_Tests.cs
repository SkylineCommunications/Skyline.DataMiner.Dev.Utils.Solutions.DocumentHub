namespace DevPack.Tests.SDM.DomSource
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Text.Json.Nodes;
	using FluentAssertions;
	using FluentAssertions.Execution;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class DomSourceDomRepository_FilterTests_Tests
	{
		private IDocumentHubApiHelper helper;

		[TestInitialize]
		public void Initialize()
		{
			helper = Helper.GetHelper();
			CreateAll(helper);
		}

		[TestMethod]
		public void ReadFilter_Name_Equals()
		{
			// Arrange
			var nameToTest = DemoData.DomSources[0].Name;
			var filter = DomSourceExposers.Name.Equal(nameToTest);

			// Act
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
			var moduleToTest = "inventory";
			var filter = DomSourceExposers.Module.Equal(moduleToTest);

			// Act
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
			var credentialToTest = "cred-inventory";
			var filter = DomSourceExposers.Credential.Contains(credentialToTest, StringComparison.OrdinalIgnoreCase);

			// Act
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
			var pathToTest = "server01";
			var filter = DomSourceExposers.NetworkSharePath.Contains(pathToTest, StringComparison.OrdinalIgnoreCase);

			// Act
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
			var filter = new ANDFilterElement<DomSource>(
				DomSourceExposers.Module.Equal("inventory"),
				DomSourceExposers.Credential.Equal("cred-inventory-001"));

			// Act
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
			var ALLFILTER = new TRUEFilterElement<DomSource>();
			var all = helper.DomSources.Read(ALLFILTER);

			foreach (var item in all)
			{
				Debug.WriteLine(JsonConvert.SerializeObject(item));
			}

			var filter = new ORFilterElement<DomSource>(
				DomSourceExposers.Module.Equal("network"),
				DomSourceExposers.Module.Equal("audit"));

			// Act
			var retrieved = helper.DomSources.Read(filter);

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(2);
			}
		}

		[TestMethod]
		public void Count_ReturnsAll()
		{
			// Arrange
			var filter = new TRUEFilterElement<DomSource>();

			// Act
			var retrieved = helper.DomSources.Read(filter).ToArray();

			// Assert
			using (new AssertionScope())
			{
				retrieved.Should().NotBeNull();
				retrieved.Count().Should().Be(5);
			}
		}

		private static void CreateAll(IDocumentHubApiHelper helper)
		{
			foreach (var item in DemoData.DomSources)
			{
				helper.DomSources.Create(item);
			}
		}
	}
}