using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using SolutionInfo = PentaWork.Tools.PowerPlatform.Dtos.SolutionInfo;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Solution list operation backing the `solution list` command.
    /// Solution export/import/unpack is intentionally left to the pac cli (`pac solution ...`).</summary>
    internal static class SolutionOperations
    {
        public static List<SolutionInfo> List(ServiceClient connection, IOperationOutput output)
        {
            var solutionQuery = new QueryExpression
            {
                EntityName = "solution",
                ColumnSet = new ColumnSet("publisherid", "installedon", "version", "versionnumber", "uniquename", "friendlyname", "isvisible")
            };
            solutionQuery.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);

            var solutions = connection
                .Query(solutionQuery, true)
                .Select(e => new SolutionInfo
                {
                    Id = e.Id,
                    Name = e.Attributes["friendlyname"].ToString()!,
                    UniqueName = e.Attributes["uniquename"].ToString()!,
                    Version = new Version(e.Attributes["version"].ToString()!),
                    Publisher = ((EntityReference)e.Attributes["publisherid"]).Name
                })
                .ToList();

            solutions.ForEach(output.WriteObject);
            return solutions;
        }
    }
}
