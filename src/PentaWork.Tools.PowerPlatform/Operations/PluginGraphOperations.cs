using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PluginGraph;
using PentaWork.Tools.PluginGraph.Model.GraphObjects;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Plugin call-graph analysis operation backing the `plugin-graph export` command. --solution
    /// is optional - the whole connected system is analyzed when it's omitted, see the corresponding
    /// `Guid?` widening in PluginGraphAnalyzer.AnalyzeSystem.</summary>
    internal static class PluginGraphOperations
    {
        internal record PluginGraphExportRequest(string? SolutionUniqueName, string Namespaces, string OutputPath, bool Clear, bool Log);
        internal record PluginGraphExportResult(string OutputPath, int EntityCount);

        public static PluginGraphExportResult Export(ServiceClient connection, PluginGraphExportRequest request, IOperationOutput output)
        {
            if (request.Clear)
            {
                ClearGuard.EnsureSafeToClear(request.OutputPath);
                if (Directory.Exists(request.OutputPath)) Directory.Delete(request.OutputPath, true);
            }
            if (!Directory.Exists(request.OutputPath)) Directory.CreateDirectory(request.OutputPath);

            var pluginGraphAnalyzer = new PluginGraphAnalyzer();
            pluginGraphAnalyzer.Progress += (progress, total, name) =>
            {
                if (progress == total) output.WriteProgress("Analyzing", "Done!", completed: true);
                else output.WriteProgress("Analyzing", $"Analyzing plugin step '{name}' ...", progress * 100 / total);
            };
            pluginGraphAnalyzer.Warning += output.WriteWarning;

            var solutionId = ResolveSolutionId(connection, request.SolutionUniqueName);

            output.WriteVerbose("Loading plugin steps and analyzing...");
            var entityGraphList = pluginGraphAnalyzer.AnalyzeSystem(connection, solutionId, request.Namespaces, request.Log);

            output.WriteVerbose("Writing markdown files....");
            entityGraphList.ForEach(e => File.WriteAllText(Path.Combine(request.OutputPath, $"{e.EntityName}.md"), e.ToMarkdown()));
            File.WriteAllText(Path.Combine(request.OutputPath, "0_TOC.md"), new SystemOverview(entityGraphList).ToMarkdown());

            var result = new PluginGraphExportResult(request.OutputPath, entityGraphList.Count);
            output.WriteObject(result);
            return result;
        }

        private static Guid? ResolveSolutionId(ServiceClient connection, string? solutionUniqueName)
        {
            if (string.IsNullOrEmpty(solutionUniqueName)) return null;

            var query = new QueryExpression("solution") { ColumnSet = new ColumnSet("uniquename") };
            query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

            return connection.Query(query, true).SingleOrDefault()?.Id
                ?? throw new CliDataException($"Solution with unique name '{solutionUniqueName}' not found!");
        }
    }
}
