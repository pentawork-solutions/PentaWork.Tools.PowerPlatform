using CliFx;
using CliFx.Infrastructure;
using Microsoft.PowerPlatform.Dataverse.Client;
using PentaWork.Tools.PowerPlatform.Commands;

namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// Shared glue between a CliFx command and an Operations call: resolves the connection,
    /// buffers results/warnings/verbose/progress through <see cref="ConsoleOperationOutput"/>,
    /// flushes the buffered result as one JSON document, and maps exceptions to the CLI's documented exit
    /// codes (0 success, 2 validation, 3 connection, 4 Dataverse/file, 5 partial batch failure).
    /// </summary>
    internal static class CommandRunner
    {
        /// <summary>Runs an operation that needs a live Dataverse connection.</summary>
        public static ValueTask RunWithConnection(DataverseCommand command, IConsole console, string? outputPath, Action<ServiceClient, IOperationOutput> action) =>
            Run(command.Verbose, console, outputPath, output =>
            {
                using var connection = ConnectionFactory.Create(command.ConnectionString, command.TimeoutMinutes);
                action(connection, output);
            });

        /// <summary>Runs an operation that needs no Dataverse connection (e.g. proxy/plugin-graph generation
        /// helpers that only touch the filesystem before the connection is established would still go
        /// through <see cref="RunWithConnection"/> - this overload is for the rare fully local case).</summary>
        public static ValueTask Run(bool verbose, IConsole console, string? outputPath, Action<IOperationOutput> action)
        {
            var output = new ConsoleOperationOutput(verbose, console.Error);
            try
            {
                action(output);
                var payload = output.Objects.Count switch
                {
                    0 => null,
                    1 => output.Objects[0],
                    _ => output.Objects
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload, JsonSettings.Default);
                if (string.IsNullOrEmpty(outputPath) || outputPath == ConsoleIO.StdStreamAlias)
                    console.Output.WriteLine(json);
                else
                    ConsoleIO.WriteOutput(outputPath, json);

                return default;
            }
            catch (CliException ex)
            {
                throw new CommandException(ex.Message, (int)ex.ExitCode, showHelp: false, ex);
            }
            catch (Exception ex)
            {
                if (verbose) console.Error.WriteLine(ex.ToString());
                throw new CommandException(ex.Message, (int)ExitCode.DataverseOrFile, showHelp: false, ex);
            }
        }
    }
}
