using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("assembly update", Description = "Update an existing plugin assembly's content/version in Dataverse.")]
    internal partial class AssemblyUpdateCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
                AssemblyOperations.Update(connection, new AssemblyOperations.AssemblyUpdateRequest(Dll), output));

        [CommandParameter(0, Name = "dll", Description = "Path to the plugin assembly (.dll) to update.")]
        public required string Dll { get; set; }
    }
}
