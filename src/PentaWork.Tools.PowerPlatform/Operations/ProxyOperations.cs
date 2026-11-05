using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;
using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.XrmProxies;
using PentaWork.Tools.PowerPlatform.XrmProxies.Model;
using PentaWork.Tools.PowerPlatform.XrmProxies.Templates;

namespace PentaWork.Tools.PowerPlatform.Operations
{
    /// <summary>Proxy/fake class generation operation backing the `proxy generate` command - queries Dataverse
    /// for SDK messages/actions/entity metadata/system forms, builds the XrmProxies model graph and renders
    /// it through the Scriban C#/TypeScript/Fake templates.</summary>
    internal static class ProxyOperations
    {
        internal record ProxyGenerateRequest(
            string ProxyNamespace,
            string FakeNamespace,
            string? Solution,
            IReadOnlyList<string> IncludeEntities,
            IReadOnlyList<string> ExcludeEntities,
            string OutputPath,
            bool Clear,
            bool UseBaseProxy,
            string? CSharpOutputDirectory,
            string? FakeOutputDirectory,
            string? TypeScriptOutputDirectory,
            bool UseLogicalNames = false)
        {
            public string CSOutputPath => Path.Combine(OutputPath, "CS");
            public string TSOutputPath => Path.Combine(OutputPath, "TS");

            public string EffectiveCSOutputPath => CSharpOutputDirectory ?? CSOutputPath;
            public string EffectiveFakeOutputPath => FakeOutputDirectory ?? Path.Combine(OutputPath, "Fake");
            public string EffectiveTSOutputPath => TypeScriptOutputDirectory ?? TSOutputPath;
            public bool HasDirectOutputTargets => CSharpOutputDirectory is not null || FakeOutputDirectory is not null || TypeScriptOutputDirectory is not null;
        }

        internal record ProxyGenerateResult(string OutputPath, int EntityCount);

        public static ProxyGenerateResult Generate(ServiceClient connection, ProxyGenerateRequest request, IOperationOutput output)
        {
            if (request.Clear)
            {
                var clearPaths = new List<string>();
                if (request.HasDirectOutputTargets)
                {
                    clearPaths.Add(request.EffectiveCSOutputPath);
                    clearPaths.Add(request.EffectiveFakeOutputPath);
                    clearPaths.Add(request.EffectiveTSOutputPath);
                }
                else
                {
                    clearPaths.Add(request.OutputPath);
                }

                foreach (var path in clearPaths.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    ClearGuard.EnsureSafeToClear(path);
                    if (Directory.Exists(path)) Directory.Delete(path, true);
                }
            }

            var languageCode = GetOrganizationBaseLanguageCode(connection, output);
            var sdkMessages = GetAllSdkMessages(connection, output);
            var actions = GetAllActions(connection, output);
            var entityMetadata = GetAllEntityMetadata(connection, sdkMessages, output);
            var systemForms = GetAllSystemForms(connection, output);

            var includeEntities = request.IncludeEntities.ToList();
            if (!string.IsNullOrEmpty(request.Solution))
            {
                var solutionEntities = GetSolutionEntityNames(connection, request.Solution, output);
                includeEntities.AddRange(entityMetadata.Where(e => solutionEntities.Any(s => s == e.MetadataId)).Select(e => e.LogicalName));
            }
            var filteredEntityMetadata = GetFilteredEntityMetaData(entityMetadata, includeEntities, request.ExcludeEntities);
            var entityInfoList = new EntityInfoList(filteredEntityMetadata, systemForms, actions, languageCode, request.UseLogicalNames);

            EnsureFolder(request.OutputPath);
            EnsureFolder(request.EffectiveCSOutputPath);
            EnsureFolder(request.EffectiveFakeOutputPath);
            EnsureFolder(request.EffectiveTSOutputPath);

            GenerateBaseClasses(request, output);
            GenerateAllCSharp(entityInfoList, request, output);
            GenerateAllJavascript(entityInfoList, request, output);

            output.WriteProgress("Generating", "Done!", completed: true);

            var result = new ProxyGenerateResult(request.OutputPath, entityInfoList.Count);
            output.WriteObject(result);
            return result;
        }

        internal static void EnsureFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        }

        private static int GetOrganizationBaseLanguageCode(ServiceClient connection, IOperationOutput output)
        {
            output.WriteProgress("Generating", "Getting organization base language ...", 5);

            var query = new QueryExpression("organization");
            query.ColumnSet = new ColumnSet("languagecode");

            return (int)connection.Query(query, true).First()["languagecode"];
        }

        private static List<Entity> GetAllSdkMessages(ServiceClient connection, IOperationOutput output)
        {
            output.WriteProgress("Generating", "Getting SDK Messages ...", 0);

            var query = new QueryExpression("sdkmessage");
            query.LinkEntities.Add(new LinkEntity("sdkmessage", "sdkmessagefilter", "sdkmessageid", "sdkmessageid", JoinOperator.Inner));
            query.LinkEntities[0].Columns.AddColumns("primaryobjecttypecode", "secondaryobjecttypecode");
            query.LinkEntities[0].EntityAlias = "sdmessagefilter";

            query.ColumnSet = new ColumnSet("name", "isprivate");
            query.Criteria.AddCondition("isprivate", ConditionOperator.Equal, false);

            return connection.Query(query, true);
        }

        private static List<ActionInfo> GetAllActions(ServiceClient connection, IOperationOutput output)
        {
            output.WriteProgress("Generating", "Getting available Actions ...", 10);

            var query = new QueryExpression("workflow");
            query.LinkEntities.Add(new LinkEntity("workflow", "sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.Inner));
            query.LinkEntities[0].Columns.AddColumns("name");
            query.LinkEntities[0].EntityAlias = "sdkmessage";

            query.ColumnSet = new ColumnSet("name", "sdkmessageid", "primaryentity");
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1);
            query.Criteria.AddCondition("type", ConditionOperator.Equal, 1);
            query.Criteria.AddCondition("category", ConditionOperator.Equal, 3);

            var actionNameDic = new UniqueNameDictionary();
            return connection.Query(query, true)
                .Select(e => new ActionInfo(actionNameDic.GetUniqueName((string)e["name"], ((EntityReference)e["sdkmessageid"]).Id.ToString()), (string)((AliasedValue)e["sdkmessage.name"]).Value, (string)e["primaryentity"]))
                .ToList();
        }

        private static List<EntityMetadata> GetAllEntityMetadata(ServiceClient connection, List<Entity> sdkMessages, IOperationOutput output)
        {
            output.WriteProgress("Generating", "Getting metadata ...", 20);

            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                RetrieveAsIfPublished = false
            };

            return ((RetrieveAllEntitiesResponse)connection.Execute(request))
                .EntityMetadata
                .Where(e =>
                       !e.IsIntersect.GetValueOrDefault()
                    && !e.IsPrivate.GetValueOrDefault()
                    // Remove all Entities which are not useable with any non private SDK message
                    && sdkMessages.Any(s => ((AliasedValue)s["sdmessagefilter.primaryobjecttypecode"]).Value as string == e.LogicalName
                                         || ((AliasedValue)s["sdmessagefilter.secondaryobjecttypecode"]).Value as string == e.LogicalName))
                .ToList();
        }

        private static List<Entity> GetAllSystemForms(ServiceClient connection, IOperationOutput output)
        {
            output.WriteProgress("Generating", "Getting system forms ...", 30);

            var query = new QueryExpression("systemform");
            query.Criteria.AddCondition("objecttypecode", ConditionOperator.NotEqual, "none");
            query.ColumnSet = new ColumnSet(true);

            return connection.Query(query, true);
        }

        private static List<Guid> GetSolutionEntityNames(ServiceClient connection, string solution, IOperationOutput output)
        {
            output.WriteProgress("Generating", "Getting Solution Entities ...", 35);

            var solutionQuery = new QueryExpression
            {
                EntityName = "solution",
                ColumnSet = new ColumnSet("uniquename", "friendlyname", "isvisible")
            };
            solutionQuery.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);
            solutionQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solution);

            var solutionId = connection
                .Query(solutionQuery, true)
                .SingleOrDefault()?.Id ?? throw new CliDataException($"Solution with unique name '{solution}' not found!");

            var componentQuery = new QueryExpression
            {
                EntityName = "solutioncomponent",
                ColumnSet = new ColumnSet("objectid", "solutioncomponentid", "componenttype")
            };
            componentQuery.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
            componentQuery.Criteria.AddCondition("componenttype", ConditionOperator.Equal, 1); // Entity

            return connection.Query(componentQuery, true).Select(c => new Guid(c.Attributes["objectid"].ToString()!)).ToList();
        }

        private static List<EntityMetadata> GetFilteredEntityMetaData(List<EntityMetadata> entityMetadataList, List<string> includeEntities, IReadOnlyList<string> excludeEntities)
        {
            var filteredEntityMetadataList = includeEntities.Count > 0
                ? entityMetadataList.Where(e => includeEntities.Any(i => i == e.LogicalName)).ToList()
                : entityMetadataList;
            return filteredEntityMetadataList.Where(f => excludeEntities.All(e => e != f.LogicalName)).ToList();
        }

        internal static void GenerateBaseClasses(ProxyGenerateRequest request, IOperationOutput output)
        {
            if (request.UseBaseProxy)
            {
                output.WriteProgress("Generating", "Generating Base Proxy ...", 40);
                File.WriteAllText(Path.Combine(request.EffectiveCSOutputPath, "BaseProxy.cs"), ScribanTemplateRenderer.Render("CSharp.BaseProxy", new { request.ProxyNamespace }));
            }

            output.WriteProgress("Generating", "Generating Attributes ...", 40);
            File.WriteAllText(Path.Combine(request.EffectiveCSOutputPath, "Attributes.cs"), ScribanTemplateRenderer.Render("CSharp.Attributes", new { request.ProxyNamespace }));

            File.WriteAllText(Path.Combine(request.EffectiveCSOutputPath, "AssemblyInfoAddition.cs"), ScribanTemplateRenderer.Render("CSharp.AssemblyInfoAddition", new { }));

            output.WriteProgress("Generating", "Generating Extensions ...", 45);
            EnsureFolder(Path.Combine(request.EffectiveCSOutputPath, "Extensions"));

            File.WriteAllText(Path.Combine(request.EffectiveCSOutputPath, "Extensions", "EnumExtensions.cs"), ScribanTemplateRenderer.Render("CSharp.EnumExtensions", new { request.ProxyNamespace }));
        }

        internal static void GenerateAllCSharp(EntityInfoList entityInfoList, ProxyGenerateRequest request, IOperationOutput output)
        {
            output.WriteProgress("Generating", "[CS] Generating Proxy & Fake Classes ...", 50);
            EnsureFolder(Path.Combine(request.EffectiveCSOutputPath, "Entities"));
            EnsureFolder(request.EffectiveFakeOutputPath);
            foreach (var entityInfo in entityInfoList)
            {
                var optionSetEnumsCs = string.Join(Environment.NewLine,
                    entityInfo.OptionSetList.Select(os => ScribanTemplateRenderer.Render("CSharp.OptionSet", new { OptionSetInfo = os })));
                File.WriteAllText(Path.Combine(request.EffectiveCSOutputPath, "Entities", $"{entityInfo.UniqueDisplayName}.cs"),
                    ScribanTemplateRenderer.Render("CSharp.ProxyClass", new { EntityInfo = entityInfo, request.ProxyNamespace, request.UseBaseProxy, OptionSetEnumsCs = optionSetEnumsCs }));

                File.WriteAllText(Path.Combine(request.EffectiveFakeOutputPath, $"{entityInfo.UniqueDisplayName}.cs"),
                    ScribanTemplateRenderer.Render("CSharp.Fake", new { EntityInfo = entityInfo, request.ProxyNamespace, request.FakeNamespace }));
            }

            output.WriteProgress("Generating", "[CS] Generating Relation Classes ...", 65);
            EnsureFolder(Path.Combine(request.EffectiveCSOutputPath, "Relations"));
            // Group by intersect entity. It is possible, that multiple schemas are using the same intersect entity!
            foreach (var relationInfoGroup in entityInfoList.SelectMany(e => e.ManyToManyRelationList).GroupBy(e => e.IntersectEntityName))
            {
                var relationInfo = relationInfoGroup.First();
                File.WriteAllText(Path.Combine(request.EffectiveCSOutputPath, "Relations", $"{relationInfo.UniqueIntersectDisplayName}.cs"),
                    ScribanTemplateRenderer.Render("CSharp.RelationProxyClass", new { RelationClassInfo = relationInfo, request.ProxyNamespace, request.UseBaseProxy }));
            }
        }

        internal static void GenerateAllJavascript(EntityInfoList entityInfoList, ProxyGenerateRequest request, IOperationOutput output)
        {
            output.WriteProgress("Generating", "[TS] Generating Proxy Base Types ...", 75);
            File.WriteAllText(Path.Combine(request.EffectiveTSOutputPath, "ProxyTypes.ts"), ScribanTemplateRenderer.Render("Javascript.ProxyTypes", new { }));

            output.WriteProgress("Generating", "[TS] Generating Entity Proxies...", 85);
            EnsureFolder(Path.Combine(request.EffectiveTSOutputPath, "Entities"));
            foreach (var entityInfo in entityInfoList)
            {
                var optionSetEnumsJs = string.Join(Environment.NewLine,
                    entityInfo.OptionSetList.Select(os => ScribanTemplateRenderer.Render("Javascript.OptionSetJS", new { OptionSetInfo = os })));
                File.WriteAllText(Path.Combine(request.EffectiveTSOutputPath, "Entities", $"{entityInfo.UniqueDisplayName}.ts"),
                    ScribanTemplateRenderer.Render("Javascript.ProxyClassJS", new { EntityInfo = entityInfo, OptionSetEnumsJs = optionSetEnumsJs }));
            }

            output.WriteProgress("Generating", "[TS] Generating Attribute Name Modules...", 90);
            EnsureFolder(Path.Combine(request.EffectiveTSOutputPath, "Attributes"));
            foreach (var entityInfo in entityInfoList)
            {
                File.WriteAllText(Path.Combine(request.EffectiveTSOutputPath, "Attributes", $"{entityInfo.UniqueDisplayName}.ts"),
                    ScribanTemplateRenderer.Render("Javascript.AttributeJS", new { EntityInfo = entityInfo }));
            }

            output.WriteProgress("Generating", "[TS] Generating Form Info Modules...", 95);
            EnsureFolder(Path.Combine(request.EffectiveTSOutputPath, "FormInfos"));
            foreach (var entityInfo in entityInfoList)
            {
                File.WriteAllText(Path.Combine(request.EffectiveTSOutputPath, "FormInfos", $"{entityInfo.UniqueDisplayName}.ts"),
                    ScribanTemplateRenderer.Render("Javascript.FormInfosJS", new { EntityInfo = entityInfo }));
            }
        }
    }
}
