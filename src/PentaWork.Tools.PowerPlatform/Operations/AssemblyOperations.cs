using System.Reflection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Plugin assembly update operation backing the `assembly update` command. Reads assembly
    /// metadata cross-platform and without loading the assembly via <see cref="AssemblyName.GetAssemblyName"/>.</summary>
    internal static class AssemblyOperations
    {
        internal record AssemblyUpdateRequest(string AssemblyFilePath);
        internal record AssemblyUpdateResult(string AssemblyName, string Version, bool Updated, string? SkipReason);

        public static AssemblyUpdateResult Update(ServiceClient connection, AssemblyUpdateRequest request, IOperationOutput output)
        {
            var assemblyName = AssemblyName.GetAssemblyName(request.AssemblyFilePath);
            var shortName = assemblyName.Name ?? throw new CliValidationException($"'{request.AssemblyFilePath}' has no assembly name.");
            var version = (assemblyName.Version ?? new Version(0, 0, 0, 0)).ToString();
            var culture = string.IsNullOrEmpty(assemblyName.CultureName) ? "neutral" : assemblyName.CultureName;
            var publicKeyTokenBytes = assemblyName.GetPublicKeyToken();
            var publicKeyToken = publicKeyTokenBytes is { Length: > 0 } ? Convert.ToHexString(publicKeyTokenBytes).ToLowerInvariant() : "null";

            output.WriteVerbose($"Processing assembly '{shortName}' ...");

            var assemblyQuery = new QueryExpression
            {
                EntityName = "pluginassembly",
                ColumnSet = new ColumnSet(true)
            };
            assemblyQuery.Criteria.AddCondition("name", ConditionOperator.Equal, shortName);

            var response = connection.Query(assemblyQuery, true);
            if (response.Count > 1)
            {
                var skipReason = $"More than one assembly with name '{shortName}' found! Skipping!";
                output.WriteWarning(skipReason);
                var skippedResult = new AssemblyUpdateResult(shortName, version, Updated: false, skipReason);
                output.WriteObject(skippedResult);
                return skippedResult;
            }
            if (response.Count == 0)
            {
                throw new CliDataException($"No plugin assembly with name '{shortName}' found in connected system!");
            }

            var crmAssembly = response[0];
            crmAssembly["version"] = version;
            crmAssembly["culture"] = culture;
            crmAssembly["publickeytoken"] = publicKeyToken;
            crmAssembly["content"] = Convert.ToBase64String(File.ReadAllBytes(request.AssemblyFilePath));

            output.WriteVerbose($"Publishing assembly '{shortName}' to Dataverse ...");
            connection.Update(crmAssembly);
            output.WriteVerbose("Publishing done!");

            var result = new AssemblyUpdateResult(shortName, version, Updated: true, null);
            output.WriteObject(result);
            return result;
        }
    }
}
