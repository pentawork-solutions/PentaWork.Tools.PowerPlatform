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
                    UseBaseProxy: true,
                    CSharpOutputDirectory: null,
                    FakeOutputDirectory: null,
                    TypeScriptOutputDirectory: null);
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

        [TestMethod]
        public void GenerateAll_WithDirectOutputTargets_WritesToExplicitFolders()
        {
            var outputPath = Path.Combine(Path.GetTempPath(), "PentaWorkSmokeTest_" + Guid.NewGuid());
            Directory.CreateDirectory(outputPath);
            try
            {
                var csDir = Path.Combine(outputPath, "cs_proxies");
                var fakeDir = Path.Combine(outputPath, "fake_proxies");
                var tsDir = Path.Combine(outputPath, "ts_proxies");

                var request = new ProxyOperations.ProxyGenerateRequest(
                    ProxyNamespace: "Smoke.Proxies",
                    FakeNamespace: "Smoke.Fakes",
                    Solution: null,
                    IncludeEntities: [],
                    ExcludeEntities: [],
                    OutputPath: outputPath,
                    Clear: false,
                    UseBaseProxy: true,
                    CSharpOutputDirectory: csDir,
                    FakeOutputDirectory: fakeDir,
                    TypeScriptOutputDirectory: tsDir);
                var output = new ConsoleOperationOutput(verbose: false);

                var entityInfoList = MetadataFixture.BuildEntityInfoList();

                // Don't create CS/TS/Fake under outputPath - let the renderer create the direct targets.
                ProxyOperations.EnsureFolder(csDir);
                ProxyOperations.EnsureFolder(tsDir);

                ProxyOperations.GenerateBaseClasses(request, output);
                ProxyOperations.GenerateAllCSharp(entityInfoList, request, output);
                ProxyOperations.GenerateAllJavascript(entityInfoList, request, output);

                // BaseProxy.cs should be in the explicit C# dir, not under outputPath/CS
                Assert.IsTrue(File.Exists(Path.Combine(csDir, "BaseProxy.cs")), "BaseProxy.cs should be in explicit C# dir");
                Assert.IsTrue(File.Exists(Path.Combine(csDir, "Entities", "TestEntity.cs")), "TestEntity.cs in explicit C# dir");

                // Fake should be in the explicit fake dir, not under outputPath/Fake
                Assert.IsTrue(File.Exists(Path.Combine(fakeDir, "TestEntity.cs")), "TestEntity.cs fake should be in explicit fake dir");

                // TypeScript should be in the explicit TS dir, not under outputPath/TS
                Assert.IsTrue(File.Exists(Path.Combine(tsDir, "ProxyTypes.ts")), "ProxyTypes.ts in explicit TS dir");

                // Under the staging outputPath there should be no CS, Fake, or TS folders
                Assert.IsFalse(Directory.Exists(Path.Combine(outputPath, "CS")), "CS dir should not exist under staging outputPath");
                Assert.IsFalse(Directory.Exists(Path.Combine(outputPath, "Fake")), "Fake dir should not exist under staging outputPath");
                Assert.IsFalse(Directory.Exists(Path.Combine(outputPath, "TS")), "TS dir should not exist under staging outputPath");
            }
            finally
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }

        [TestMethod]
        public void GenerateAll_WithMixedOutputTargets_UsesFallbackPathsForUnsetTargets()
        {
            var outputPath = Path.Combine(Path.GetTempPath(), "PentaWorkSmokeTest_" + Guid.NewGuid());
            Directory.CreateDirectory(outputPath);
            try
            {
                var csharpOutputPath = Path.Combine(outputPath, "custom-csharp");
                var request = new ProxyOperations.ProxyGenerateRequest(
                    ProxyNamespace: "Smoke.Proxies",
                    FakeNamespace: "Smoke.Fakes",
                    Solution: null,
                    IncludeEntities: [],
                    ExcludeEntities: [],
                    OutputPath: outputPath,
                    Clear: false,
                    UseBaseProxy: true,
                    CSharpOutputDirectory: csharpOutputPath,
                    FakeOutputDirectory: null,
                    TypeScriptOutputDirectory: null);
                var output = new ConsoleOperationOutput(verbose: false);
                var entityInfoList = MetadataFixture.BuildEntityInfoList();

                ProxyOperations.EnsureFolder(request.OutputPath);
                ProxyOperations.EnsureFolder(request.EffectiveCSOutputPath);
                ProxyOperations.EnsureFolder(request.EffectiveFakeOutputPath);
                ProxyOperations.EnsureFolder(request.EffectiveTSOutputPath);
                ProxyOperations.GenerateBaseClasses(request, output);
                ProxyOperations.GenerateAllCSharp(entityInfoList, request, output);
                ProxyOperations.GenerateAllJavascript(entityInfoList, request, output);

                Assert.IsTrue(File.Exists(Path.Combine(csharpOutputPath, "BaseProxy.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(outputPath, "Fake", "TestEntity.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(outputPath, "TS", "ProxyTypes.ts")));
                Assert.IsFalse(Directory.Exists(Path.Combine(outputPath, "CS")));
            }
            finally
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }
}
