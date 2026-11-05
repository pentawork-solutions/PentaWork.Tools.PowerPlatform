using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;
using PentaWork.Tools.PowerPlatform.Tests.Fixtures;
using PentaWork.Tools.PowerPlatform.XrmProxies;
using PentaWork.Tools.PowerPlatform.XrmProxies.Model;
using ScribanTemplateRenderer = PentaWork.Tools.PowerPlatform.XrmProxies.Templates.ScribanTemplateRenderer;

namespace PentaWork.Tools.PowerPlatform.Tests
{
    /// <summary>
    /// Covers `proxy generate --use-logical-names`: entity/attribute names are taken from logical names
    /// as-is, option set enums use the option set name with an e_/eg_ prefix.
    /// </summary>
    [TestClass]
    public class LogicalNameProxyTests
    {
        private static EntityInfo GetMainEntity() =>
            MetadataFixture.BuildEntityInfoList(useLogicalNames: true).Single(e => e.LogicalName == "test_entity");

        [TestMethod]
        public void EntityNames_AreLogicalNamesAsIs()
        {
            var entityInfoList = MetadataFixture.BuildEntityInfoList(useLogicalNames: true);

            CollectionAssert.AreEquivalent(new[] { "test_entity", "related_entity" }, entityInfoList.Select(e => e.UniqueDisplayName).ToList());
        }

        [TestMethod]
        public void AttributeNames_AreLogicalNamesAsIs_WithoutIdSuffix()
        {
            var entity = GetMainEntity();

            CollectionAssert.AreEquivalent(
                new[] { "test_entityid", "name", "revenue", "rating", "prioritycode", "categories", "parentid" },
                entity.AttributeList.Select(a => a.UniqueDisplayName).ToList());
            Assert.AreEqual("test_entityid", entity.PrimaryIdAttribute.UniqueDisplayName);
        }

        [TestMethod]
        public void OptionSetEnums_UseOptionSetNameWithUnderscorePrefix()
        {
            var entity = GetMainEntity();

            CollectionAssert.AreEquivalent(
                new[] { "e_test_entity_categories", "e_test_entity_prioritycode" },
                entity.OptionSetList.Select(o => o.UniqueDisplayName).ToList());
            Assert.AreEqual("e_test_entity_prioritycode", entity.AttributeList.Single(a => a.LogicalName == "prioritycode").ReturnType);
            // Option values keep their label based names
            CollectionAssert.AreEqual(new[] { "Low", "Medium", "High" },
                entity.OptionSetList.Single(o => o.LogicalName == "test_entity_prioritycode").Options.Select(o => o.UniqueValueName).ToList());
        }

        [TestMethod]
        public void GlobalOptionSet_UsesEgPrefix()
        {
            var dic = new UniqueNameDictionary(1033, useLogicalNames: true);
            var optionSet = new Microsoft.Xrm.Sdk.Metadata.OptionSetMetadata { Name = "new_status", IsGlobal = true };

            Assert.AreEqual("eg_new_status", dic.GetUniqueName(optionSet));
        }

        [TestMethod]
        public void ManyToManyAttributes_AreLogicalNamesAsIs()
        {
            var relation = GetMainEntity().ManyToManyRelationList.Single();

            Assert.AreEqual("test_entity_related_entity_assoc", relation.UniqueIntersectDisplayName);
            Assert.AreEqual("test_entityid", relation.UniqueEntity1AttributeName);
            Assert.AreEqual("related_entityid", relation.UniqueEntity2AttributeName);
        }

        [TestMethod]
        public void AsValidIdentifier_KeepsCasingAndEscapesKeywords()
        {
            Assert.AreEqual("new_MyField", "new_MyField".AsValidIdentifier());
            Assert.AreEqual("_1abc", "1abc".AsValidIdentifier());
            Assert.AreEqual("@class", "class".AsValidIdentifier());
        }

        [TestMethod]
        public void ProxyClass_RendersLogicalNames()
        {
            var entity = GetMainEntity();
            var optionSetEnumsCs = string.Join(Environment.NewLine,
                entity.OptionSetList.Select(os => ScribanTemplateRenderer.Render("CSharp.OptionSet", new { OptionSetInfo = os })));

            var result = ScribanTemplateRenderer.Render("CSharp.ProxyClass",
                new { EntityInfo = entity, ProxyNamespace = "Test.Proxies", UseBaseProxy = true, OptionSetEnumsCs = optionSetEnumsCs });

            StringAssert.Contains(result, "public sealed class test_entity : BaseProxy");
            StringAssert.Contains(result, "public decimal? revenue");
            StringAssert.Contains(result, "public enum e_test_entity_prioritycode");
        }

        [TestMethod]
        public void GenerateAll_WithLogicalNames_WritesLogicalNameFiles()
        {
            var outputPath = Path.Combine(Path.GetTempPath(), "PentaWorkSmokeTest_" + Guid.NewGuid());
            Directory.CreateDirectory(outputPath);
            try
            {
                var request = new ProxyOperations.ProxyGenerateRequest(
                    ProxyNamespace: "Smoke.Proxies",
                    FakeNamespace: "Smoke.Fakes",
                    Solution: null,
                    IncludeEntities: [],
                    ExcludeEntities: [],
                    OutputPath: outputPath,
                    Clear: false,
                    UseBaseProxy: true,
                    CSharpOutputDirectory: null,
                    FakeOutputDirectory: null,
                    TypeScriptOutputDirectory: null,
                    UseLogicalNames: true);
                var output = new ConsoleOperationOutput(verbose: false);
                var entityInfoList = MetadataFixture.BuildEntityInfoList(request.UseLogicalNames);

                ProxyOperations.EnsureFolder(request.EffectiveCSOutputPath);
                ProxyOperations.EnsureFolder(request.EffectiveTSOutputPath);
                ProxyOperations.GenerateBaseClasses(request, output);
                ProxyOperations.GenerateAllCSharp(entityInfoList, request, output);
                ProxyOperations.GenerateAllJavascript(entityInfoList, request, output);

                Assert.IsTrue(File.Exists(Path.Combine(request.CSOutputPath, "Entities", "test_entity.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(request.CSOutputPath, "Relations", "test_entity_related_entity_assoc.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(outputPath, "Fake", "test_entity.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(request.TSOutputPath, "Entities", "test_entity.ts")));
            }
            finally
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }
}
