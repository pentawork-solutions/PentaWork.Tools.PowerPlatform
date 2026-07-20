using dnlib.DotNet;
using PentaWork.Tools.PluginGraph;
using PentaWork.Tools.PluginGraph.Model;
using PentaWork.Tools.PluginGraph.Model.XrmInfoObjects;

namespace PentaWork.Tools.PluginGraphTests
{
    [TestClass]
    public sealed class TestPluginsServiceNoProxyTests
    {
        private Dictionary<Guid, PluginModuleList> _moduleList;
        private readonly PluginGraphAnalyzer _pluginGraphAnalyzer = new PluginGraphAnalyzer();
        private readonly PluginStepInfo _pluginStepInfo = new PluginStepInfo
        {
            Plugin = new PluginInfo
            {
                AssemblyInfo = new AssemblyInfo
                {
                    Name = "PentaWork.Tools.Tests.Plugins",
                },
                PackageInfo = new PackageInfo
                {
                    Id = new Guid("f305f42a-c37a-487a-a4b0-521b946315b6")
                }
            }
        };

        [TestInitialize]
        public void Initialize()
        {
            // The fixture assemblies (PentaWork.Tools.Tests.Plugins + its side assembly) are ProjectReferences
            // of this test project, so MSBuild already copies their built DLLs next to this one.
            var pluginAssemblyPath = AppContext.BaseDirectory;
            var assemblyList = Directory.GetFiles(pluginAssemblyPath, "PentaWork.Tools.Tests.*.dll", SearchOption.TopDirectoryOnly);

            var moduleList = new PluginModuleList();
            var ctx = ModuleDef.CreateModuleContext();
            var asmResolver = (AssemblyResolver)ctx.AssemblyResolver;
            asmResolver.EnableTypeDefCache = true;

            foreach (var assemblyFile in assemblyList)
            {
                var module = ModuleDefMD.Load(assemblyFile, ctx);
                module.Context = ctx;
                asmResolver.AddToCache(module);

                moduleList.Add(module);
            }
            _moduleList = new Dictionary<Guid, PluginModuleList>
            {
                { new Guid("f305f42a-c37a-487a-a4b0-521b946315b6"), moduleList }
            };
        }

        [TestMethod]
        public void ShouldAnalyseServiceCreateWithoutProxiesSuccessfully()
        {
            // Arrange
            _pluginStepInfo.Plugin.TypeName = "PentaWork.Tools.Tests.Plugins.TestPluginServiceCreate";

            // Act
            var apiCalls = _pluginGraphAnalyzer.AnalyzeApiCalls(_moduleList, [_pluginStepInfo], "PentaWork.Tools.Tests.*");
            var pluginApiCalls = apiCalls.FirstOrDefault().Value;

            // Assert
            Assert.IsNotNull(pluginApiCalls);
            Assert.AreEqual("create", pluginApiCalls.FirstOrDefault()?.Message);
            Assert.AreEqual(2, pluginApiCalls.FirstOrDefault()?.EntityInfo.UsedFields.Count);
            Assert.AreEqual("account", pluginApiCalls.FirstOrDefault()?.EntityInfo.LogicalName);
        }

        [TestMethod]
        public void ShouldAnalyseServiceUpdateWithoutProxiesSuccessfully()
        {
            // Arrange
            _pluginStepInfo.Plugin.TypeName = "PentaWork.Tools.Tests.Plugins.TestPluginServiceUpdate";

            // Act
            var apiCalls = _pluginGraphAnalyzer.AnalyzeApiCalls(_moduleList, [_pluginStepInfo], "PentaWork.Tools.Tests.*");
            var pluginApiCalls = apiCalls.FirstOrDefault().Value;

            // Assert
            Assert.IsNotNull(pluginApiCalls);
            Assert.AreEqual("update", pluginApiCalls.FirstOrDefault()?.Message);
            Assert.AreEqual(2, pluginApiCalls.FirstOrDefault()?.EntityInfo.UsedFields.Count);
            Assert.AreEqual("account", pluginApiCalls.FirstOrDefault()?.EntityInfo.LogicalName);
        }

        [TestMethod]
        public void ShouldAnalyseServiceDeleteWithoutProxiesSuccessfully()
        {
            // Arrange
            _pluginStepInfo.Plugin.TypeName = "PentaWork.Tools.Tests.Plugins.TestPluginServiceDelete";

            // Act
            var apiCalls = _pluginGraphAnalyzer.AnalyzeApiCalls(_moduleList, [_pluginStepInfo], "PentaWork.Tools.Tests.*");
            var pluginApiCalls = apiCalls.FirstOrDefault().Value;

            // Assert
            Assert.IsNotNull(pluginApiCalls);
            Assert.AreEqual("delete", pluginApiCalls.FirstOrDefault()?.Message);
            Assert.AreEqual(0, pluginApiCalls.FirstOrDefault()?.EntityInfo.UsedFields.Count);
            Assert.AreEqual("account", pluginApiCalls.FirstOrDefault()?.EntityInfo.LogicalName);
        }
    }
}
