using System.Numerics;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Entity export/import/remove operations backing the `entity export|import|remove` commands.</summary>
    internal static class EntityOperations
    {
        internal record EntityExportRequest(
            string EntityName,
            IReadOnlyList<string> Columns,
            IReadOnlyList<ConditionExpression> FilterConditions,
            IReadOnlyList<string> IgnoreColumns,
            IReadOnlyList<string> Relations,
            int PageSize,
            int PageNumber,
            bool GetAll,
            bool Sharings,
            bool ValidForCreate,
            bool ValidForUpdate);

        internal record EntityImportRequest(
            EntityData EntityData,
            EntityReference FallbackOwner,
            bool MapByName,
            bool TakeFirst,
            bool ImpersonateOwner,
            IReadOnlyList<string> MapByNameLookups,
            IReadOnlyList<string> CreateOnly);

        internal record EntityRemoveRequest(string EntityName, IReadOnlyList<ConditionExpression> FilterConditions);
        internal record EntityImportResult(int Created, int Updated, int Processed);
        internal record EntityRemoveResult(int DeletedCount, int FailedCount);

        #region Export

        public static EntityData Export(ServiceClient connection, EntityExportRequest request, IOperationOutput output)
        {
            var fetchedMetaData = new List<EntityMetadata>();

            var entities = GetEntities(connection, request);
            var metadata = connection.GetMetadata(request.EntityName);
            var relevantAttributes = GetRelevantAttributes(metadata, request);
            return GetEntityData(connection, entities, metadata, relevantAttributes, request, fetchedMetaData, output);
        }

        private static List<Entity> GetEntities(ServiceClient connection, EntityExportRequest request)
        {
            var query = new QueryExpression
            {
                EntityName = request.EntityName,
                ColumnSet = request.Columns.Count > 0 ? new ColumnSet(request.Columns.ToArray()) : new ColumnSet(true),
                PageInfo = new PagingInfo { Count = request.PageSize, PageNumber = request.PageNumber }
            };
            foreach (var condition in request.FilterConditions) query.Criteria.AddCondition(condition);
            return connection.Query(query, request.GetAll);
        }

        private static List<AttributeMetadata> GetRelevantAttributes(EntityMetadata metadata, EntityExportRequest request)
        {
            bool Filter(AttributeMetadata attr)
            {
                var relevant = false;
                if (request.ValidForCreate) relevant = attr.IsValidForCreate == true;
                if (request.ValidForUpdate) relevant = relevant || attr.IsValidForUpdate == true;
                if (!request.ValidForCreate && !request.ValidForUpdate) relevant = attr.IsValidForUpdate == true || attr.IsValidForCreate == true;
                return relevant && !request.IgnoreColumns.Contains(attr.LogicalName);
            }

            return metadata.Attributes.Where(Filter).ToList();
        }

        private static EntityData GetEntityData(ServiceClient connection, List<Entity> entities, EntityMetadata metadata,
            List<AttributeMetadata> relevantAttributes, EntityExportRequest request, List<EntityMetadata> fetchedMetaData, IOperationOutput output)
        {
            var exportInfo = new EntityData
            {
                EntityName = request.EntityName,
                PrimaryIdField = metadata.PrimaryIdAttribute,
                PrimaryNameField = metadata.PrimaryNameAttribute,
                ExportedFrom = connection.ConnectedOrgUniqueName,
                ExportedOn = DateTime.UtcNow
            };

            if (metadata.PrimaryNameAttribute != null) entities = entities.OrderBy(e => e[metadata.PrimaryNameAttribute].ToString()).ToList();

            var entityInfos = new List<EntityInfo>();
            foreach (var entity in entities)
            {
                var entityInfo = new EntityInfo
                {
                    Id = metadata.PrimaryIdAttribute != null && entity.Attributes.Contains(metadata.PrimaryIdAttribute) ? (Guid)entity.Attributes[metadata.PrimaryIdAttribute] : Guid.Empty,
                    Name = metadata.PrimaryNameAttribute != null && entity.Attributes.Contains(metadata.PrimaryNameAttribute) ? entity.Attributes[metadata.PrimaryNameAttribute].ToString()! : string.Empty
                };
                entityInfo.Attributes = GetAttributes(connection, entity, relevantAttributes, fetchedMetaData);
                entityInfo.Sharings = GetSharings(connection, entity, request, output);
                entityInfo.Relations = GetRelations(connection, entity, metadata, request, output);

                entityInfos.Add(entityInfo);
            }
            exportInfo.Entities = entityInfos.ToArray();
            return exportInfo;
        }

        private static AttributeInfo[] GetAttributes(ServiceClient connection, Entity entity, List<AttributeMetadata> relevantAttributes, List<EntityMetadata> fetchedMetaData)
        {
            var attributeInfos = new List<AttributeInfo>();
            foreach (var attr in entity.Attributes.OrderBy(a => a.Key))
            {
                var attrMetadata = relevantAttributes.SingleOrDefault(ra => ra.LogicalName == attr.Key);
                if (attrMetadata == null) continue;

                attributeInfos.Add(new AttributeInfo
                {
                    Name = attr.Key,
                    ValidForCreate = attrMetadata.IsValidForCreate == true,
                    ValidForUpdate = attrMetadata.IsValidForUpdate == true,
                    Type = attrMetadata.AttributeType,
                    Value = SerializeValue(attrMetadata.AttributeType, attr.Value, fetchedMetaData, connection)
                });
            }
            return attributeInfos.ToArray();
        }

        private static ShareInfo[] GetSharings(ServiceClient connection, Entity entity, EntityExportRequest request, IOperationOutput output)
        {
            if (!request.Sharings) return [];

            var shares = new List<ShareInfo>();
            var shareRequest = new RetrieveSharedPrincipalsAndAccessRequest { Target = entity.ToEntityReference() };
            var response = (RetrieveSharedPrincipalsAndAccessResponse)connection.Execute(shareRequest);
            foreach (var share in response.PrincipalAccesses.OrderBy(p => p.Principal.Name))
            {
                var primaryNameField = share.Principal.LogicalName == "systemuser" ? "fullname" : "name"; // Teams
                var principal = connection.Retrieve(share.Principal.LogicalName, share.Principal.Id, new ColumnSet(primaryNameField));
                var principalName = principal.Contains(primaryNameField) ? principal[primaryNameField].ToString()! : string.Empty;

                shares.Add(new ShareInfo
                {
                    Id = share.Principal.Id,
                    Name = principalName,
                    LogicalName = share.Principal.LogicalName,
                    AccessMask = share.AccessMask
                });
            }
            return shares.ToArray();
        }

        private static RelationInfo[] GetRelations(ServiceClient connection, Entity entity, EntityMetadata entityMetadata, EntityExportRequest request, IOperationOutput output)
        {
            if (request.Relations.Count == 0) return [];

            var relations = new List<RelationInfo>();
            foreach (var relation in request.Relations)
            {
                var schemaDefinition = entityMetadata.ManyToManyRelationships.SingleOrDefault(r => r.SchemaName == relation);
                if (schemaDefinition == null)
                {
                    output.WriteWarning($"No relation definition matching the schema name '{relation}' was found!");
                    continue;
                }

                var fromEntityIdField = schemaDefinition.Entity1LogicalName == entity.LogicalName ? schemaDefinition.Entity1IntersectAttribute : schemaDefinition.Entity2IntersectAttribute;
                var toEntityLogicalName = schemaDefinition.Entity1LogicalName == entity.LogicalName ? schemaDefinition.Entity2LogicalName : schemaDefinition.Entity1LogicalName;
                var toEntityIdField = schemaDefinition.Entity1LogicalName == entity.LogicalName ? schemaDefinition.Entity2IntersectAttribute : schemaDefinition.Entity1IntersectAttribute;
                var toEntityMetaData = connection.GetMetadata(toEntityLogicalName);

                var relationQuery = new QueryExpression
                {
                    EntityName = schemaDefinition.IntersectEntityName,
                    ColumnSet = new ColumnSet(true)
                };
                relationQuery.Criteria.AddCondition(fromEntityIdField, ConditionOperator.Equal, entity.Id);
                var relationEntities = connection.Query(relationQuery, true);
                if (relationEntities.Count == 0) continue;

                var relatedEntitiesQuery = new QueryExpression
                {
                    EntityName = toEntityLogicalName,
                    ColumnSet = new ColumnSet(toEntityMetaData.PrimaryNameAttribute)
                };
                var condition = new ConditionExpression(toEntityMetaData.PrimaryIdAttribute, ConditionOperator.In, relationEntities.Select(r => (Guid)r[toEntityIdField]).Cast<object>().ToArray());
                relatedEntitiesQuery.Criteria.AddCondition(condition);

                var relatedEntities = connection.Query(relatedEntitiesQuery, true);
                var relationInfo = new RelationInfo
                {
                    SchemaName = schemaDefinition.SchemaName,
                    IntersectEntityName = schemaDefinition.IntersectEntityName,
                    ToLogicalName = toEntityMetaData.LogicalName,
                    ToPrimaryNameAttribute = toEntityMetaData.PrimaryNameAttribute,
                    ToPrimaryIdAttribute = toEntityMetaData.PrimaryIdAttribute,
                    FromPrimaryIdAttribute = fromEntityIdField,
                    Entities = relatedEntities
                        .OrderBy(r => r.Contains(toEntityMetaData.PrimaryNameAttribute) ? r[toEntityMetaData.PrimaryNameAttribute].ToString() : r.Id.ToString())
                        .Select(r => new RelatedEntityInfo
                        {
                            Id = r.Id,
                            Name = r.Contains(toEntityMetaData.PrimaryNameAttribute) ? r[toEntityMetaData.PrimaryNameAttribute].ToString()! : string.Empty
                        })
                        .ToArray()
                };
                relations.Add(relationInfo);
            }
            return relations.ToArray();
        }

        private static string SerializeValue(AttributeTypeCode? type, object value, List<EntityMetadata> fetchedMetaData, ServiceClient connectionForLookup)
        {
            switch (type)
            {
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Picklist:
                    return ((OptionSetValue)value).Value.ToString();
                case AttributeTypeCode.DateTime:
                case AttributeTypeCode.Boolean:
                case AttributeTypeCode.Double:
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.BigInt:
                case AttributeTypeCode.Decimal:
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.Memo:
                case AttributeTypeCode.String:
                case AttributeTypeCode.EntityName:
                    return value.ToString()!;
                case AttributeTypeCode.Money:
                    return ((Money)value).Value.ToString();
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    var entityRef = (EntityReference)value;
                    if (!fetchedMetaData.Any(m => m.LogicalName == entityRef.LogicalName))
                        fetchedMetaData.Add(connectionForLookup.GetMetadata(entityRef.LogicalName));

                    var metadata = fetchedMetaData.Single(m => m.LogicalName == entityRef.LogicalName);
                    return $"{entityRef.LogicalName};{entityRef.Id};{entityRef.Name};{metadata.PrimaryNameAttribute}";
                case AttributeTypeCode.Virtual:
                    if (value is OptionSetValue osValue) return $"virt-os;{osValue.Value}";
                    if (value is OptionSetValueCollection osValues) return $"virt-osc;{string.Join(",", osValues.Select(o => o.Value.ToString()))}";
                    throw new CliDataException($"Can't serialize attribute type with value type: {type} / {value.GetType()}");
                default:
                    throw new CliDataException($"Can't serialize attribute type: {type}");
            }
        }

        #endregion

        #region Import

        public static EntityImportResult Import(ServiceClient connection, EntityImportRequest request, IOperationOutput output)
        {
            var fallbackOwner = connection.TryRetrieve(request.FallbackOwner.LogicalName, request.FallbackOwner.Id, new ColumnSet(true));
            if (fallbackOwner == null) throw new CliDataException("Could not find fallback owner in connected system!");
            if (fallbackOwner.Attributes.Contains("isdisabled") && (bool?)fallbackOwner.Attributes["isdisabled"] == true)
                throw new CliDataException("Given fallback user is disabled in connected system!");

            // Set the fallback owner name for logging
            var fallbackOwnerRef = request.FallbackOwner;
            fallbackOwnerRef.Name = fallbackOwner.Contains("fullname") ? fallbackOwner.Attributes["fullname"].ToString()! : fallbackOwner.Id.ToString();
            fallbackOwnerRef.Name = fallbackOwnerRef.Name == string.Empty && fallbackOwner.Contains("name") ? fallbackOwner.Attributes["name"].ToString()! : fallbackOwnerRef.Name;

            var created = 0;
            var updated = 0;
            var processed = 0;
            var metadata = connection.GetMetadata(request.EntityData.EntityName);

            foreach (var entityInfo in request.EntityData.Entities)
            {
                output.WriteProgress("Importing", $"Importing entity '{entityInfo.Name}' ...", 100 * processed++ / request.EntityData.Entities.Length);
                try
                {
                    var matchingSystemEntities = connection.GetMatchingEntities(request.EntityData.EntityName, entityInfo.Id, entityInfo.Name, request.MapByName ? request.EntityData.PrimaryNameField : null);
                    var systemEntity = matchingSystemEntities.FirstOrDefault();
                    var isUpdate = systemEntity != null;

                    if (request.CreateOnly.Contains(entityInfo.Name) && isUpdate) { output.WriteVerbose($"Skipping update of '{entityInfo.Name}' - Create Only ..."); continue; }
                    if (matchingSystemEntities.Count > 1) output.WriteVerbose($"Found multiple matches for '{entityInfo.Name}' ...");
                    if (matchingSystemEntities.Count > 1 && !request.TakeFirst) { output.WriteVerbose("Skipping ..."); continue; }

                    systemEntity ??= new Entity(request.EntityData.EntityName);
                    SetAttributes(connection, systemEntity, entityInfo.Attributes, isUpdate, request);
                    var ownerRef = SetOwner(connection, systemEntity, metadata, fallbackOwnerRef);
                    ExecuteChange(connection, systemEntity, isUpdate, request.ImpersonateOwner);

                    output.WriteVerbose($"{(isUpdate ? "UPDATED" : "CREATED")}: {entityInfo.Name} {(!string.IsNullOrEmpty(ownerRef?.Name) ? $"[Owner: {ownerRef.Name}]" : "")}");
                    created += isUpdate ? 0 : 1;
                    updated += isUpdate ? 1 : 0;
                }
                catch (Exception ex)
                {
                    output.WriteWarning($"Import of '{entityInfo.Name}' failed: {ex.Message}");
                }
            }
            output.WriteProgress("Importing", "Done!", completed: true);
            output.WriteVerbose($"Created: {created} Updated: {updated}");

            var result = new EntityImportResult(created, updated, processed);
            output.WriteObject(result);
            return result;
        }

        private static void SetAttributes(ServiceClient connection, Entity systemEntity, AttributeInfo[] attributes, bool isUpdate, EntityImportRequest request)
        {
            foreach (var attr in attributes)
            {
                // Skip Id Attribute on Update
                if (isUpdate && attr.Name == request.EntityData.PrimaryIdField) continue;
                // Skip Id Attribute, if mapped by name. Otherwise it could be a problem for certain entities like userqueries for example.
                // If a user query gets created, and reassigned, the module wont find the view and would try to
                // create a entity with an entity already present in the system.
                if (request.MapByName && attr.Name == request.EntityData.PrimaryIdField) continue;
                systemEntity[attr.Name] = DeserializeValue(connection, attr, request);
            }
        }

        private static EntityReference? SetOwner(ServiceClient connection, Entity entity, EntityMetadata metadata, EntityReference fallbackOwner)
        {
            // Some entities like themes and currencies dont have an owner!
            if (!metadata.Attributes.Any(a => a.LogicalName == "ownerid")) return null;

            var owner = fallbackOwner;
            // New entities dont have an ownerid set, if the importing data is not containing one.
            if (entity.Contains("ownerid"))
            {
                owner = (EntityReference)entity["ownerid"];
                var systemOwner = connection.GetEntitiesByName(owner.LogicalName, owner.LogicalName == "systemuser" ? "fullname" : "name", owner.Name);

                // Check, if the given owner exists in the target system
                if (systemOwner.Count != 0) owner = systemOwner.First().ToEntityReference();
            }

            entity["ownerid"] = owner;
            return owner;
        }

        private static void ExecuteChange(ServiceClient connection, Entity entity, bool isUpdate, bool impersonateOwner)
        {
            var originalCaller = connection.CallerId;
            if (impersonateOwner && entity.Attributes.Contains("ownerid")) connection.CallerId = ((EntityReference)entity["ownerid"]).Id;

            if (isUpdate) connection.Update(entity);
            else connection.Create(entity);

            if (impersonateOwner) connection.CallerId = originalCaller;
        }

        private static object DeserializeValue(ServiceClient connection, AttributeInfo attr, EntityImportRequest request)
        {
            switch (attr.Type)
            {
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Picklist:
                    return new OptionSetValue(int.Parse(attr.Value));
                case AttributeTypeCode.DateTime:
                    return DateTime.Parse(attr.Value);
                case AttributeTypeCode.Boolean:
                    return bool.Parse(attr.Value);
                case AttributeTypeCode.Money:
                    return new Money(decimal.Parse(attr.Value));
                case AttributeTypeCode.Decimal:
                    return decimal.Parse(attr.Value);
                case AttributeTypeCode.Double:
                    return double.Parse(attr.Value);
                case AttributeTypeCode.Integer:
                    return int.Parse(attr.Value);
                case AttributeTypeCode.BigInt:
                    return BigInteger.Parse(attr.Value);
                case AttributeTypeCode.Uniqueidentifier:
                    return new Guid(attr.Value);
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    {
                        var entityRef = attr.Value.Split([';'], StringSplitOptions.RemoveEmptyEntries);
                        object deserializedValue = new EntityReference(entityRef[0], new Guid(entityRef[1])) { Name = entityRef[2] };
                        if (request.MapByNameLookups.Contains(attr.Name))
                        {
                            var entities = connection.GetEntitiesByName(entityRef[0], entityRef[3], entityRef[2]);
                            deserializedValue = entities.FirstOrDefault()?.ToEntityReference()
                                ?? throw new CliDataException($"No matching '{entityRef[0]}' for name '{entityRef[2]}' found!");
                        }
                        return deserializedValue;
                    }
                case AttributeTypeCode.Memo:
                case AttributeTypeCode.String:
                case AttributeTypeCode.EntityName:
                    return attr.Value;
                case AttributeTypeCode.Virtual:
                    if (attr.Value.StartsWith("virt-os;"))
                        return new OptionSetValue(int.Parse(attr.Value.Split(';')[1]));
                    if (attr.Value.StartsWith("virt-osc;"))
                    {
                        var splitted = attr.Value.Split(';');
                        var optionValues = splitted[1].Split(',').Select(o => new OptionSetValue(int.Parse(o))).ToList();
                        return new OptionSetValueCollection(optionValues);
                    }
                    throw new CliDataException($"Can't deserialize attribute type with value type: {attr.Type}");
                default:
                    throw new CliDataException($"Can't deserialize attribute type: {attr.Type}");
            }
        }

        #endregion

        #region Remove

        public static EntityRemoveResult Remove(ServiceClient connection, EntityRemoveRequest request, IOperationOutput output)
        {
            output.WriteVerbose("Getting entities ...");
            var query = new QueryExpression
            {
                EntityName = request.EntityName,
                ColumnSet = new ColumnSet()
            };
            foreach (var condition in request.FilterConditions) query.Criteria.AddCondition(condition);
            var entities = connection.Query(query, true);

            output.WriteVerbose($"Deleting {entities.Count} entities ...");

            var deleteRequests = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings { ContinueOnError = true, ReturnResponses = true },
                Requests = []
            };

            entities.ForEach(e => deleteRequests.Requests.Add(new DeleteRequest { Target = e.ToEntityReference() }));
            var response = (ExecuteMultipleResponse)connection.Execute(deleteRequests);

            var failedCount = 0;
            foreach (var responseItem in response.Responses)
            {
                if (responseItem.Fault == null) continue;

                failedCount++;
                var deleteRequest = (DeleteRequest)deleteRequests.Requests[responseItem.RequestIndex];
                output.WriteWarning($"{deleteRequest.Target.Name} ({deleteRequest.Target.Id}): {responseItem.Fault.Message}");
            }

            var result = new EntityRemoveResult(entities.Count - failedCount, failedCount);
            output.WriteObject(result);
            return result;
        }

        #endregion
    }
}
