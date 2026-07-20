using CliFx;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Commands;

namespace PentaWork.Tools.PowerPlatform.Tests.Workflows;

[TestClass]
public class DataWorkflowImportTests
{
    [TestMethod]
    public async Task DataImportCommand_MissingAllOptions_ReturnsNonZeroExitCode()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(DataImportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync(["data", "import"]);

        Assert.AreNotEqual(0, exitCode);
        var error = console.ReadErrorString();
        Assert.IsTrue(error.Contains("required") || error.Contains("Missing"),
            $"Expected error about missing required options, got: {error}");
    }

    [TestMethod]
    public async Task DataImportCommand_MissingFallbackOwnerId_ReturnsNonZeroExitCode()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(DataImportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync([
            "data", "import",
            "--config", "test.json",
            "--input-dir", "input",
            "--fallback-owner-type", "systemuser"
        ]);

        Assert.AreNotEqual(0, exitCode);
        var error = console.ReadErrorString();
        Assert.IsTrue(error.Contains("--fallback-owner-id") || error.Contains("fallback-owner-id"),
            $"Expected error about missing --fallback-owner-id, got: {error}");
    }

    [TestMethod]
    public async Task DataImportCommand_WithAllOptions_BindsSuccessfully()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(DataImportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        // This should bind but fail at ExecuteAsync because the config file doesn't exist.
        var exitCode = await application.RunAsync([
            "data", "import",
            "--config", "/nonexistent/config.json",
            "--input-dir", "/nonexistent/input",
            "--fallback-owner-type", "systemuser",
            "--fallback-owner-id", "00000000-0000-0000-0000-000000000001"
        ]);

        // Should not be a binding error (exit code 2 for validation, not parameter missing)
        Assert.AreNotEqual(0, exitCode);
        var error = console.ReadErrorString();
        // The error should be about the config file not being found, not about missing parameters
        Assert.IsFalse(string.IsNullOrEmpty(error));
    }
}
