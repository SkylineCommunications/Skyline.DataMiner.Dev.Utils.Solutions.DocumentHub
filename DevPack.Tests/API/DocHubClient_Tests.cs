namespace DevPack.Tests.API
{
    using FluentAssertions;
    using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
    using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

    [TestClass]
    public class DocHubClient_Tests
    {
        [TestMethod]
        public void Constructor_WithValidConnection_ShouldInitializeFilesProperty()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();

            // Act
            var client = new DocHubClient(connection);

            // Assert
            client.Should().NotBeNull();
            client.Files.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithNullConnection_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () => new DocHubClient(null);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Files_Property_ShouldNotBeNull()
        {
            // Arrange
            var connection = ConnectionHelper.CreateConnection();
            var client = new DocHubClient(connection);

            // Act & Assert
            client.Files.Should().NotBeNull();
        }
    }
}