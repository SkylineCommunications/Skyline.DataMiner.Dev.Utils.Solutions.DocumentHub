namespace DevPack.Tests.API
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;
	using Skyline.DataMiner.Solutions.DocumentHub.API.FileAdapters;
	using Skyline.DataMiner.Solutions.DocumentHub.API.Paging;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers;
	using Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.DTOs;
	using Skyline.DataMiner.Solutions.DocumentHub.SDM.Models;
	using Skyline.DataMiner.Solutions.DocumentHub.Tests.Setup;

	[TestClass]
	public class LocalHandler_Tests
	{
		private string workspace = null!;
		private string publicRoot = null!;
		private LocalHandler handler = null!;

		[TestInitialize]
		public void Initialize()
		{
			this.workspace = Path.Combine(Path.GetTempPath(), "DocumentHubLocal_" + Guid.NewGuid().ToString("N"));
			this.publicRoot = Path.Combine(this.workspace, "Webpages", "Public");
			Directory.CreateDirectory(this.publicRoot);
			this.handler = new LocalHandler(this.publicRoot);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(this.workspace, true);
		}

		[TestMethod]
		public void ProductionRoot_IsPublic()
		{
			LocalHandler.ResolveLocalDirectory(null!).Should().Be(@"C:\Skyline DataMiner\Webpages\Public");
			LocalHandler.ResolveLocalDirectory("Icons").Should().Be(@"C:\Skyline DataMiner\Webpages\Public\Icons");
			LocalHandler.ResolveLocalDirectory("WebFileManager/docs").Should().Be(@"C:\Skyline DataMiner\Webpages\Public\WebFileManager\docs");
		}

		[TestMethod]
		public void DefaultBucket_ListsRootIconsAndLegacyFilesWithPublicRelativeMetadata()
		{
			var paths = new[] { "root.txt", @"Icons\icon.png", @"WebFileManager\docs\legacy.pdf" };
			foreach (var path in paths)
				this.CreateFile(path);

			var files = this.handler.ReadFiles(new WebFileReadData { Bucket = Bucket("/") });

			files.Select(f => f.GetFilePath()).Should().BeEquivalentTo(paths.Select(p => Path.Combine(this.publicRoot, p)));
			foreach (var file in files)
			{
				var relative = FileInfoAdapter.GetRelativePath(file.GetFilePath(), this.publicRoot);
				file.GetWebPath().Should().Be("/Public/" + relative);
				file.GetDirectory().Should().Be(FileInfoAdapter.GetRelativePath(Path.GetDirectoryName(file.GetFilePath())!, this.publicRoot));
			}
		}

		[TestMethod]
		[DataRow("Icons")]
		[DataRow("WebFileManager")]
		[DataRow("WebFileManager/docs")]
		public void ConfiguredBucket_ListsAndUploadsToPublicRelativeFolder(string uploadPath)
		{
			var source = Path.Combine(this.workspace, "source.txt");
			File.WriteAllText(source, "uploaded");
			var bucket = Bucket(uploadPath);

			var url = this.handler.UploadFile(new WebFileUploadData { Bucket = bucket, FilePath = source, Name = "file.txt" });
			url.Should().Be("/Public/" + uploadPath + "/file.txt");
			File.ReadAllText(Path.Combine(this.publicRoot, uploadPath, "file.txt")).Should().Be("uploaded");
			this.handler.FileExists(new WebFileExistsData { Directory = uploadPath, Name = "file.txt" }).Should().BeTrue();
			var file = this.handler.ReadFiles(new WebFileReadData { Bucket = bucket }).Should().ContainSingle().Subject;

			var destination = Path.Combine(this.workspace, "download.txt");
			this.handler.DownloadFile(file, destination);
			File.ReadAllText(destination).Should().Be("uploaded");

			// The production delete scripts pass the exact listed path, including nested directories.
			var api = new Files(ConnectionHelper.CreateConnection(), (_, __) => this.handler);
			api.DeleteFile(bucket, file.GetFilePath());
			this.handler.FileExists(new WebFileExistsData { Directory = uploadPath, Name = "file.txt" }).Should().BeFalse();
		}

		[TestMethod]
		public void PagingAndFilters_DoNotSkipFilesWhenCheckingForAnotherPage()
		{
			var expected = new[] { "match.txt", @"Icons\match.TXT", @"WebFileManager\match.txt" };
			foreach (var path in expected)
				this.CreateFile(path);
			this.CreateFile("unrelated.txt");
			this.CreateFile(@"Icons\match.png");
			var context = new LocalPageData { PageSize = 1 };
			var request = new WebFileReadData { Bucket = Bucket("", "txt"), Filter = "MATCH", Context = context };
			var files = new List<IDocHubFile>();

			do
			{
				files.AddRange(this.handler.ReadFiles(request));
				context.HasNextPage().Should().Be(context.HasNextPage());
			}
			while (context.HasNextPage());

			files.Select(f => f.GetFilePath()).Should().BeEquivalentTo(expected.Select(p => Path.Combine(this.publicRoot, p)));
		}

		[TestMethod]
		public void DeleteExactListedPath_DoesNotDeleteSameNamedFileAtBucketRoot()
		{
			this.CreateFile(@"Icons\file.txt");
			var nested = this.CreateFile(@"Icons\Nested\file.txt");
			this.handler.DeleteFile(new DeleteData { Bucket = Bucket("Icons"), Name = nested });

			File.Exists(nested).Should().BeFalse();
			File.Exists(Path.Combine(this.publicRoot, @"Icons\file.txt")).Should().BeTrue();
		}

		[TestMethod]
		public void UploadImage_UsesPublicRelativeDirectory()
		{
			using (var image = new Bitmap(1, 1))
				this.handler.UploadImage(image, "Icons", "image");

			File.Exists(Path.Combine(this.publicRoot, @"Icons\image.jpeg")).Should().BeTrue();
		}

		[TestMethod]
		[DataRow("../outside")]
		[DataRow("Icons/../../outside")]
		[DataRow(@"C:\Windows")]
		[DataRow(@"C:relative")]
		[DataRow(@"\\server\share")]
		[DataRow(@"\\?\C:\Windows")]
		[DataRow("file:stream")]
		[DataRow(@"C:\Skyline DataMiner\Webpages\PublicSibling")]
		public void BucketPathsOutsidePublic_AreRejected(string path)
		{
			Action resolve = () => LocalHandler.ResolveLocalDirectory(path);
			resolve.Should().Throw<ArgumentException>();
		}

		[TestMethod]
		public void Traversal_IsRejectedAcrossLocalOperations()
		{
			var outside = Path.Combine(this.workspace, "outside.txt");
			File.WriteAllText(outside, "outside");
			Action exists = () => this.handler.FileExists(new WebFileExistsData { Directory = "", Name = "../../outside.txt" });
			Action upload = () => this.handler.UploadFile(new WebFileUploadData { Bucket = Bucket(""), FilePath = outside, Name = "../../outside.txt" });
			Action delete = () => this.handler.DeleteFile(new DeleteData { Bucket = Bucket(""), Name = outside });
			Action download = () => this.handler.DownloadFile(new FileInfoAdapter { fileInfo = new FileInfo(outside) }, Path.Combine(this.workspace, "copy.txt"));
			Action read = () => this.handler.ReadFiles(new WebFileReadData { Bucket = Bucket("../") });

			exists.Should().Throw<ArgumentException>();
			upload.Should().Throw<ArgumentException>();
			delete.Should().Throw<ArgumentException>();
			download.Should().Throw<ArgumentException>();
			read.Should().Throw<ArgumentException>();
			File.ReadAllText(outside).Should().Be("outside");
		}

		[TestMethod]
		public void AbsolutePathsWithinPublic_ArePreservedButOtherBucketFilesAreRejected()
		{
			var path = this.CreateFile(@"WebFileManager\legacy.txt");
			this.handler.ReadFiles(new WebFileReadData { Bucket = Bucket(Path.GetDirectoryName(path)!) })
				.Should().ContainSingle().Which.GetFilePath().Should().Be(path);

			Action delete = () => this.handler.DeleteFile(new DeleteData { Bucket = Bucket("Icons"), Name = path });
			delete.Should().Throw<ArgumentException>();
			File.Exists(path).Should().BeTrue();
		}

		[TestMethod]
		public void JunctionOutsidePublic_IsRejectedWithoutReadingWritingOrDeletingItsTarget()
		{
			var outside = Path.Combine(this.workspace, "outside");
			Directory.CreateDirectory(outside);
			var protectedFile = Path.Combine(outside, "file.txt");
			File.WriteAllText(protectedFile, "protected");
			var junction = Path.Combine(this.publicRoot, "link");
			using (var process = Process.Start(new ProcessStartInfo
			{
				FileName = Environment.GetEnvironmentVariable("ComSpec")!,
				Arguments = "/c mklink /J \"" + junction + "\" \"" + outside + "\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			})!)
			{
				process.WaitForExit();
				process.ExitCode.Should().Be(0, process.StandardError.ReadToEnd());
			}

			try
			{
				Action read = () => this.handler.ReadFiles(new WebFileReadData { Bucket = Bucket("") });
				Action exists = () => this.handler.FileExists(new WebFileExistsData { Directory = "link", Name = "file.txt" });
				Action upload = () => this.handler.UploadFile(new WebFileUploadData { Bucket = Bucket("link"), FilePath = protectedFile, Name = "file.txt" });
				Action delete = () => this.handler.DeleteFile(new DeleteData { Bucket = Bucket(""), Name = @"link\file.txt" });
				Action download = () => this.handler.DownloadFile(
					new FileInfoAdapter { fileInfo = new FileInfo(Path.Combine(junction, "file.txt")) },
					Path.Combine(this.workspace, "copy.txt"));

				read.Should().Throw<IOException>();
				exists.Should().Throw<IOException>();
				upload.Should().Throw<IOException>();
				delete.Should().Throw<IOException>();
				download.Should().Throw<IOException>();
				File.ReadAllText(protectedFile).Should().Be("protected");
			}
			finally
			{
				// Remove the link itself before recursive test-directory cleanup.
				Directory.Delete(junction);
			}
		}

		private static DocumentBucket Bucket(string path, string extensions = "")
		{
			return new DocumentBucket { StorageType = StorageType.Local, UploadPath = path, Extensions = extensions };
		}

		private string CreateFile(string relativePath)
		{
			var path = Path.Combine(this.publicRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, relativePath);
			return path;
		}
	}
}
