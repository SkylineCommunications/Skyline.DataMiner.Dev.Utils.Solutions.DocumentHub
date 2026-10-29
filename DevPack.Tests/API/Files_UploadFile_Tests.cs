namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;
	using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

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

		[DataTestMethod]
		[DataRow(null, "")]
		[DataRow("", "")]
		[DataRow("/", "")]
		[DataRow("Memories/2026", @"Memories\2026")]
		[DataRow("/Memories/2026", @"Memories\2026")]
		[DataRow(@"Memories\2026", @"Memories\2026")]
		[DataRow(@"Memories\2026/October", @"Memories\2026\October")]
		[DataRow(@"Memories/2026/October", @"Memories\2026\October")]
		public void ResolveLocalDirectory_ShouldProduceSecureWindowsBasePath(string? relativePath, string expectedDirectory)
		{
			var directory = LocalHandler.ResolveLocalDirectory(relativePath!);
			var expected = Path.Combine(LocalHandler.WebFileManagerRoot, expectedDirectory);

			directory.Should().Be(expected);
			SecurePath.ConstructSecurePath(directory, "photo.jpg")
				.ToString().Should().Be(Path.Combine(expected, "photo.jpg"));
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
				.WithParameterName("fileName");
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
				.WithParameterName("fileName");
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
				.WithParameterName("fileName");
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
				Identifier = Guid.NewGuid().ToString(),
				Name = "DOM Bucket",
				StorageType = StorageType.DOM,
			};

			// Create the bucket on the mocked IConnection so that the validation logic can find it
			var apiHelper = new DocumentHubApiHelper(connection);
			apiHelper.DocumentBuckets.Create(bucket);

			// Act - use named parameter to call the qualifier overload
			Action act = () => client.Files.UploadFile(bucket, _testFilePath, uploadPathQualifier: "subfolder");

			// Assert
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*DOM storage*");
		}

		#endregion
	}
}