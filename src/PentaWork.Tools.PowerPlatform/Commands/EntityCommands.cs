using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using Microsoft.Xrm.Sdk;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("entity export", Description = "Export entity instances to JSON.")]
    internal partial class EntityExportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, Output, (connection, output) =>
            {
                if (ValidFor is not ("create" or "update" or "all"))
                    throw new CliValidationException("--valid-for must be create, update, or all.");
                var request = new EntityOperations.EntityExportRequest(LogicalName, Columns, FilterTripleParser.ParseAll(Filters), IgnoreColumns, Relations, PageSize, PageNumber, All, Sharings, ValidFor == "create", ValidFor == "update");
                output.WriteObject(EntityOperations.Export(connection, request, output));
            });

        [CommandParameter(0, Name = "logical-name", Description = "Logical name of the entity type to export.")]
        public required string LogicalName { get; set; }

        [CommandOption("column", Description = "Column to retrieve. Repeatable. Defaults to all columns.")]
        public IReadOnlyList<string> Columns { get; set; } = [];

        [CommandOption("filter", Description = "Filter triple field:operator:value. Repeatable.")]
        public IReadOnlyList<string> Filters { get; set; } = [];

        [CommandOption("ignore-column", Description = "Column to exclude from the export. Repeatable.")]
        public IReadOnlyList<string> IgnoreColumns { get; set; } = [];

        [CommandOption("relation", Description = "N:M relationship schema name to export alongside each entity. Repeatable.")]
        public IReadOnlyList<string> Relations { get; set; } = [];

        [CommandOption("page-size", Description = "Page size.")]
        public int PageSize { get; set; } = 500;

        [CommandOption("page-number", Description = "Page to retrieve (ignored with --all).")]
        public int PageNumber { get; set; } = 1;

        [CommandOption("all", Description = "Retrieve every page instead of just --page-number.")]
        public bool All { get; set; }

        [CommandOption("sharings", Description = "Include sharings (POA) for each exported entity.")]
        public bool Sharings { get; set; }

        [CommandOption("valid-for", Description = "Which attributes to export: create, update, or all.")]
        public string ValidFor { get; set; } = "all";

        [CommandOption("output", 'o', Description = "Write JSON to this file instead of stdout.")]
        public string? Output { get; set; }
    }

    [Command("entity import", Description = "Import entity instances from JSON.")]
    internal partial class EntityImportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
            {
                var entityData = ConsoleIO.ReadInputJson<EntityData>(Input);
                var request = new EntityOperations.EntityImportRequest(entityData, new EntityReference(FallbackOwnerType, FallbackOwnerId), MapByName, TakeFirst, ImpersonateOwner, MapByNameLookup, CreateOnly);
                EntityOperations.Import(connection, request, output);
            });

        [CommandOption("input", 'i', Description = "Read entity JSON from this file instead of stdin.")]
        public string? Input { get; set; }

        [CommandOption("fallback-owner-type", Description = "Logical name of the fallback owner.")]
        public required string FallbackOwnerType { get; set; }

        [CommandOption("fallback-owner-id", Description = "Id of the fallback owner.")]
        public required Guid FallbackOwnerId { get; set; }

        [CommandOption("map-by-name", Description = "Match existing entities by primary name instead of id.")]
        public bool MapByName { get; set; }

        [CommandOption("take-first", Description = "With --map-by-name, update the first ambiguous match.")]
        public bool TakeFirst { get; set; }

        [CommandOption("impersonate-owner", Description = "Impersonate the target owner while creating or updating.")]
        public bool ImpersonateOwner { get; set; }

        [CommandOption("map-by-name-lookup", Description = "Lookup attribute to re-resolve by name. Repeatable.")]
        public IReadOnlyList<string> MapByNameLookup { get; set; } = [];

        [CommandOption("create-only", Description = "Entity display name that should only be created. Repeatable.")]
        public IReadOnlyList<string> CreateOnly { get; set; } = [];
    }

    [Command("entity remove", Description = "DESTRUCTIVE: delete all entity instances matching the given filters, without further confirmation.")]
    internal partial class EntityRemoveCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
            {
                var result = EntityOperations.Remove(connection, new EntityOperations.EntityRemoveRequest(LogicalName, FilterTripleParser.ParseAll(Filters)), output);
                if (result.FailedCount > 0)
                    throw new CliPartialFailureException($"{result.FailedCount} of {result.FailedCount + result.DeletedCount} deletes failed - see warnings above.");
            });

        [CommandParameter(0, Name = "logical-name", Description = "Logical name of the entity type to remove instances of.")]
        public required string LogicalName { get; set; }

        [CommandOption("filter", Description = "Filter triple field:operator:value. Repeatable.")]
        public IReadOnlyList<string> Filters { get; set; } = [];
    }
}
