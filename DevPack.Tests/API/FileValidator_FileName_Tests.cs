namespace DevPack.Tests.API
{
	using System;
	using FluentAssertions;
	using Skyline.DataMiner.Solutions.DocumentHub.API.DocHubClient;

	[TestClass]
	public class FileValidator_FileName_Tests
	{
		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("   ")]
		[DataRow("report")]
		[DataRow("report.pdf")]
		[DataRow("quarterly report.pdf")]
		[DataRow("ünïcødé.png")]
		[DataRow("folder/leaf.jpg")] // directory parts are stripped later, so separators are tolerated
		[DataRow(@"folder\leaf.jpg")]
		public void ValidateFileName_AcceptsUsableNames(string? name)
		{
			((Action)(() => FileValidator.ValidateFileName(name!))).Should().NotThrow();
		}

		[TestMethod]
		[DataRow("report<3", "<")]
		[DataRow("a:b", ":")]
		[DataRow("q?*|\"", "? * | \"")]
		[DataRow("x>y<z", "> <")]
		public void ValidateFileName_NamesTheOffendingCharacters(string name, string expectedChars)
		{
			((Action)(() => FileValidator.ValidateFileName(name)))
				.Should().Throw<ArgumentException>()
				.WithMessage($"The new name contains characters that are not allowed: {expectedChars}");
		}

		[TestMethod]
		public void ValidateFileName_UsesTheGivenSubject()
		{
			((Action)(() => FileValidator.ValidateFileName("report<3", "The file name")))
				.Should().Throw<ArgumentException>()
				.WithMessage("The file name contains characters that are not allowed: <");
		}

		[TestMethod]
		public void ValidateFileName_ControlCharacters_GiveGenericMessage()
		{
			((Action)(() => FileValidator.ValidateFileName("bad\u0007name")))
				.Should().Throw<ArgumentException>()
				.WithMessage("The new name contains characters that are not allowed.");
		}

		[TestMethod]
		public void ValidateFileName_TrimsOuterWhitespace()
		{
			((Action)(() => FileValidator.ValidateFileName("  report  "))).Should().NotThrow();
		}

		[TestMethod]
		[DataRow("report.")]
		[DataRow("folder/leaf.")]
		public void ValidateFileName_RejectsTrailingDotOnTheFileName(string name)
		{
			((Action)(() => FileValidator.ValidateFileName(name)))
				.Should().Throw<ArgumentException>()
				.WithMessage("The new name cannot end with a dot or a space.");
		}

		[TestMethod]
		public void ValidateFileName_RejectsNameThatIsOnlyADirectory()
		{
			((Action)(() => FileValidator.ValidateFileName("folder/")))
				.Should().Throw<ArgumentException>()
				.WithMessage("The new name must contain a file name.");
		}
	}
}
