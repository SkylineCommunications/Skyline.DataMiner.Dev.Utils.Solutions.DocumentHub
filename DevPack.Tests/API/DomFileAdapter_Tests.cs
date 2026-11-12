namespace DevPack.Tests.API
{
	using System;
	using System.IO;
	using FluentAssertions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;

	[TestClass]
	public class DomFileAdapter_Tests
	{
		private string testFilePath = null!;

		[TestInitialize]
		public void Initialize()
		{
			testFilePath = Path.Combine(Path.GetTempPath(), "dochub_dom_metadata_test_" + Guid.NewGuid().ToString("N") + ".txt");
			File.WriteAllBytes(testFilePath, new byte[2048]);
		}

		[TestCleanup]
		public void Cleanup()
		{
			File.Delete(testFilePath);
		}

		[TestMethod]
		public void GetSize_ShouldMatchLocalFileMetadata()
		{
			var fileInfo = new FileInfo(testFilePath);
			var file = new DomFileAdapter(fileInfo);
			var localFile = new FileInfoAdapter { fileInfo = fileInfo };

			file.GetSize().Should().Be(localFile.GetSize()).And.Be("2 KB");
		}

		[TestMethod]
		public void GetCreatedAt_ShouldMatchLocalFileMetadataInUtc()
		{
			var fileInfo = new FileInfo(testFilePath);
			var file = new DomFileAdapter(fileInfo);
			var localFile = new FileInfoAdapter { fileInfo = fileInfo };

			file.GetCreatedAt().Should().Be(localFile.GetCreatedAt());
			file.GetCreatedAt().Kind.Should().Be(DateTimeKind.Utc);
		}

		[TestMethod]
		public void GetCreatedBy_ShouldMatchLocalFileOwner()
		{
			var fileInfo = new FileInfo(testFilePath);
			var file = new DomFileAdapter(fileInfo);
			var localFile = new FileInfoAdapter { fileInfo = fileInfo };

			file.GetCreatedBy().Should().Be(localFile.GetCreatedBy()).And.NotBeNullOrEmpty();
		}

		[TestMethod]
		public void OtherGetters_ShouldPreserveDomIdentityAndEmptyPaths()
		{
			var instance = new DomInstance
			{
				ID = new DomInstanceId(Guid.NewGuid()),
			};
			var file = new DomFileAdapter(new FileInfo(testFilePath))
			{
				Filename = "attachment.txt",
				Module = "test-module",
				Instance = instance,
			};

			file.GetFile().Should().Be("attachment.txt");
			file.GetName().Should().Be("attachment");
			file.GetExtension().Should().Be("txt");
			file.GetModule().Should().Be("test-module");
			file.GetInstanceId().Should().Be(instance.ID.Id);
			file.GetInstanceName().Should().Be(instance.Name);
			file.GetDirectory().Should().BeEmpty();
			file.GetFilePath().Should().BeEmpty();
			file.GetWebPath().Should().BeEmpty();
		}

		[TestMethod]
		public void GetSize_WhenFileDoesNotExist_ShouldPropagateFileNotFoundException()
		{
			File.Delete(testFilePath);
			var file = new DomFileAdapter(new FileInfo(testFilePath));

			Action act = () => file.GetSize();

			act.Should().Throw<FileNotFoundException>();
		}

		[TestMethod]
		public void Constructor_WithNullFileInfo_ShouldThrowArgumentNullException()
		{
			Action act = () => new DomFileAdapter(null);

			act.Should().Throw<ArgumentNullException>().WithParameterName("fileInfo");
		}
	}
}
