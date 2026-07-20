using dnlib.DotNet;
using PentaWork.Tools.PluginGraph.Model;

namespace PentaWork.Tools.PluginGraph.Hooks
{
    internal interface IHook
    {
        bool HookApplicable(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame);
        void ExecuteHook(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame);
    }
}
