using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("relation import", Description = "Import N:M relations from JSON.")]
    internal partial class RelationImportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
            {
                var entityData = ConsoleIO.ReadInputJson<EntityData>(Input);
                var request = new RelationOperations.RelationImportRequest(entityData, ClearTarget, MapEntitiesByName, MapRelatedByName, ParseRelationConditions(RelationConditions));
                RelationOperations.Import(connection, request, output);
            });

        private static List<RelationCondition> ParseRelationConditions(IEnumerable<string> triples) =>
            triples.Select(triple =>
            {
                var parts = triple.Split(':', 3);
                if (parts.Length != 3)
                    throw new CliValidationException($"Invalid relation condition '{triple}'. Expected format: schemaName:targetField:sourceField");
                return new RelationCondition { SchemaName = parts[0], TargetField = parts[1], SourceField = parts[2] };
            }).ToList();

        [CommandOption("input", 'i', Description = "Read entity and relation JSON from this file instead of stdin.")]
        public string? Input { get; set; }

        [CommandOption("clear-target", Description = "Clear the target's existing relations before importing.")]
        public bool ClearTarget { get; set; }

        [CommandOption("map-entities-by-name", Description = "Match the relating entity by primary name instead of id.")]
        public bool MapEntitiesByName { get; set; }

        [CommandOption("map-related-by-name", Description = "Match related entities by primary name instead of id.")]
        public bool MapRelatedByName { get; set; }

        [CommandOption("relation-condition", Description = "Mapping condition schemaName:targetField:sourceField. Repeatable.")]
        public IReadOnlyList<string> RelationConditions { get; set; } = [];
    }
}
