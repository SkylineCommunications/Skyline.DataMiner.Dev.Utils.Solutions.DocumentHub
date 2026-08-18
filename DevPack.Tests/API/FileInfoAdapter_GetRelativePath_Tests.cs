namespace DevPack.Tests.API
{
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;

	[TestClass]
	public class FileInfoAdapter_GetRelativePath_Tests
	{
		[TestMethod]
		public void GetRelativePath_FileInsideRoot_ReturnsForwardSlashRelativePath()
		{
			// Arrange
			var fullPath = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager\myfile.txt";
			var root = @"C:\Skyline DataMiner\Webpages";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().Be("Public/WebFileManager/myfile.txt");
		}

		[TestMethod]
		public void GetRelativePath_DirectoryInsideRoot_ReturnsForwardSlashRelativePath()
		{
			// Arrange
			var fullPath = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager\Sub";
			var root = @"C:\Skyline DataMiner\Webpages\Public\WebFileManager";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().Be("Sub");
		}

		[TestMethod]
		public void GetRelativePath_UsesForwardSlashesForNestedPath()
		{
			// Arrange
			var fullPath = @"C:\Root\a\b\c\file.txt";
			var root = @"C:\Root";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().Be("a/b/c/file.txt");
			result.Should().NotContain("\\");
		}

		[TestMethod]
		public void GetRelativePath_PathEqualToRoot_ReturnsEmpty()
		{
			// Arrange
			var fullPath = @"C:\Skyline DataMiner\Webpages";
			var root = @"C:\Skyline DataMiner\Webpages";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GetRelativePath_PathEqualToRootWithTrailingSeparator_ReturnsEmpty()
		{
			// Arrange
			var fullPath = @"C:\Skyline DataMiner\Webpages\";
			var root = @"C:\Skyline DataMiner\Webpages";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GetRelativePath_RootIsMerelyPrefix_ReturnsEmpty()
		{
			// Arrange - "C:\Web" must not match "C:\Web2".
			var fullPath = @"C:\Web2\file.txt";
			var root = @"C:\Web";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GetRelativePath_PathOutsideRoot_ReturnsEmpty()
		{
			// Arrange
			var fullPath = @"C:\Other\Location\file.txt";
			var root = @"C:\Skyline DataMiner\Webpages";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GetRelativePath_MatchIsCaseInsensitive()
		{
			// Arrange
			var fullPath = @"C:\SKYLINE DATAMINER\Webpages\Public\file.txt";
			var root = @"C:\skyline dataminer\webpages";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().Be("Public/file.txt");
		}

		[TestMethod]
		public void GetRelativePath_RootWithTrailingSeparator_ReturnsRelativePath()
		{
			// Arrange
			var fullPath = @"C:\Skyline DataMiner\Webpages\Public\file.txt";
			var root = @"C:\Skyline DataMiner\Webpages\";

			// Act
			var result = FileInfoAdapter.GetRelativePath(fullPath, root);

			// Assert
			result.Should().Be("Public/file.txt");
		}

		[TestMethod]
		public void GetRelativePath_EmptyFullPath_ReturnsEmpty()
		{
			// Act
			var result = FileInfoAdapter.GetRelativePath(string.Empty, @"C:\Skyline DataMiner\Webpages");

			// Assert
			result.Should().BeEmpty();
		}

		[TestMethod]
		public void GetRelativePath_NullFullPath_ReturnsEmpty()
		{
			// Act
			var result = FileInfoAdapter.GetRelativePath(null, @"C:\Skyline DataMiner\Webpages");

			// Assert
			result.Should().BeEmpty();
		}
	}
}