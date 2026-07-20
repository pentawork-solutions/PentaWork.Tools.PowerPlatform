using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Workflows;

/// <summary>
/// Declarative configuration for recurring data export/import workflows.
/// Loaded from a JSON file and validated locally.
/// </summary>
internal sealed record DataWorkflowConfiguration(IReadOnlyList<DataWorkflowItem> Items);

internal sealed record DataWorkflowItem(
    string FileName,
    string LogicalName,
    IReadOnlyList<string> Filters,
    IReadOnlyList<string> IgnoreColumns,
    IReadOnlyList<string> Relations,
    bool Sharings,
    bool MapByName,
    IReadOnlyList<string> MapByNameLookups,
    IReadOnlyList<string> CreateOnly,
    bool ImpersonateOwner,
    bool PurgeTarget);

internal static partial class DataWorkflowConfigurationParser
{
    /// <summary>
    /// Loads a <see cref="DataWorkflowConfiguration"/> from a JSON file.
    /// Reads and deserializes it through <see cref="ConsoleIO.ReadInputJson{T}"/>.
    /// </summary>
    internal static DataWorkflowConfiguration Load(string configurationPath)
    {
        var configuration = ConsoleIO.ReadInputJson<DataWorkflowConfiguration>(configurationPath);
        var normalizedConfiguration = Normalize(configuration);
        Validate(normalizedConfiguration);
        return normalizedConfiguration;
    }

    private static DataWorkflowConfiguration Normalize(DataWorkflowConfiguration configuration) =>
        configuration with
        {
            Items = (configuration.Items ?? []).Select(item => item with
            {
                Filters = item.Filters ?? [],
                IgnoreColumns = item.IgnoreColumns ?? [],
                Relations = item.Relations ?? [],
                MapByNameLookups = item.MapByNameLookups ?? [],
                CreateOnly = item.CreateOnly ?? []
            }).ToArray()
        };

    /// <summary>
    /// Validates a <see cref="DataWorkflowConfiguration"/> and throws
    /// <see cref="CliValidationException"/> for violations.
    /// </summary>
    internal static void Validate(DataWorkflowConfiguration configuration)
    {
        if (configuration.Items is null || configuration.Items.Count == 0)
            throw new CliValidationException("Data workflow configuration must contain at least one item.");

        var seenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < configuration.Items.Count; i++)
        {
            var item = configuration.Items[i];

            if (string.IsNullOrEmpty(item.FileName))
                throw new CliValidationException($"Data workflow item at index {i} has an empty FileName.");

            if (string.IsNullOrEmpty(item.LogicalName))
                throw new CliValidationException($"Data workflow item at index {i} has an empty LogicalName.");

            if (item.FileName.Contains('/') || item.FileName.Contains('\\'))
                throw new CliValidationException($"Data workflow item FileName '{item.FileName}' must not contain a directory separator.");

            if (!seenFileNames.Add(item.FileName))
                throw new CliValidationException($"Data workflow configuration contains duplicate FileName '{item.FileName}'.");
        }
    }
}
