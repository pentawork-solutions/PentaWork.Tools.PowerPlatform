using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;

namespace PentaWork.Tools.PowerPlatform.Common
{
    internal static class ServiceClientExtensions
    {
        public static List<Entity> QueryEntity(this ServiceClient client, string logicalName, bool getAll, params ConditionExpression[] conditions)
        {
            var query = new QueryExpression(logicalName);
            query.ColumnSet = new ColumnSet(true);
            conditions?.ToList().ForEach(query.Criteria.AddCondition);

            return client.Query(query, getAll);
        }

        public static List<Entity> Query(this ServiceClient client, QueryExpression query, bool getAll = false) =>
            QueryCore(query, getAll, client.RetrieveMultiple);

        /// <summary>The paging loop itself, independent of <see cref="ServiceClient"/> so it can be
        /// unit-tested with a fake page fetcher. Unlike the original, each subsequent page carries the
        /// previous response's PagingCookie forward - advancing PageNumber alone is not a reliable
        /// cursor once results shift between calls (Dataverse's paging contract expects the cookie).</summary>
        internal static List<Entity> QueryCore(QueryExpression query, bool getAll, Func<QueryExpression, EntityCollection> retrieveMultiple)
        {
            var entities = new List<Entity>();

            if (query.PageInfo == null) query.PageInfo = new PagingInfo { Count = 1000, PageNumber = 1 };
            var response = retrieveMultiple(query);

            entities.AddRange(response.Entities);
            if (getAll && response.MoreRecords)
            {
                while (response.MoreRecords)
                {
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = response.PagingCookie;
                    response = retrieveMultiple(query);
                    entities.AddRange(response.Entities);
                }
            }

            return entities;
        }

        public static Entity TryRetrieve(this ServiceClient client, string logicalName, Guid id, ColumnSet columns)
        {
            Entity entity = null;
            try
            {
                entity = client.Retrieve(logicalName, id, columns);
            }
            catch (FaultException) { /*No entity found*/ }
            return entity;
        }

        public static EntityMetadata GetMetadata(this ServiceClient client, string logicalName)
        {
            var request = new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                RetrieveAsIfPublished = true
            };
            return ((RetrieveEntityResponse)client.Execute(request)).EntityMetadata;
        }

        public static List<Entity> GetAll(this ServiceClient client, string logicalName, params string[] columns)
        {
            var query = new QueryExpression
            {
                EntityName = logicalName,
                ColumnSet = new ColumnSet(columns)
            };
            return client.Query(query, true);
        }

        public static List<Entity> GetEntitiesByName(this ServiceClient client, string logicalName, string nameField, string entityName)
        {
            var query = new QueryExpression
            {
                EntityName = logicalName,
                ColumnSet = new ColumnSet(true)
            };
            query.Criteria.AddCondition(nameField, ConditionOperator.Equal, entityName);
            return client.Query(query);
        }

        public static List<Entity> GetMatchingEntities(this ServiceClient client, string logicalName, Guid id, string name, string primaryNameField = null)
        {
            List<Entity> entities;
            if (!string.IsNullOrEmpty(primaryNameField))
            {
                entities = client.GetEntitiesByName(logicalName, primaryNameField, name);
            }
            else
            {
                var entity = client.TryRetrieve(logicalName, id, new ColumnSet());
                entities = entity != null
                    ? new List<Entity> { entity }
                    : new List<Entity>();
            }
            return entities;
        }

        public static void AddToSolution(this ServiceClient client, SolutionComponentType componentType, Guid componentId, string solutionUniqueName)
        {
            var addSolutionComponentRequest = new AddSolutionComponentRequest
            {
                ComponentType = (int)componentType,
                ComponentId = componentId,
                SolutionUniqueName = solutionUniqueName
            };
            client.Execute(addSolutionComponentRequest);
        }
    }
}
