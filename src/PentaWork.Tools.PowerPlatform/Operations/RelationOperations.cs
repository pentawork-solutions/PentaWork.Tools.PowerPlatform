using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>N:M relation import operation backing the `relation import` command.</summary>
    internal static class RelationOperations
    {
        internal record RelationImportRequest(
            EntityData EntityData,
            bool ClearTargetRelations,
            bool MapEntitiesByName,
            bool MapRelatedEntitiesByName,
            IReadOnlyList<RelationCondition> RelationConditions);

        public static void Import(ServiceClient connection, RelationImportRequest request, IOperationOutput output)
        {
            var processedEntities = 0;
            foreach (var entityInfo in request.EntityData.Entities)
            {
                if (entityInfo.Relations.Length == 0) continue;

                output.WriteProgress("Importing", $"Importing relations for '{entityInfo.Name}' ...", 100 * processedEntities++ / request.EntityData.Entities.Length);

                var matchingSystemEntities = connection.GetMatchingEntities(request.EntityData.EntityName, entityInfo.Id, entityInfo.Name, request.MapEntitiesByName ? request.EntityData.PrimaryNameField : null);
                if (matchingSystemEntities.Count > 1) output.WriteWarning($"Multiple entities for '{entityInfo.Name}' found ...");
                if (matchingSystemEntities.Count == 0)
                {
                    output.WriteWarning($"Entity '{entityInfo.Name}' not found!");
                    continue;
                }

                var relatingEntity = matchingSystemEntities.First();
                if (request.ClearTargetRelations) ClearRelations(connection, entityInfo, relatingEntity, output);
                ImportRelations(connection, entityInfo, relatingEntity, request, output);
            }
            output.WriteProgress("Importing", "Done!", completed: true);
        }

        private static void ClearRelations(ServiceClient connection, EntityInfo entityInfo, Entity relatingEntity, IOperationOutput output)
        {
            var processedRelations = 0;
            foreach (var relationInfo in entityInfo.Relations)
            {
                output.WriteProgress("Clearing", $"Clearing relations for schema '{relationInfo.SchemaName}' ...", 100 * processedRelations++ / Math.Max(1, entityInfo.Relations.Sum(r => r.Entities.Length)));
                var query = new QueryExpression
                {
                    EntityName = relationInfo.IntersectEntityName,
                    ColumnSet = new ColumnSet(true)
                };
                query.Criteria.AddCondition(relationInfo.FromPrimaryIdAttribute, ConditionOperator.Equal, relatingEntity.Id);

                var relations = connection.Query(query);
                connection.Disassociate(relatingEntity.LogicalName, relatingEntity.Id, new Relationship(relationInfo.SchemaName),
                    new EntityReferenceCollection(relations
                        .Select(r => new EntityReference(relationInfo.ToLogicalName, r.GetAttributeValue<Guid>(relationInfo.ToPrimaryIdAttribute)))
                        .ToList()
                    ));

                output.WriteProgress("Clearing", "Done!", completed: true);
            }
        }

        private static void ImportRelations(ServiceClient connection, EntityInfo entityInfo, Entity relatingEntity, RelationImportRequest request, IOperationOutput output)
        {
            var processedRelations = 0;
            foreach (var relationInfo in entityInfo.Relations)
            {
                var relatedEntityReferences = new EntityReferenceCollection();
                foreach (var relation in relationInfo.Entities)
                {
                    output.WriteProgress("Importing", $"Importing relations for schema '{relationInfo.SchemaName}' ...", 100 * processedRelations++ / Math.Max(1, entityInfo.Relations.Sum(r => r.Entities.Length)));

                    var relatedEntity = connection.TryRetrieve(relationInfo.ToLogicalName, relation.Id, new ColumnSet());
                    if (request.MapRelatedEntitiesByName)
                    {
                        var query = new QueryExpression
                        {
                            EntityName = relationInfo.ToLogicalName,
                            ColumnSet = new ColumnSet(true)
                        };
                        query.Criteria.AddCondition(relationInfo.ToPrimaryNameAttribute, ConditionOperator.Equal, relation.Name);
                        AddRelationConditions(query, relatingEntity, relationInfo.SchemaName, request);

                        var matches = connection.Query(query);
                        if (matches.Count > 1) output.WriteWarning($"Multiple related entities for '{relation.Name}' found ...");
                        relatedEntity = matches.FirstOrDefault();
                    }
                    if (relatedEntity == null)
                    {
                        output.WriteWarning($"Related entity '{relation.Name}' not found!");
                        continue;
                    }
                    relatedEntityReferences.Add(relatedEntity.ToEntityReference());
                }

                // Disassociate first to ensure no duplicates get associated!
                connection.Disassociate(relatingEntity.LogicalName, relatingEntity.Id, new Relationship(relationInfo.SchemaName), relatedEntityReferences);
                connection.Associate(relatingEntity.LogicalName, relatingEntity.Id, new Relationship(relationInfo.SchemaName), relatedEntityReferences);
                output.WriteProgress("Importing", "Done!", completed: true);
            }
        }

        private static void AddRelationConditions(QueryExpression query, Entity relatingEntity, string schemaName, RelationImportRequest request)
        {
            foreach (var condition in request.RelationConditions.Where(r => r.SchemaName == schemaName))
            {
                if (!relatingEntity.Contains(condition.SourceField)) continue;

                var value = relatingEntity[condition.SourceField];
                if (value is EntityReference entityReference) value = entityReference.Id;

                query.Criteria.AddCondition(condition.TargetField, ConditionOperator.Equal, value);
            }
        }
    }
}
