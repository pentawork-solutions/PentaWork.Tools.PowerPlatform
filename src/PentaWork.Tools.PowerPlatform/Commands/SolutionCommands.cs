using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("solution list", Description = "List all installed, visible solutions.")]
    internal partial class SolutionListCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, Output, (connection, output) => SolutionOperations.List(connection, output));

        [CommandOption("output", 'o', Description = "Write JSON to this file instead of stdout.")]
        public string? Output { get; set; }
    }
}
