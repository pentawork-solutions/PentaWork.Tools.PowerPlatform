using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("solution list", Description = "List all installed, visible solutions.")]
    internal partial class SolutionListCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, Output, (connection, output) => SolutionOperations.List(connection, output));

        [CommandOption("output", 'o', Description = "Write JSON to this file instead of stdout.")]
        public string? Output { get; set; }
    }

    [Command("solution export", Description = "Export a solution to a zip file.")]
    internal partial class SolutionExportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
                SolutionOperations.Export(connection, new SolutionOperations.SolutionExportRequest(UniqueName, OutputDirectory, Managed, PublishAll, Force, ExtractDirectory, Clean), output));

        [CommandParameter(0, Name = "unique-name", Description = "Unique name of the solution to export.")]
        public required string UniqueName { get; set; }

        [CommandOption("output-dir", Description = "Directory the solution zip is written to.")]
        public required string OutputDirectory { get; set; }

        [CommandOption("managed", Description = "Export as a managed solution.")]
        public bool Managed { get; set; }

        [CommandOption("publish-all", Description = "Publish all customizations before exporting.")]
        public bool PublishAll { get; set; }

        [CommandOption("force", Description = "Export even if a zip for this version already exists.")]
        public bool Force { get; set; }

        [CommandOption("extract-dir", Description = "Extract the exported solution ZIP into this directory.")]
        public string? ExtractDirectory { get; set; }

        [CommandOption("clean", Description = "DESTRUCTIVE: clear --extract-dir before extracting.")]
        public bool Clean { get; set; }
    }

    [Command("solution import", Description = "Import a solution from a zip file.")]
    internal partial class SolutionImportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
                SolutionOperations.Import(connection, new SolutionOperations.SolutionImportRequest(Zip, DeleteExisting, Overwrite, PublishWorkflows, Force), output));

        [CommandParameter(0, Name = "zip", Description = "Path to the solution zip file.")]
        public required string Zip { get; set; }

        [CommandOption("delete-existing", Description = "DESTRUCTIVE: delete the existing solution before importing.")]
        public bool DeleteExisting { get; set; }

        [CommandOption("overwrite", Description = "Overwrite unmanaged customizations.")]
        public bool Overwrite { get; set; }

        [CommandOption("publish-workflows", Description = "Publish workflows after import.")]
        public bool PublishWorkflows { get; set; }

        [CommandOption("force", Description = "Import even if the same version is already deployed.")]
        public bool Force { get; set; }
    }
}
