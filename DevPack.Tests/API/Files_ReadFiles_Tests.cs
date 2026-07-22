namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using DevPack.Tests.SDM;
	using FluentAssertions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Exceptions;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Exposers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class Files_ReadFiles_Tests
	{
		#region ReadFiles by DocumentBucket - Validation

		private IConnection connection;
		private IDocumentHubApiHelper apiHelper;
		private DocHubClient client;

		[TestInitialize]
		public void Initialize()
		{
			connection = ConnectionHelper.CreateConnection();
			apiHelper = Helper.GetHelper(connection);
			client = new DocHubClient(connection);
		}

		[TestMethod]
		public void ReadFiles_ByDomSource_WithNullBucket_ShouldThrowValidationException()
		{
			// Act
			Action act = () => client.Files.ReadFiles((DocumentBucket)null, new ReadFilesConfiguration { Context = null, Filter = null, DomInstanceIds = [] });

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*bucket*");
		}

		[TestMethod]
		public void ReadFiles_ByBucket_WithNullBucket_ShouldThrowArgumentNullException()
		{
			// Act
			Action act = () => client.Files.ReadFiles((DocumentBucket)null, new ReadFilesConfiguration { Context = null, Filter = null });

			// Assert
			act.Should().Throw<ArgumentNullException>().WithParameterName("bucket");
		}

		#endregion

		#region ReadFiles by DomSource - Validation

		[TestMethod]
		public void ReadFiles_ByDomSource_WithNullSource_ShouldThrowArgumentNullException()
		{
			// Arrange
			apiHelper.DocumentBuckets.Create(DemoData.DocumentBuckets[4]);
			var domInstanceIds = new List<Guid> { Guid.NewGuid() };

			// Act
			Action act = () => client.Files.ReadFiles(DemoData.DocumentBuckets[4], new ReadFilesConfiguration { DomInstanceIds = domInstanceIds});

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*source type DOM*");
		}

		[TestMethod]
		public void ReadFiles_ByDomSource_WithNullModule_ShouldThrowArgumentException()
		{
			// Arrange
			var testDomSource = DemoData.DomSources[2].Clone(); // deep copy to avoid modifying the original demo data since it's static
			var testBucket = DemoData.DocumentBuckets[1];

			// setting the module to null
			testDomSource.Module = null;

			apiHelper.DomSources.Create(testDomSource);
			apiHelper.DocumentBuckets.Create(testBucket);

			var domInstanceIds = new List<Guid> { Guid.NewGuid() };

			// Act
			Action act = () => client.Files.ReadFiles(testBucket, new ReadFilesConfiguration { DomInstanceIds = domInstanceIds });

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*Module*");

			// Cleanup: Reset the module to its original value for other tests
			apiHelper.DomSources.Update(DemoData.DomSources[2]);
		}

		[TestMethod]
		public void ReadFiles_ByDomSource_WithEmptyModule_ShouldThrowArgumentException()
		{
			// Arrange
			var testDomSource = DemoData.DomSources[2].Clone(); // deep copy to avoid modifying the original demo data since it's static
			var testBucket = DemoData.DocumentBuckets[1];

			var identifier = DemoData.DocumentBuckets[1].SharePointConfiguration.Identifier ?? Guid.Empty.ToString();
			apiHelper.SharePointConfigurations.Read(SharePointConfigurationExposers.Identifier.Equal(identifier));
			// setting the module to empty string
			testDomSource.Module = string.Empty;

			apiHelper.DomSources.Create(testDomSource);
			apiHelper.DocumentBuckets.Create(testBucket);

			var domInstanceIds = new List<Guid> { Guid.NewGuid() };

			// Act
			Action act = () => client.Files.ReadFiles(testBucket, new ReadFilesConfiguration { DomInstanceIds = domInstanceIds });

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*Module*");

			// Cleanup: Reset the module to its original value for other tests
			apiHelper.DomSources.Update(DemoData.DomSources[2]);
		}

		#endregion
	}
}