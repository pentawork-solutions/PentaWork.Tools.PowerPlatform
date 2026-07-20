using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// Resolves the Dataverse connection string (--connection-string, falling back to
    /// PW_PP_CONNECTION_STRING) and creates a ready-to-use <see cref="ServiceClient"/>, applying the
    /// global --timeout-minutes option.
    /// </summary>
    internal static class ConnectionFactory
    {
        public const string ConnectionStringEnvironmentVariable = "PW_PP_CONNECTION_STRING";

        public static string ResolveConnectionString(string? connectionStringOption)
        {
            var connectionString = !string.IsNullOrWhiteSpace(connectionStringOption)
                ? connectionStringOption
                : Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new CliValidationException(
                    $"No connection string provided. Pass --connection-string or set the {ConnectionStringEnvironmentVariable} environment variable.");
            }

            return connectionString;
        }

        public static ServiceClient Create(string? connectionStringOption, int timeoutMinutes)
        {
            var connectionString = ResolveConnectionString(connectionStringOption);

            if (timeoutMinutes <= 0)
                throw new CliValidationException("--timeout-minutes must be a positive number of minutes.");

            ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(timeoutMinutes);

            ServiceClient client;
            try
            {
                client = new ServiceClient(connectionString, NullLogger.Instance);
            }
            catch (Exception ex)
            {
                throw new CliConnectionException($"Failed to connect to Dataverse: {ex.Message}", ex);
            }

            if (!client.IsReady)
            {
                var lastError = client.LastError;
                client.Dispose();
                throw new CliConnectionException($"Failed to connect to Dataverse: {lastError}");
            }

            return client;
        }

        /// <summary>Renders a connection string safe for logging by redacting credential-bearing segments.</summary>
        public static string Redact(string connectionString)
        {
            var secretKeys = new[] { "Password", "ClientSecret", "Secret", "Certificate", "CertificatePassword" };
            var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            var redacted = segments.Select(segment =>
            {
                var separatorIndex = segment.IndexOf('=');
                if (separatorIndex < 0) return segment;

                var key = segment[..separatorIndex].Trim();
                var isSecret = secretKeys.Any(secretKey => string.Equals(key, secretKey, StringComparison.OrdinalIgnoreCase));
                return isSecret ? $"{key}=***" : segment;
            });

            return string.Join(";", redacted);
        }
    }
}
