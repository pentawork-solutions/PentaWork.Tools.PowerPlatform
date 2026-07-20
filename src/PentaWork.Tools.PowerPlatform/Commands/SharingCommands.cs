using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("sharing import", Description = "Import entity sharings from JSON.")]
    internal partial class SharingImportCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
            {
                var request = new SharingOperations.SharingImportRequest(ConsoleIO.ReadInputJson<EntityData>(Input), MapEntitiesByName, MapPrincipalsByName);
                SharingOperations.Import(connection, request, output);
            });

        [CommandOption("input", 'i', Description = "Read entity and sharing JSON from this file instead of stdin.")]
        public string? Input { get; set; }

        [CommandOption("map-entities-by-name", Description = "Match the shared entity by primary name instead of id.")]
        public bool MapEntitiesByName { get; set; }

        [CommandOption("map-principals-by-name", Description = "Match principals by name instead of id.")]
        public bool MapPrincipalsByName { get; set; }
    }
}
