namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using FluentAssertions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient.Exceptions;
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
			apiHelper = connection.GetMockedHelper();
			client = new DocHubClient(connection);
		}

		[TestMethod]
		public void ReadFiles_ByBucket_WithNullBucket_ShouldThrowArgumentNullException()
		{
			// Act
			Action act = () => client.Files.ReadFiles((DocumentBucket)null, context: null, domInstanceIds: null);

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*bucket*");
		}

		#endregion

		#region ReadFiles by DomSource - Validation

		[TestMethod]
		public void ReadFiles_ByDomSource_WithNullSource_ShouldThrowArgumentNullException()
		{
			// Arrange
			var testBucket = DemoData.DocumentBuckets[1];
			apiHelper.DocumentBuckets.Create(testBucket);
			var domInstanceIds = new List<Guid> { Guid.NewGuid() };

			// Act
			Action act = () => client.Files.ReadFiles(testBucket, domInstanceIds);

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*source type DOM*");
		}

		[TestMethod]
		public void ReadFiles_ByDomSource_WithNullModule_ShouldThrowArgumentException()
		{
			// Arrange
			var testDomSource = DemoData.DomSources[1];
			var testBucket = DemoData.DocumentBuckets[2];

			// setting the module to null
			testDomSource.Module = null;

			apiHelper.DomSources.Create(testDomSource);
			apiHelper.DocumentBuckets.Create(testBucket);

			var domInstanceIds = new List<Guid> { Guid.NewGuid() };

			// Act
			Action act = () => client.Files.ReadFiles(testBucket, domInstanceIds);

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*Module*");
		}

		[TestMethod]
		public void ReadFiles_ByDomSource_WithEmptyModule_ShouldThrowArgumentException()
		{
			// Arrange
			var testDomSource = DemoData.DomSources[1];
			var testBucket = DemoData.DocumentBuckets[2];

			// setting the module to empty string
			testDomSource.Module = string.Empty;

			apiHelper.DomSources.Create(testDomSource);
			apiHelper.DocumentBuckets.Create(testBucket);

			var domInstanceIds = new List<Guid> { Guid.NewGuid() };

			// Act
			Action act = () => client.Files.ReadFiles(testBucket, domInstanceIds);

			// Assert
			act.Should().Throw<ValidationException>().WithMessage("*Module*");
		}

		[TestMethod]
		public void ReadFiles_ByDomSource_WithNullDomInstanceIds_ShouldThrowArgumentNullException()
		{
			// Arrange
			var bucket = new DocumentBucket
			{
				Identifier = Guid.NewGuid().ToString(),
				Name = "Test Source",
			};

			// Act
			Action act = () => client.Files.ReadFiles(bucket, null);

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("domInstanceIds");
		}

		#endregion
	}
}