using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;

namespace PentaWork.Tools.PowerPlatform.Commands
{
    [Command("proxy generate", Description = "Generate proxy and fake classes from live Dataverse metadata.")]
    internal partial class ProxyGenerateCommand : DataverseCommand
    {
        public override ValueTask ExecuteAsync(IConsole console) =>
            RunWithConnectionAsync(console, null, (connection, output) =>
                ProxyOperations.Generate(connection, new ProxyOperations.ProxyGenerateRequest(ProxyNamespace, FakeNamespace, Solution, Include, Exclude, OutputDirectory, Clear, UseBaseProxy, CSharpOutputDirectory, FakeOutputDirectory, TypeScriptOutputDirectory), output));

        [CommandOption("proxy-namespace", Description = "Namespace for the generated C# proxy classes.")]
        public required string ProxyNamespace { get; set; }

        [CommandOption("fake-namespace", Description = "Namespace for the generated FakeXrmEasy fake classes.")]
        public required string FakeNamespace { get; set; }

        [CommandOption("solution", Description = "Only generate proxies for entities in this solution.")]
        public string? Solution { get; set; }

        [CommandOption("include", Description = "Entity logical name to include. Repeatable.")]
        public IReadOnlyList<string> Include { get; set; } = [];

        [CommandOption("exclude", Description = "Entity logical name to exclude. Repeatable.")]
        public IReadOnlyList<string> Exclude { get; set; } = [];

        [CommandOption("output-dir", Description = "Directory the proxies are written to.")]
        public required string OutputDirectory { get; set; }

        [CommandOption("clear", Description = "DESTRUCTIVE: delete --output-dir before generating.")]
        public bool Clear { get; set; }

        [CommandOption("use-base-proxy", Description = "Generate a shared BaseProxy.cs with change tracking.")]
        public bool UseBaseProxy { get; set; }

        [CommandOption("csharp-output-dir", Description = "Write C# proxy output directly to this directory.")]
        public string? CSharpOutputDirectory { get; set; }

        [CommandOption("fake-output-dir", Description = "Write FakeXrmEasy fake output directly to this directory.")]
        public string? FakeOutputDirectory { get; set; }

        [CommandOption("typescript-output-dir", Description = "Write TypeScript proxy output directly to this directory.")]
        public string? TypeScriptOutputDirectory { get; set; }
    }
}
