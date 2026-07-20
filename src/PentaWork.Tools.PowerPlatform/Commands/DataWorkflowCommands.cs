using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using Microsoft.Xrm.Sdk;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Workflows;

namespace PentaWork.Tools.PowerPlatform.Commands;

[Command("data export", Description = "Export configured entity data sets to JSON files.")]
internal partial class DataExportCommand : DataverseCommand
{
    [CommandOption("config", Description = "Path to the data workflow JSON configuration.")]
    public required string Config { get; set; }

    [CommandOption("output-dir", Description = "Directory receiving <FileName>.json for every configured item.")]
    public required string OutputDirectory { get; set; }

    public override ValueTask ExecuteAsync(IConsole console) =>
        RunWithConnectionAsync(console, null, (connection, output) =>
        {
            var configuration = DataWorkflowConfigurationParser.Load(Config);
            var request = new DataWorkflowExportRequest(configuration, OutputDirectory);
            DataWorkflowOperations.Export(connection, request, output);
        });
}

[Command("data import", Description = "Import configured entity data sets from JSON files.")]
internal partial class DataImportCommand : DataverseCommand
{
    [CommandOption("config", Description = "Path to the data workflow JSON configuration.")]
    public required string Config { get; set; }

    [CommandOption("input-dir", Description = "Directory containing <FileName>.json for every configured item.")]
    public required string InputDirectory { get; set; }

    [CommandOption("fallback-owner-type", Description = "Logical name of the fallback owner.")]
    public required string FallbackOwnerType { get; set; }

    [CommandOption("fallback-owner-id", Description = "Id of the fallback owner.")]
    public required Guid FallbackOwnerId { get; set; }

    public override ValueTask ExecuteAsync(IConsole console) =>
        RunWithConnectionAsync(console, null, (connection, output) =>
        {
            var configuration = DataWorkflowConfigurationParser.Load(Config);
            var request = new DataWorkflowImportRequest(configuration, InputDirectory, new EntityReference(FallbackOwnerType, FallbackOwnerId));
            DataWorkflowOperations.Import(connection, request, output);
        });
}
