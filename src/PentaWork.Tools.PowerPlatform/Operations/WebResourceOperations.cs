using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Web resource create/update operation backing the `webresource update` command.</summary>
    internal static class WebResourceOperations
    {
        internal record WebResourceUpdateRequest(string WebResourceFilePath, string Name, bool Publish, bool Create, string? Solution);
        internal record WebResourceUpdateResult(Guid? Id, string Name, bool Created, bool Published, string? SkipReason);

        public static WebResourceUpdateResult Update(ServiceClient connection, WebResourceUpdateRequest request, IOperationOutput output)
        {
            output.WriteVerbose($"Processing '{Path.GetFileName(request.WebResourceFilePath)}' ...");

            var webresourceQuery = new QueryExpression
            {
                EntityName = "webresource",
                ColumnSet = new ColumnSet(true)
            };
            webresourceQuery.Criteria.AddCondition("name", ConditionOperator.Equal, request.Name);

            var response = connection.Query(webresourceQuery, true);
            if (response.Count > 1)
            {
                var skipReason = $"More than one webresource with name '{request.Name}' found! Skipping!";
                output.WriteWarning(skipReason);
                var skippedResult = new WebResourceUpdateResult(null, request.Name, Created: false, Published: false, skipReason);
                output.WriteObject(skippedResult);
                return skippedResult;
            }
            if (response.Count == 0 && !request.Create)
            {
                var skipReason = $"No webresource with name '{request.Name}' found! Use --create, if necessary.";
                output.WriteWarning(skipReason);
                var skippedResult = new WebResourceUpdateResult(null, request.Name, Created: false, Published: false, skipReason);
                output.WriteObject(skippedResult);
                return skippedResult;
            }

            var isNew = response.Count == 0;
            var crmWebresource = isNew ? new Entity("webresource") : response[0];
            crmWebresource["content"] = Convert.ToBase64String(File.ReadAllBytes(request.WebResourceFilePath));

            if (isNew)
            {
                crmWebresource["name"] = request.Name;
                crmWebresource["displayname"] = request.Name;
                crmWebresource["webresourcetype"] = new OptionSetValue(3);

                output.WriteVerbose($"Creating webresource '{request.Name}' in Dataverse ...");
                crmWebresource.Id = connection.Create(crmWebresource);

                if (!string.IsNullOrEmpty(request.Solution))
                {
                    output.WriteVerbose($"Adding new webresource '{request.Name}' to {request.Solution} ...");
                    connection.AddToSolution(SolutionComponentType.WebResource, crmWebresource.Id, request.Solution);
                }
            }
            else
            {
                output.WriteVerbose($"Updating webresource '{request.Name}' in Dataverse ...");
                connection.Update(crmWebresource);
            }

            if (request.Publish)
            {
                output.WriteVerbose("Publishing webresource ...");
                connection.Execute(new PublishXmlRequest
                {
                    ParameterXml = $"<importexportxml><webresources><webresource>{crmWebresource.Id}</webresource></webresources></importexportxml>"
                });
            }
            output.WriteVerbose("Updating done!");

            var result = new WebResourceUpdateResult(crmWebresource.Id, request.Name, isNew, request.Publish, null);
            output.WriteObject(result);
            return result;
        }
    }
}
