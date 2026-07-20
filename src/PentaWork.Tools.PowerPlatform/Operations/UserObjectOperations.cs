using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Per-user view/dashboard/chart listing operation backing the `user-object list` command.</summary>
    internal static class UserObjectOperations
    {
        public static List<UserObjectInfo> List(ServiceClient connection, IOperationOutput output)
        {
            output.WriteProgress("Retrieving", "Retrieving all users ...", 0);

            var query = new QueryExpression
            {
                EntityName = "systemuser",
                ColumnSet = new ColumnSet("fullname")
            };
            query.Criteria.AddCondition("isdisabled", ConditionOperator.Equal, false);
            var users = connection.Query(query, true);

            var processed = 0;
            var userObjects = new List<UserObjectInfo>();
            foreach (var user in users)
            {
                output.WriteProgress("Retrieving", $"Retrieving views for user '{user["fullname"]}' ...", 20 + 80 * processed++ / users.Count);
                userObjects.AddRange(GetUserObjects(connection, user));
            }
            output.WriteProgress("Retrieving", "Done!", completed: true);

            userObjects.ForEach(output.WriteObject);
            return userObjects;
        }

        private static List<UserObjectInfo> GetUserObjects(ServiceClient connection, Entity user)
        {
            var originalCaller = connection.CallerId;
            connection.CallerId = user.Id;

            var views = connection.GetAll("userquery", "name");
            var charts = connection.GetAll("userqueryvisualization", "name");
            var dashboards = connection.GetAll("userform", "name");

            connection.CallerId = originalCaller;
            return views.Select(v => new UserObjectInfo { UserId = user.Id, FullName = user["fullname"].ToString()!, ObjectType = "View", ObjectId = v.Id, ObjectName = v["name"].ToString()! })
                .Concat(charts.Select(c => new UserObjectInfo { UserId = user.Id, FullName = user["fullname"].ToString()!, ObjectType = "Chart", ObjectId = c.Id, ObjectName = c["name"].ToString()! }))
                .Concat(dashboards.Select(d => new UserObjectInfo { UserId = user.Id, FullName = user["fullname"].ToString()!, ObjectType = "Dashboard", ObjectId = d.Id, ObjectName = d["name"].ToString()! }))
                .ToList();
        }
    }
}
