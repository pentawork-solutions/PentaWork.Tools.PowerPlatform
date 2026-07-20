using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using SolutionInfo = PentaWork.Tools.PowerPlatform.Dtos.SolutionInfo;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Solution list/export/import operations backing the `solution list|export|import` commands.
    /// Solution zip inspection reads the `solution.xml` entry directly out of the <see cref="ZipArchive"/>
    /// instead of extracting to a temp directory.</summary>
    internal static class SolutionOperations
    {
        internal record SolutionExportRequest(string UniqueName, string OutputDirectory, bool Managed, bool PublishAll, bool Force, string? ExtractDirectory, bool CleanExtractDirectory);
        internal record SolutionExportResult(string FilePath, bool Skipped, string? ExtractDirectory);
        internal record SolutionImportRequest(string SolutionFilePath, bool DeleteExisting, bool Overwrite, bool PublishWorkflows, bool Force);
        internal record SolutionImportResult(string SolutionName, string SolutionVersion, bool Imported, string? SkipReason);

        #region List

        public static List<SolutionInfo> List(ServiceClient connection, IOperationOutput output)
        {
            var solutionQuery = new QueryExpression
            {
                EntityName = "solution",
                ColumnSet = new ColumnSet("publisherid", "installedon", "version", "versionnumber", "uniquename", "friendlyname", "isvisible")
            };
            solutionQuery.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);

            var solutions = connection
                .Query(solutionQuery, true)
                .Select(e => new SolutionInfo
                {
                    Id = e.Id,
                    Name = e.Attributes["friendlyname"].ToString()!,
                    UniqueName = e.Attributes["uniquename"].ToString()!,
                    Version = new Version(e.Attributes["version"].ToString()!),
                    Publisher = ((EntityReference)e.Attributes["publisherid"]).Name
                })
                .ToList();

            solutions.ForEach(output.WriteObject);
            return solutions;
        }

        #endregion

        #region Export

        public static SolutionExportResult Export(ServiceClient connection, SolutionExportRequest request, IOperationOutput output)
        {
            ValidateExtractOptions(request.ExtractDirectory, request.CleanExtractDirectory);

            if (request.PublishAll) connection.Execute(new PublishAllXmlRequest());

            var solutionVersion = RetrieveSolutionVersionString(connection, request.UniqueName)
                ?? throw new CliDataException($"Solution '{request.UniqueName}' not found in connected system!");

            Directory.CreateDirectory(request.OutputDirectory);
            var solutionFilePath = Path.Combine(request.OutputDirectory,
                $"{request.UniqueName} - {solutionVersion} - {connection.ConnectedOrgFriendlyName} - {(request.Managed ? "managed" : "unmanaged")}.zip");

            if (File.Exists(solutionFilePath) && !request.Force)
            {
                output.WriteWarning("Solution already existing in target path. Skipping ...");
                MaybeExtract(request.ExtractDirectory, request.CleanExtractDirectory, solutionFilePath);
                var skippedResult = new SolutionExportResult(solutionFilePath, Skipped: true, ExtractDirectory: request.ExtractDirectory);
                output.WriteObject(skippedResult);
                return skippedResult;
            }

            output.WriteProgress("Exporting", $"Exporting solution '{request.UniqueName}' ({solutionVersion}) from '{connection.ConnectedOrgFriendlyName}'...", 0);

            var exportSolutionRequest = new ExportSolutionRequest
            {
                Managed = request.Managed,
                SolutionName = request.UniqueName
            };
            var exportSolutionResponse = (ExportSolutionResponse)connection.Execute(exportSolutionRequest);

            output.WriteProgress("Exporting", "Saving solution ...", 75);

            File.WriteAllBytes(solutionFilePath, exportSolutionResponse.ExportSolutionFile);

            output.WriteProgress("Exporting", "Done!", completed: true);

            MaybeExtract(request.ExtractDirectory, request.CleanExtractDirectory, solutionFilePath);
            var result = new SolutionExportResult(solutionFilePath, Skipped: false, ExtractDirectory: request.ExtractDirectory);
            output.WriteObject(result);
            return result;
        }

        private static string? RetrieveSolutionVersionString(ServiceClient connection, string uniqueName)
        {
            var solutionQuery = new QueryExpression
            {
                EntityName = "solution",
                ColumnSet = new ColumnSet("publisherid", "installedon", "version", "versionnumber", "friendlyname")
            };
            solutionQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

            var entities = connection.Query(solutionQuery, true);
            return entities.SingleOrDefault()?.Attributes["version"].ToString();
        }

        #endregion

        /// <summary>
        /// Extracts a solution ZIP into a directory. Protects the filesystem and workspace roots
        /// from deletion through <see cref="ClearGuard.EnsureSafeToClear"/>.
        /// </summary>
        internal static void ValidateExtractOptions(string? extractDirectory, bool cleanExtractDirectory)
        {
            if (cleanExtractDirectory && string.IsNullOrWhiteSpace(extractDirectory))
                throw new CliValidationException("--clean requires --extract-dir.");
        }

        internal static void ExtractZip(string zipPath, string extractDirectory, bool cleanExtractDirectory)
        {
            if (cleanExtractDirectory)
            {
                ClearGuard.EnsureSafeToClear(extractDirectory);
                if (Directory.Exists(extractDirectory))
                    Directory.Delete(extractDirectory, recursive: true);
            }

            Directory.CreateDirectory(extractDirectory);
            ZipFile.ExtractToDirectory(zipPath, extractDirectory, overwriteFiles: true);
        }

        private static void MaybeExtract(string? extractDirectory, bool cleanExtractDirectory, string solutionFilePath)
        {
            if (!string.IsNullOrEmpty(extractDirectory))
                ExtractZip(solutionFilePath, extractDirectory, cleanExtractDirectory);
        }

        #region Import

        public static SolutionImportResult Import(ServiceClient connection, SolutionImportRequest request, IOperationOutput output)
        {
            var (solutionName, solutionVersion) = GetSolutionInfo(request.SolutionFilePath);
            var targetVersion = RetrieveSolutionVersionAsVersion(connection, solutionName);

            if (targetVersion != null) output.WriteVerbose($"Found version {targetVersion} in target system ...");
            else output.WriteVerbose("No version found in target system ...");

            if (targetVersion == solutionVersion && !request.Force)
            {
                var skipReason = $"Solution '{solutionName}' ({solutionVersion}) already deployed to target system!";
                output.WriteWarning(skipReason);
                var skippedResult = new SolutionImportResult(solutionName, solutionVersion.ToString(), Imported: false, skipReason);
                output.WriteObject(skippedResult);
                return skippedResult;
            }

            if (request.DeleteExisting) RemoveSolution(connection, solutionName);

            var solution = File.ReadAllBytes(request.SolutionFilePath);
            output.WriteVerbose($"Importing solution '{solutionName}' to '{connection.ConnectedOrgFriendlyName}'...");

            var importSolutionRequest = new ImportSolutionRequest
            {
                CustomizationFile = solution,
                OverwriteUnmanagedCustomizations = request.Overwrite,
                PublishWorkflows = request.PublishWorkflows
            };

            connection.Execute(importSolutionRequest);

            var result = new SolutionImportResult(solutionName, solutionVersion.ToString(), Imported: true, null);
            output.WriteObject(result);
            return result;
        }

        private static Version? RetrieveSolutionVersionAsVersion(ServiceClient connection, string solutionName)
        {
            var solutionQuery = new QueryExpression
            {
                EntityName = "solution",
                ColumnSet = new ColumnSet("version", "versionnumber")
            };
            solutionQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);

            var response = connection.Query(solutionQuery, true);
            return response.Count == 1 ? new Version(response[0].Attributes["version"].ToString()!) : null;
        }

        private static (string Name, Version Version) GetSolutionInfo(string solutionFilePath)
        {
            using var archive = ZipFile.OpenRead(solutionFilePath);
            var solutionEntry = archive.GetEntry("solution.xml")
                ?? throw new CliValidationException($"'{solutionFilePath}' does not look like a solution zip (no solution.xml entry).");

            using var stream = solutionEntry.Open();
            var solutionXml = XDocument.Load(stream);
            var uniqueName = solutionXml.XPathSelectElements("ImportExportXml/SolutionManifest/UniqueName").Single().Value;
            var version = new Version(solutionXml.XPathSelectElements("ImportExportXml/SolutionManifest/Version").Single().Value);

            return (uniqueName, version);
        }

        private static void RemoveSolution(ServiceClient connection, string solutionName)
        {
            var query = new QueryExpression("solution");
            query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);
            query.ColumnSet = new ColumnSet("solutionid", "friendlyname");

            var solution = connection.Query(query, true).SingleOrDefault();
            if (solution != null) connection.Delete("solution", solution.Id);
        }

        #endregion
    }
}
