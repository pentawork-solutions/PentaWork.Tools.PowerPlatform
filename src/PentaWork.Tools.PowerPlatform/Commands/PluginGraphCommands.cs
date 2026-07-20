using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("plugin-graph export", Description = "Analyze plugin assemblies and render one Markdown file per entity/message.")]
    internal partial class PluginGraphExportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
                PluginGraphOperations.Export(connection, new PluginGraphOperations.PluginGraphExportRequest(Solution, Namespace, OutputDirectory, Clear, Log), output));

        [CommandOption("namespace", Description = "Namespace(s) to analyze. Comma-separated, wildcards allowed.")]
        public required string Namespace { get; set; }

        [CommandOption("output-dir", Description = "Directory the Markdown files are written to.")]
        public required string OutputDirectory { get; set; }

        [CommandOption("solution", Description = "Only analyze plugin steps/packages in this solution.")]
        public string? Solution { get; set; }

        [CommandOption("clear", Description = "DESTRUCTIVE: delete --output-dir before generating.")]
        public bool Clear { get; set; }

        [CommandOption("log", Description = "Log the IL interpreter's execution for each plugin.")]
        public bool Log { get; set; }
    }
}
