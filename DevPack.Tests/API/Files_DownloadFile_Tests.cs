namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Moq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;
	using DriveItem = Microsoft.Graph.Models.DriveItem;

	[TestClass]
	public class Files_DownloadFile_Tests
	{
		private string testDirectory = null!;

		[TestInitialize]
		public void Initialize()
		{
			testDirectory = Path.Combine(Path.GetTempPath(), "dochub_download_test_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(testDirectory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(testDirectory))
				Directory.Delete(testDirectory, recursive: true);
		}

		[TestMethod]
		public void DownloadFile_WithNullBucket_ShouldThrowArgumentNullException()
		{
			var client = new DocHubClient(ConnectionHelper.CreateConnection());

			Action act = () => client.Files.DownloadFile(null, Mock.Of<IDocHubFile>(), Path.Combine(testDirectory, "file.txt"));

			act.Should().Throw<ArgumentNullException>().WithParameterName("bucket");
		}

		[TestMethod]
		public void DownloadFile_WithNullFile_ShouldThrowArgumentNullException()
		{
			var client = new DocHubClient(ConnectionHelper.CreateConnection());
			var bucket = new DocumentBucket { StorageType = StorageType.Local };

			Action act = () => client.Files.DownloadFile(bucket, null, Path.Combine(testDirectory, "file.txt"));

			act.Should().Throw<ArgumentNullException>().WithParameterName("file");
		}

		[TestMethod]
		public void DownloadFile_ShouldDispatchToBucketStorageHandler()
		{
			var connection = ConnectionHelper.CreateConnection();
			var handler = new RecordingStorageHandler();
			var bucket = new DocumentBucket { StorageType = StorageType.DOM };
			var file = Mock.Of<IDocHubFile>();
			var destinationPath = Path.Combine(testDirectory, "file.txt");
			DocumentBucket? dispatchedBucket = null;
			var files = new Files(
				connection,
				(actualConnection, actualBucket) =>
				{
					actualConnection.Should().BeSameAs(connection);
					dispatchedBucket = actualBucket;
					return handler;
				});

			files.DownloadFile(bucket, file, destinationPath);

			dispatchedBucket.Should().BeSameAs(bucket);
			handler.File.Should().BeSameAs(file);
			handler.DestinationPath.Should().Be(destinationPath);
		}

		[TestMethod]
		public void LocalHandler_DownloadFile_ShouldCopyFile()
		{
			var sourcePath = Path.Combine(testDirectory, "source.txt");
			var destinationPath = Path.Combine(testDirectory, "destination.txt");
			File.WriteAllText(sourcePath, "local content");
			File.WriteAllText(destinationPath, "old content");
			var file = new FileInfoAdapter { fileInfo = new FileInfo(sourcePath) };

			new LocalHandler().DownloadFile(file, destinationPath);

			File.ReadAllText(destinationPath).Should().Be("local content");
			DownloadTemporaryFiles().Should().BeEmpty();
		}

		[TestMethod]
		public void LocalHandler_DownloadFile_WhenSourceDoesNotExist_ShouldLeaveNoOutput()
		{
			var sourcePath = Path.Combine(testDirectory, "missing.txt");
			var destinationPath = Path.Combine(testDirectory, "destination.txt");
			var file = new FileInfoAdapter { fileInfo = new FileInfo(sourcePath) };

			Action act = () => new LocalHandler().DownloadFile(file, destinationPath);

			act.Should().Throw<FileNotFoundException>();
			File.Exists(destinationPath).Should().BeFalse();
			DownloadTemporaryFiles().Should().BeEmpty();
		}

		[TestMethod]
		public void SharePointHandler_DownloadFile_ShouldCopyGraphContent()
		{
			var destinationPath = Path.Combine(testDirectory, "destination.txt");
			File.WriteAllText(destinationPath, "original content");
			var file = new DriveItemAdapter
			{
				DriveItem = new DriveItem { Id = "item-id", Name = "source.txt" },
			};
			var handler = new SharePointHandler(
				item =>
				{
					item.Should().BeSameAs(file.DriveItem);
					return new MemoryStream(new byte[] { 1, 2, 3 });
				});

			handler.DownloadFile(file, destinationPath);

			File.ReadAllBytes(destinationPath).Should().Equal(1, 2, 3);
			DownloadTemporaryFiles().Should().BeEmpty();
		}

		[TestMethod]
		public void SharePointHandler_DownloadFile_WhenStreamFails_ShouldLeaveNoPartialOutput()
		{
			var destinationPath = Path.Combine(testDirectory, "destination.txt");
			File.WriteAllText(destinationPath, "original content");
			var file = new DriveItemAdapter
			{
				DriveItem = new DriveItem { Id = "item-id", Name = "source.txt" },
			};
			var handler = new SharePointHandler(_ => new ThrowingReadStream());

			Action act = () => handler.DownloadFile(file, destinationPath);

			act.Should().Throw<IOException>().WithMessage("Graph stream failed.");
			File.ReadAllText(destinationPath).Should().Be("original content");
			DownloadTemporaryFiles().Should().BeEmpty();
		}

		[TestMethod]
		public void DomHandler_DownloadFile_ShouldWriteAttachmentBytes()
		{
			var destinationPath = Path.Combine(testDirectory, "destination.txt");
			var file = Mock.Of<IDocHubDomFile>();
			var handler = new DomAttachmentsHandler(
				actualFile =>
				{
					actualFile.Should().BeSameAs(file);
					return new byte[] { 4, 5, 6 };
				});

			handler.DownloadFile(file, destinationPath);

			File.ReadAllBytes(destinationPath).Should().Equal(4, 5, 6);
			DownloadTemporaryFiles().Should().BeEmpty();
		}

		[TestMethod]
		public void DomHandler_DownloadFile_WhenAttachmentReadFails_ShouldLeaveNoOutput()
		{
			var destinationPath = Path.Combine(testDirectory, "destination.txt");
			var file = Mock.Of<IDocHubDomFile>();
			var handler = new DomAttachmentsHandler(_ => throw new IOException("DOM attachment failed."));

			Action act = () => handler.DownloadFile(file, destinationPath);

			act.Should().Throw<IOException>().WithMessage("DOM attachment failed.");
			File.Exists(destinationPath).Should().BeFalse();
			DownloadTemporaryFiles().Should().BeEmpty();
		}

		private string[] DownloadTemporaryFiles()
		{
			return Directory.GetFiles(testDirectory, "*.download");
		}

		private sealed class RecordingStorageHandler : IStorageHandler
		{
			public IDocHubFile File { get; private set; } = null!;

			public string DestinationPath { get; private set; } = null!;

			public void DownloadFile(IDocHubFile file, string destinationPath)
			{
				File = file;
				DestinationPath = destinationPath;
			}

			public bool FileExists(FileExistsData data)
			{
				throw new NotSupportedException();
			}

			public List<IDocHubFile> ReadFiles(ReadData data)
			{
				throw new NotSupportedException();
			}

			public Task<List<IDocHubFile>> ReadFilesAsync(ReadData data)
			{
				return Task.FromException<List<IDocHubFile>>(new NotSupportedException());
			}

			public string UploadFile(UploadData data)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class ThrowingReadStream : Stream
		{
			public override bool CanRead => true;

			public override bool CanSeek => false;

			public override bool CanWrite => false;

			public override long Length => throw new NotSupportedException();

			public override long Position
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			public override void Flush()
			{
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new IOException("Graph stream failed.");
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
		}
	}
}
