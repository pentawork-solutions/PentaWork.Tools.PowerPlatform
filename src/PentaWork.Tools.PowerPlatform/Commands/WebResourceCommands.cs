using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("webresource update", Description = "Update an existing or new web resource's content.")]
    internal partial class WebResourceUpdateCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
                WebResourceOperations.Update(connection, new WebResourceOperations.WebResourceUpdateRequest(File, Name, Publish, Create, Solution), output));

        [CommandParameter(0, Name = "file", Description = "Path to the web resource file to upload.")]
        public required string File { get; set; }

        [CommandOption("name", Description = "Logical name of the web resource in Dataverse.")]
        public required string Name { get; set; }

        [CommandOption("publish", Description = "Publish the web resource after updating.")]
        public bool Publish { get; set; }

        [CommandOption("create", Description = "Create the web resource if it doesn't exist yet.")]
        public bool Create { get; set; }

        [CommandOption("solution", Description = "Add a newly created web resource to this solution.")]
        public string? Solution { get; set; }
    }
}
