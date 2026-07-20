using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using Microsoft.PowerPlatform.Dataverse.Client;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    /// <summary>Common connection and diagnostic options shared by every Dataverse command.</summary>
    internal abstract partial class DataverseCommand : ICommand
    {
        protected ValueTask RunWithConnectionAsync(IConsole console, string? outputPath, Action<ServiceClient, IOperationOutput> action) =>
            CommandRunner.RunWithConnection(this, console, outputPath, action);

        public abstract ValueTask ExecuteAsync(IConsole console);

        [CommandOption("connection-string", Description = "Dataverse connection string.", EnvironmentVariable = ConnectionFactory.ConnectionStringEnvironmentVariable)]
        public string? ConnectionString { get; set; }

        [CommandOption("timeout-minutes", Description = "Connection timeout in minutes.")]
        public int TimeoutMinutes { get; set; } = 4;

        [CommandOption("verbose", Description = "Write verbose diagnostic logs to stderr.")]
        public bool Verbose { get; set; }
    }
}
