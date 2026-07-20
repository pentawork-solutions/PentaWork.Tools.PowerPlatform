using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("user-object list", Description = "List all user views, dashboards and charts.")]
    internal partial class UserObjectListCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, Output, (connection, output) => UserObjectOperations.List(connection, output));

        [CommandOption("output", 'o', Description = "Write JSON to this file instead of stdout.")]
        public string? Output { get; set; }
    }
}
