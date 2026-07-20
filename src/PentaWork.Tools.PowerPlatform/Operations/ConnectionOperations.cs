using Microsoft.PowerPlatform.Dataverse.Client;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Verifies a connection string resolves and authenticates successfully - gives users a fast
    /// way to validate --connection-string / PW_PP_CONNECTION_STRING.</summary>
    internal static class ConnectionOperations
    {
        internal record ConnectionTestResult(
            string OrganizationFriendlyName,
            string OrganizationUniqueName,
            Guid OrganizationId,
            string? OrganizationVersion,
            Guid UserId);

        public static ConnectionTestResult Test(ServiceClient connection, IOperationOutput output)
        {
            var result = new ConnectionTestResult(
                connection.ConnectedOrgFriendlyName,
                connection.ConnectedOrgUniqueName,
                connection.ConnectedOrgId,
                connection.ConnectedOrgVersion?.ToString(),
                connection.CallerId);

            output.WriteVerbose($"Connected to '{result.OrganizationFriendlyName}' ({result.OrganizationUniqueName}) as {result.UserId}.");
            output.WriteObject(result);
            return result;
        }
    }
}
