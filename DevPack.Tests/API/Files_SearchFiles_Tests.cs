namespace DevPack.Tests.API
{
	using System;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class Files_SearchFiles_Tests
	{
		#region Validation Tests

		[TestMethod]
		public void SearchFiles_WithNullBucket_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);

			// Act
			Action act = () => client.Files.SearchFiles(null, "filetype:pdf");

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("bucket");
		}

		[TestMethod]
		public void SearchFiles_WithNullQuery_ShouldThrowArgumentException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket { StorageType = StorageType.SharePoint, UploadPath = "docs" };

			// Act
			Action act = () => client.Files.SearchFiles(bucket, null);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithParameterName("query");
		}

		[TestMethod]
		public void SearchFiles_WithWhitespaceQuery_ShouldThrowArgumentException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket { StorageType = StorageType.SharePoint, UploadPath = "docs" };

			// Act
			Action act = () => client.Files.SearchFiles(bucket, "   ");

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithParameterName("query");
		}

		[TestMethod]
		public void SearchFiles_WithLocalStorage_ShouldThrowNotSupportedException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket { StorageType = StorageType.Local, UploadPath = "docs" };

			// Act
			Action act = () => client.Files.SearchFiles(bucket, "anything");

			// Assert
			act.Should().Throw<NotSupportedException>()
				.WithMessage("*not supported for storage type*");
		}

		[TestMethod]
		public void SearchFiles_WithDomStorage_ShouldThrowNotSupportedException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket { StorageType = StorageType.DOM, UploadPath = "docs" };

			// Act
			Action act = () => client.Files.SearchFiles(bucket, "anything");

			// Assert
			act.Should().Throw<NotSupportedException>()
				.WithMessage("*not supported for storage type*");
		}

		#endregion
	}
}
