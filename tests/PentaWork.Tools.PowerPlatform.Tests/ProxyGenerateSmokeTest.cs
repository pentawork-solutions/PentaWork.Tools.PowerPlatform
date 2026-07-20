using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Operations;
using PentaWork.Tools.PowerPlatform.Tests.Fixtures;

namespace PentaWork.Tools.PowerPlatform.Tests
{
    /// <summary>
    /// Exercises the real `proxy generate` file-generation path directly against
    /// <see cref="ProxyOperations"/> (no live Dataverse connection needed - metadata retrieval happens
    /// before these helpers run and is exercised separately). Confirms the operation's own wiring of
    /// ScribanTemplateRenderer + File.WriteAllText + folder creation works end-to-end, not just the
    /// renderer in isolation like TemplateGoldenFileTests does.
    /// </summary>
    [TestClass]
    public class ProxyGenerateSmokeTest
    {
        [TestMethod]
        public void GenerateAll_ProducesExpectedFileTree()
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
                    UseBaseProxy: true);
                var output = new ConsoleOperationOutput(verbose: false);

                var entityInfoList = MetadataFixture.BuildEntityInfoList();

                ProxyOperations.EnsureFolder(outputPath);
                ProxyOperations.EnsureFolder(request.CSOutputPath);
                ProxyOperations.EnsureFolder(request.TSOutputPath);

                ProxyOperations.GenerateBaseClasses(request, output);
                ProxyOperations.GenerateAllCSharp(entityInfoList, request, output);
                ProxyOperations.GenerateAllJavascript(entityInfoList, request, output);

                var csOutputPath = request.CSOutputPath;
                var tsOutputPath = request.TSOutputPath;

                // Base classes
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "BaseProxy.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "Attributes.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "AssemblyInfoAddition.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "Extensions", "EnumExtensions.cs")));

                // Per-entity C# proxy + fake classes
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "Entities", "TestEntity.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "Entities", "RelatedEntity.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(outputPath, "Fake", "TestEntity.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(csOutputPath, "Relations", "TestEntityRelatedEntityAssoc.cs")));

                // TypeScript output
                Assert.IsTrue(File.Exists(Path.Combine(tsOutputPath, "ProxyTypes.ts")));
                Assert.IsTrue(File.Exists(Path.Combine(tsOutputPath, "Entities", "TestEntity.ts")));
                Assert.IsTrue(File.Exists(Path.Combine(tsOutputPath, "Attributes", "TestEntity.ts")));
                Assert.IsTrue(File.Exists(Path.Combine(tsOutputPath, "FormInfos", "TestEntity.ts")));

                var proxyClassContent = File.ReadAllText(Path.Combine(csOutputPath, "Entities", "TestEntity.cs"));
                StringAssert.Contains(proxyClassContent, "public enum ePriority");
                StringAssert.Contains(proxyClassContent, "public decimal? Revenue");
                StringAssert.Contains(proxyClassContent, "public override Guid Id");
            }
            finally
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }
}
