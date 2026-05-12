namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class Files_UploadFile_Tests
	{
		private static string _testFilePath;

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			// Create a minimal test file for validation tests
			_testFilePath = Path.Combine(Path.GetTempPath(), "dochub_test.txt");
			File.WriteAllText(_testFilePath, "test");
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			if (File.Exists(_testFilePath))
			{
				File.Delete(_testFilePath);
			}
		}

		#region UploadFile - Null Bucket Validation

		[TestMethod]
		public void UploadFile_Web_WithNullBucket_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);

			// Act
			Action act = () => client.Files.UploadFile(null, _testFilePath);

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("bucket");
		}

		[TestMethod]
		public void UploadFile_DOM_WithNullBucket_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var domInstanceId = Guid.NewGuid();

			// Act
			Action act = () => client.Files.UploadFile(null, _testFilePath, domInstanceId);

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("bucket");
		}

		[TestMethod]
		public void UploadFile_WithQualifier_NullBucket_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);

			// Act - use named parameter to call the qualifier overload
			Action act = () => client.Files.UploadFile(null, _testFilePath, uploadPathQualifier: "subfolder");

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("bucket");
		}

		#endregion

		#region UploadFile - Null FilePath Validation

		[TestMethod]
		public void UploadFile_Web_WithNullFilePath_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket
			{
				Name = "Test Bucket",
				StorageType = StorageType.Local,
				UploadPath = "TestFolder",
			};

			// Act
			Action act = () => client.Files.UploadFile(bucket, null);

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("filePath");
		}

		[TestMethod]
		public void UploadFile_DOM_WithNullFilePath_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket
			{
				Name = "Test Bucket",
				StorageType = StorageType.DOM,
			};
			var domInstanceId = Guid.NewGuid();

			// Act
			Action act = () => client.Files.UploadFile(bucket, null, domInstanceId);

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("filePath");
		}

		[TestMethod]
		public void UploadFile_WithQualifier_NullFilePath_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket
			{
				Name = "Test Bucket",
				StorageType = StorageType.Local,
				UploadPath = "TestFolder",
			};

			// Act - use named parameter to call the qualifier overload
			Action act = () => client.Files.UploadFile(bucket, null, uploadPathQualifier: "subfolder");

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("filePath");
		}

		#endregion

		#region UploadFile - DOM Instance Validation

		[TestMethod]
		public void UploadFile_DOM_WithEmptyDomInstanceId_ShouldThrowArgumentNullException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket
			{
				Name = "Test Bucket",
				StorageType = StorageType.DOM,
			};

			// Act
			Action act = () => client.Files.UploadFile(bucket, _testFilePath, Guid.Empty);

			// Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("domInstanceId");
		}

		#endregion

		#region UploadFile - Storage Type Validation

		[TestMethod]
		public void UploadFile_WithQualifier_DOMStorage_ShouldThrowInvalidOperationException()
		{
			// Arrange
			var connection = ConnectionHelper.CreateConnection();
			var client = new DocHubClient(connection);
			var bucket = new DocumentBucket
			{
				Name = "DOM Bucket",
				StorageType = StorageType.DOM,
			};

			// Act - use named parameter to call the qualifier overload
			Action act = () => client.Files.UploadFile(bucket, _testFilePath, uploadPathQualifier: "subfolder");

			// Assert
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*DOM storage*");
		}

		#endregion
	}
}