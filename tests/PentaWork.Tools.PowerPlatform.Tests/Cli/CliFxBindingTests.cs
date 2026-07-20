using CliFx;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Commands;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

[TestClass]
public class CliFxBindingTests
{
    [TestMethod]
    public async Task EntityExportHelp_ContainsTheInheritedConnectionOptions()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(EntityExportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync(["entity", "export", "--help"]);

        Assert.AreEqual(0, exitCode);
        var help = console.ReadOutputString();
        StringAssert.Contains(help, "--connection-string");
        StringAssert.Contains(help, "PW_PP_CONNECTION_STRING");
        StringAssert.Contains(help, "--timeout-minutes");
        StringAssert.Contains(help, "--verbose");
    }

    [TestMethod]
    public async Task EntityExport_MissingRequiredParameter_ReturnsValidationExitCode()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommand(EntityExportCommand.Descriptor)
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync(["entity", "export"]);

        Assert.AreNotEqual(0, exitCode);
        StringAssert.Contains(console.ReadErrorString(), "Missing required parameter");
    }
}
