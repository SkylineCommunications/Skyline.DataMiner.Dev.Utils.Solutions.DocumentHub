namespace DevPack.Tests.API
{
    using System;
    using System.IO;
    using FluentAssertions;
    using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

    [TestClass]
    public class Files_DeleteFile_Tests
    {
        #region Validation Tests

        [TestMethod]
        public void DeleteFile_WithNullBucket_ShouldThrowArgumentNullException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);

            // Act
            Action act = () => client.Files.DeleteFile(null, "file.txt");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("bucket");
        }

        [TestMethod]
        public void DeleteFile_WithNullFileName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var bucket = new DocumentBucket { StorageType = StorageType.Local, UploadPath = "docs" };

            // Act
            Action act = () => client.Files.DeleteFile(bucket, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("fileName");
        }

        [TestMethod]
        public void DeleteFile_WithEmptyFileName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var bucket = new DocumentBucket { StorageType = StorageType.Local, UploadPath = "docs" };

            // Act
            Action act = () => client.Files.DeleteFile(bucket, "   ");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("fileName");
        }

        [TestMethod]
        public void DeleteFile_WithNonLocalStorage_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var bucket = new DocumentBucket { StorageType = StorageType.DOM, UploadPath = "docs" };

            // Act
            Action act = () => client.Files.DeleteFile(bucket, "file.txt");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not supported for storage type*");
        }

        [TestMethod]
        public void DeleteFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var bucket = new DocumentBucket { StorageType = StorageType.Local, UploadPath = "nonexistent_test_dir" };

            // Act
            Action act = () => client.Files.DeleteFile(bucket, "nonexistent_file.txt");

            // Assert
            act.Should().Throw<FileNotFoundException>();
        }

        [TestMethod]
        public void DeleteFile_WithExistingFile_ShouldDeleteSuccessfully()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "dochub_delete_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            var testFile = Path.Combine(testDir, "delete_me.txt");
            File.WriteAllText(testFile, "to be deleted");

            // Use LocalHandler directly with an absolute path in UploadPath.
            // Path.Combine with an absolute second argument returns that absolute path,
            // so ResolveLocalDirectory will resolve to our temp directory.
            var handler = new Skyline.DataMiner.Solutions.DocumentHub.API.LocalHandler();
            var data = new Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs.DeleteData
            {
                Bucket = new DocumentBucket { StorageType = StorageType.Local, UploadPath = testDir },
                Name = "delete_me.txt",
            };

            // Act
            File.Exists(testFile).Should().BeTrue();
            handler.DeleteFile(data);

            // Assert
            File.Exists(testFile).Should().BeFalse();

            // Cleanup
            Directory.Delete(testDir, true);
        }

        #endregion
    }
}
