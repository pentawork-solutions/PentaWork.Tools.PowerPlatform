using CliFx;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Commands;
using PentaWork.Tools.PowerPlatform.Workflows;

namespace PentaWork.Tools.PowerPlatform.Tests.Workflows;

[TestClass]
public class DataWorkflowExportTests
{
    [TestMethod]
    public void GetDataFilePath_CombinesDirectoryAndFileNameWithJsonExtension()
    {
        var path = DataWorkflowOperations.GetDataFilePath("/some/dir", "accounts");
        Assert.AreEqual(Path.Combine("/some/dir", "accounts.json"), path);
    }

    [TestMethod]
    public void GetDataFilePath_WithSpacesInFileName_DoesNotThrow()
    {
        var path = DataWorkflowOperations.GetDataFilePath("/some/dir", "my entities");
        Assert.AreEqual(Path.Combine("/some/dir", "my entities.json"), path);
    }

    [TestMethod]
    public async Task DataExportCommand_MissingConfig_ReturnsNonZeroExitCode()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(DataExportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync(["data", "export"]);

        Assert.AreNotEqual(0, exitCode);
        var error = console.ReadErrorString();
        Assert.IsTrue(error.Contains("--config") || error.Contains("config") || error.Contains("required"),
            $"Expected error about missing --config, got: {error}");
    }

    [TestMethod]
    public async Task DataExportCommand_MissingOutputDir_ReturnsNonZeroExitCode()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(DataExportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync(["data", "export", "--config", "test.json"]);

        Assert.AreNotEqual(0, exitCode);
        var error = console.ReadErrorString();
        Assert.IsTrue(error.Contains("--output-dir") || error.Contains("output-dir") || error.Contains("required"),
            $"Expected error about missing --output-dir, got: {error}");
    }
}
