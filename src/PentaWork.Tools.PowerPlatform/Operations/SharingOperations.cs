using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Entity sharing (POA grant) import operation backing the `sharing import` command.</summary>
    internal static class SharingOperations
    {
        internal record SharingImportRequest(EntityData EntityData, bool MapEntitiesByName, bool MapPrincipalsByName);

        public static void Import(ServiceClient connection, SharingImportRequest request, IOperationOutput output)
        {
            var principals = new List<Entity>();

            var processedEntities = 0;
            foreach (var entityInfo in request.EntityData.Entities)
            {
                if (entityInfo.Sharings.Length == 0) continue;

                output.WriteProgress("Importing", $"Importing shares for '{entityInfo.Name}' ...", 100 * processedEntities++ / request.EntityData.Entities.Length);

                var matchingSystemEntities = connection.GetMatchingEntities(request.EntityData.EntityName, entityInfo.Id, entityInfo.Name, request.MapEntitiesByName ? request.EntityData.PrimaryNameField : null);
                if (matchingSystemEntities.Count > 1) output.WriteVerbose($"Multiple entities for '{entityInfo.Name}' found ...");
                if (matchingSystemEntities.Count == 0)
                {
                    output.WriteWarning($"Entity '{entityInfo.Name}' not found!");
                    continue;
                }

                var processedSharings = 0;
                var entity = matchingSystemEntities.First();
                foreach (var share in entityInfo.Sharings)
                {
                    output.WriteProgress("Importing", $"Importing share '{share.Name}' ...", 100 * processedSharings++ / entityInfo.Sharings.Length);

                    var principal = GetPrincipal(connection, principals, share, request);
                    if (principal == null)
                    {
                        output.WriteWarning($"Principal '{share.Name}' not found!");
                        continue;
                    }

                    var accessRights = new PrincipalAccess { Principal = principal.ToEntityReference(), AccessMask = share.AccessMask };
                    var grantRequest = new GrantAccessRequest { Target = entity.ToEntityReference(), PrincipalAccess = accessRights };

                    connection.Execute(grantRequest);
                }
            }
            output.WriteProgress("Importing", "Done!", completed: true);
        }

        private static Entity? GetPrincipal(ServiceClient connection, List<Entity> principals, ShareInfo share, SharingImportRequest request)
        {
            var nameField = share.LogicalName == "systemuser" ? "fullname" : "name";
            var principal = request.MapPrincipalsByName
                ? principals.SingleOrDefault(p => p[nameField].ToString() == share.Name)
                : principals.SingleOrDefault(p => p.Id == share.Id);

            if (principal == null)
            {
                var principalCandidates = connection.GetMatchingEntities(share.LogicalName, share.Id, share.Name, request.MapPrincipalsByName ? nameField : null);
                principal = principalCandidates.FirstOrDefault();
                if (principal != null) principals.Add(principal);
            }

            return principal;
        }
    }
}
