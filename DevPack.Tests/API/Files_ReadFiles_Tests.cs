namespace DevPack.Tests.API
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
    using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
    using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

    [TestClass]
    public class Files_ReadFiles_Tests
    {
        #region ReadFiles by DocumentCategory - Validation

        [TestMethod]
        public void ReadFiles_ByCategory_WithNullCategory_ShouldThrowArgumentNullException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);

            // Act
            Action act = () => client.Files.ReadFiles((DocumentCategory)null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("category");
        }

        #endregion

        #region ReadFiles by DomSource - Validation

        [TestMethod]
        public void ReadFiles_ByDomSource_WithNullSource_ShouldThrowArgumentNullException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var domInstanceIds = new List<Guid> { Guid.NewGuid() };

            // Act
            Action act = () => client.Files.ReadFiles(null, domInstanceIds);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [TestMethod]
        public void ReadFiles_ByDomSource_WithNullModule_ShouldThrowArgumentException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var source = new DomSource
            {
                Name = "Test Source",
                Module = null,
            };
            var domInstanceIds = new List<Guid> { Guid.NewGuid() };

            // Act
            Action act = () => client.Files.ReadFiles(source, domInstanceIds);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("source")
                .WithMessage("*Module*");
        }

        [TestMethod]
        public void ReadFiles_ByDomSource_WithEmptyModule_ShouldThrowArgumentException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var source = new DomSource
            {
                Name = "Test Source",
                Module = string.Empty,
            };
            var domInstanceIds = new List<Guid> { Guid.NewGuid() };

            // Act
            Action act = () => client.Files.ReadFiles(source, domInstanceIds);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("source")
                .WithMessage("*Module*");
        }

        [TestMethod]
        public void ReadFiles_ByDomSource_WithNullDomInstanceIds_ShouldThrowArgumentNullException()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);
            var source = new DomSource
            {
                Name = "Test Source",
                Module = "test_module",
            };

            // Act
            Action act = () => client.Files.ReadFiles(source, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("domInstanceIds");
        }

        #endregion
    }
}