using CliFx;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Commands;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

[TestClass]
public class CliCompositionTests
{
    [TestMethod]
    public async Task RootHelp_ListsEveryDocumentedCommand()
    {
        using var console = new FakeInMemoryConsole();
        var application = new CommandLineApplicationBuilder()
            .AddCommands(
            [
                AssemblyUpdateCommand.Descriptor, ConnectionTestCommand.Descriptor,
                DataExportCommand.Descriptor, DataImportCommand.Descriptor,
                EntityExportCommand.Descriptor, EntityImportCommand.Descriptor, EntityRemoveCommand.Descriptor,
                PluginGraphExportCommand.Descriptor, ProxyGenerateCommand.Descriptor, RelationImportCommand.Descriptor,
                SharingImportCommand.Descriptor, SolutionExportCommand.Descriptor, SolutionImportCommand.Descriptor,
                SolutionListCommand.Descriptor, UserObjectListCommand.Descriptor, WebResourceUpdateCommand.Descriptor
            ])
            .UseConsole(console)
            .Build();

        var exitCode = await application.RunAsync(["--help"]);

        Assert.AreEqual(0, exitCode);
        var help = console.ReadOutputString();
        foreach (var command in new[] { "assembly update", "connection test", "data export", "data import", "entity export", "entity import", "entity remove", "plugin-graph export", "proxy generate", "relation import", "sharing import", "solution export", "solution import", "solution list", "user-object list", "webresource update" })
            StringAssert.Contains(help, command);
    }
}
