using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Workflows;

internal record DataWorkflowExportRequest(DataWorkflowConfiguration Configuration, string OutputDirectory);

internal record DataWorkflowImportRequest(
    DataWorkflowConfiguration Configuration,
    string InputDirectory,
    EntityReference FallbackOwner);

/// <summary>
/// Orchestrates recurring data workflows (export/import) based on a declarative
/// <see cref="DataWorkflowConfiguration"/>.
/// </summary>
internal static partial class DataWorkflowOperations
{
    /// <summary>
    /// Gets the full file path for an exported entity data file.
    /// </summary>
    internal static string GetDataFilePath(string directory, string fileName) =>
        Path.Combine(directory, $"{fileName}.json");

    /// <summary>
    /// Exports every configured <see cref="DataWorkflowItem"/> sequentially as a JSON file
    /// to the specified output directory.
    /// </summary>
    internal static IReadOnlyList<string> Export(ServiceClient connection, DataWorkflowExportRequest request, IOperationOutput output)
    {
        var paths = new List<string>();
        Directory.CreateDirectory(request.OutputDirectory);

        foreach (var item in request.Configuration.Items)
        {
            var parsedFilters = FilterTripleParser.ParseAll(item.Filters);

            var entityRequest = new EntityOperations.EntityExportRequest(
                item.LogicalName,
                [],
                parsedFilters,
                item.IgnoreColumns,
                item.Relations,
                500,
                1,
                true,
                item.Sharings,
                false,
                false);

            var entityData = EntityOperations.Export(connection, entityRequest, output);

            var filePath = GetDataFilePath(request.OutputDirectory, item.FileName);
            ConsoleIO.WriteOutput(filePath, System.Text.Json.JsonSerializer.Serialize(entityData, JsonSettings.Default));

            paths.Add(filePath);
        }

        output.WriteObject(paths);
        return paths;
    }

    /// <summary>
    /// Imports every configured <see cref="DataWorkflowItem"/> sequentially from a JSON file
    /// and performs optional purge, sharing, and relation imports.
    /// </summary>
    internal static void Import(ServiceClient connection, DataWorkflowImportRequest request, IOperationOutput output)
    {
        foreach (var item in request.Configuration.Items)
        {
            var filePath = GetDataFilePath(request.InputDirectory, item.FileName);

            if (!File.Exists(filePath))
            {
                output.WriteWarning($"Input file not found, skipping: {filePath}");
                continue;
            }

            var entityData = ConsoleIO.ReadInputJson<EntityData>(filePath);

            if (item.PurgeTarget)
            {
                var removeResult = EntityOperations.Remove(connection, new EntityOperations.EntityRemoveRequest(item.LogicalName, []), output);
                if (removeResult.FailedCount > 0)
                    throw new CliPartialFailureException($"{removeResult.FailedCount} deletions failed during purge for '{item.LogicalName}'.");
            }

            EntityOperations.Import(connection, new EntityOperations.EntityImportRequest(
                entityData,
                request.FallbackOwner,
                item.MapByName,
                item.MapByName,
                item.ImpersonateOwner,
                item.MapByNameLookups,
                item.CreateOnly), output);

            if (item.Sharings)
            {
                SharingOperations.Import(connection, new SharingOperations.SharingImportRequest(entityData, true, true), output);
            }

            if (item.Relations.Count > 0)
            {
                RelationOperations.Import(connection, new RelationOperations.RelationImportRequest(entityData, false, true, true, []), output);
            }
        }
    }
}
