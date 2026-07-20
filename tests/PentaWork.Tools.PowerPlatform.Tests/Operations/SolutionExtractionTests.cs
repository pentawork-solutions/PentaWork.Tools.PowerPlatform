using System.IO.Compression;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Tests.Operations;

[TestClass]
public class SolutionExtractionTests
{
    private static string CreateTempZip(string directory, params (string Name, string Content)[] entries)
    {
        Directory.CreateDirectory(directory);
        var zipPath = Path.Combine(directory, "solution.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return zipPath;
    }

    [TestMethod]
    public void ExtractZip_ExtractsAllFilesToTarget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pw-pp-extract-" + Guid.NewGuid());
        try
        {
            var zipPath = CreateTempZip(tempDir,
                ("solution.xml", "<solution></solution>"),
                ("customizations.xml", "<customizations></customizations>"));

            var extractDir = Path.Combine(tempDir, "extracted");
            SolutionOperations.ExtractZip(zipPath, extractDir, cleanExtractDirectory: false);

            Assert.IsTrue(File.Exists(Path.Combine(extractDir, "solution.xml")), "solution.xml should exist");
            Assert.IsTrue(File.Exists(Path.Combine(extractDir, "customizations.xml")), "customizations.xml should exist");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void ExtractZip_WithoutClean_PreservesUnrelatedPreExistingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pw-pp-extract-" + Guid.NewGuid());
        try
        {
            var zipPath = CreateTempZip(tempDir,
                ("solution.xml", "<solution></solution>"));

            var extractDir = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractDir);
            var oldFilePath = Path.Combine(extractDir, "old_entry.txt");
            File.WriteAllText(oldFilePath, "old content");

            SolutionOperations.ExtractZip(zipPath, extractDir, cleanExtractDirectory: false);

            Assert.IsTrue(File.Exists(oldFilePath), "old_entry.txt should be preserved without --clean");
            Assert.IsTrue(File.Exists(Path.Combine(extractDir, "solution.xml")), "solution.xml should exist");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void ExtractZip_WithClean_RemovesPreExistingFileInTarget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pw-pp-extract-" + Guid.NewGuid());
        try
        {
            var zipPath = CreateTempZip(tempDir, ("solution.xml", "<solution></solution>"));
            var extractDir = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractDir);
            var oldFilePath = Path.Combine(extractDir, "old_entry.txt");
            File.WriteAllText(oldFilePath, "old content");

            SolutionOperations.ExtractZip(zipPath, extractDir, cleanExtractDirectory: true);

            Assert.IsFalse(File.Exists(oldFilePath), "old_entry.txt should have been deleted by --clean");
            Assert.IsTrue(File.Exists(Path.Combine(extractDir, "solution.xml")), "solution.xml should exist");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void ExtractZip_WithDotPath_ThrowsCliValidationException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pw-pp-extract-" + Guid.NewGuid());
        try
        {
            var zipPath = CreateTempZip(tempDir,
                ("solution.xml", "<solution></solution>"));

            Assert.ThrowsExactly<CliValidationException>(
                () => SolutionOperations.ExtractZip(zipPath, ".", cleanExtractDirectory: true));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void ExtractZip_WithFilesystemRoot_ThrowsCliValidationException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pw-pp-extract-" + Guid.NewGuid());
        try
        {
            var zipPath = CreateTempZip(tempDir,
                ("solution.xml", "<solution></solution>"));

            var root = Path.GetPathRoot(Path.GetTempPath())!;
            Assert.ThrowsExactly<CliValidationException>(
                () => SolutionOperations.ExtractZip(zipPath, root, cleanExtractDirectory: true));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void SolutionExportResult_CarriesExtractDirectory()
    {
        var result = new SolutionOperations.SolutionExportResult("test.zip", false, "extracted");
        Assert.AreEqual("extracted", result.ExtractDirectory);
        Assert.AreEqual("test.zip", result.FilePath);
        Assert.IsFalse(result.Skipped);
    }

    [TestMethod]
    public void SolutionExportResult_WithoutExtractDirectory_IsNull()
    {
        var result = new SolutionOperations.SolutionExportResult("test.zip", false, null);
        Assert.IsNull(result.ExtractDirectory);
    }

    [TestMethod]
    public void ValidateExtractOptions_WithCleanWithoutExtractDirectory_ThrowsValidationError()
    {
        var ex = Assert.ThrowsExactly<CliValidationException>(
            () => SolutionOperations.ValidateExtractOptions(null, cleanExtractDirectory: true));

        Assert.AreEqual("--clean requires --extract-dir.", ex.Message);
    }
}
