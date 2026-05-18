namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using System.Security;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Helpers;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class FileValidator_ValidateAndSanitizeFile_Tests
	{
		private IDocumentHubApiHelper apiHelper;
		private FileValidator sut;

		[TestInitialize]
		public void Setup()
		{
			var connectionHelper = ConnectionHelper.CreateConnection();
			apiHelper = connectionHelper.GetMockedHelper();
			sut = new FileValidator(connectionHelper);
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_NullBucket_ThrowsArgumentNullException()
		{
			// Arrange
			Action act = () => sut.ValidateAndSanitizeFile(null, @"C:\some\file.txt");

			// Act & Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("bucket");
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_NullFilePath_ThrowsArgumentNullException()
		{
			// Arrange
			var bucket = new DocumentBucket();

			Action act = () => sut.ValidateAndSanitizeFile(bucket, null);

			// Act & Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("filePath");
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_WhitespaceFilePath_ThrowsArgumentNullException()
		{
			// Arrange
			var bucket = new DocumentBucket();

			Action act = () => sut.ValidateAndSanitizeFile(bucket, "   ");

			// Act & Assert
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("filePath");
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_FileDoesNotExist_ThrowsFileNotFoundException()
		{
			// Arrange
			var bucket = new DocumentBucket();
			string nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");

			Action act = () => sut.ValidateAndSanitizeFile(bucket, nonExistentPath);

			// Act & Assert
			act.Should().Throw<FileNotFoundException>();
		}

		[DataTestMethod]
		[DataRow("exe")]
		[DataRow("dll")]
		[DataRow("bat")]
		[DataRow("sh")]
		public void ValidateAndSanitizeFile_BlacklistedExtension_ThrowsSecurityException(string extension)
		{
			// Arrange
			var bucket = new DocumentBucket();
			string tempFile = Path.Combine(Path.GetTempPath(), $"testfile_{Guid.NewGuid()}.{extension}");
			File.WriteAllText(tempFile, "test content");

			try
			{
				Action act = () => sut.ValidateAndSanitizeFile(bucket, tempFile);

				// Act & Assert
				act.Should().Throw<SecurityException>()
					.WithMessage($"*{Path.GetFileNameWithoutExtension(tempFile)}*");
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[DataTestMethod]
		[DataRow("EXE")]
		[DataRow("DLL")]
		[DataRow("BAT")]
		[DataRow("SH")]
		public void ValidateAndSanitizeFile_BlacklistedExtensionUpperCase_ThrowsSecurityException(string extension)
		{
			// Arrange
			var bucket = new DocumentBucket();
			string tempFile = Path.Combine(Path.GetTempPath(), $"testfile_{Guid.NewGuid()}.{extension}");
			File.WriteAllText(tempFile, "test content");

			try
			{
				Action act = () => sut.ValidateAndSanitizeFile(bucket, tempFile);

				// Act & Assert
				act.Should().Throw<SecurityException>();
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_ValidFile_ReturnsSanitizedFileName()
		{
			// Arrange
			var bucket = new DocumentBucket
			{
				Identifier = Guid.NewGuid().ToString(),
				Name = "my-bucket",
			};
			apiHelper.DocumentBuckets.Create(bucket);
			string tempFile = Path.Combine(Path.GetTempPath(), $"testfile_{Guid.NewGuid()}.txt");
			File.WriteAllText(tempFile, "test content");

			try
			{
				// Act
				var result = sut.ValidateAndSanitizeFile(bucket, tempFile);

				// Assert
				result.Should().NotBeNull();
				result.Name.Should().Be(Path.GetFileName(tempFile));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_ValidFile_ReturnsSanitizedFilePath()
		{
			// Arrange
			var bucket = new DocumentBucket
			{
				Identifier = Guid.NewGuid().ToString(),
				Name = "my-bucket",
			};
			apiHelper.DocumentBuckets.Create(bucket);
			string tempFile = Path.Combine(Path.GetTempPath(), $"testfile_{Guid.NewGuid()}.txt");
			File.WriteAllText(tempFile, "test content");

			try
			{
				// Act
				var result = sut.ValidateAndSanitizeFile(bucket, tempFile);

				// Assert
				result.Should().NotBeNull();
				result.FilePath.Should().Be(tempFile);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void ValidateAndSanitizeFile_ValidFile_ReturnsBucketUnchanged()
		{
			// Arrange
			var bucket = new DocumentBucket
			{
				Identifier = Guid.NewGuid().ToString(),
				Name = "my-bucket",
			};
			apiHelper.DocumentBuckets.Create(bucket);
			string tempFile = Path.Combine(Path.GetTempPath(), $"testfile_{Guid.NewGuid()}.txt");
			File.WriteAllText(tempFile, "test content");

			try
			{
				// Act
				var result = sut.ValidateAndSanitizeFile(bucket, tempFile);

				// Assert
				result.Should().NotBeNull();
				result.Bucket.Should().Be(bucket);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[DataTestMethod]
		[DataRow("txt")]
		[DataRow("pdf")]
		[DataRow("docx")]
		[DataRow("xlsx")]
		public void ValidateAndSanitizeFile_AllowedExtension_DoesNotThrow(string extension)
		{
			// Arrange
			var bucket = new DocumentBucket
			{
				Identifier = Guid.NewGuid().ToString(),
				Name = "my-bucket",
			};
			apiHelper.DocumentBuckets.Create(bucket);
			string tempFile = Path.Combine(Path.GetTempPath(), $"testfile_{Guid.NewGuid()}.{extension}");
			File.WriteAllText(tempFile, "test content");

			try
			{
				// Act
				Func<UploadData> func = () => sut.ValidateAndSanitizeFile(bucket, tempFile);

				// Assert
				func.Should().NotThrow();
				var result = func();
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}