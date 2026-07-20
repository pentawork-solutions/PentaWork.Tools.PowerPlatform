using dnlib.DotNet;
using PentaWork.Tools.PluginGraph.Model;

namespace PentaWork.Tools.PluginGraph.Hooks.Calls.Entity
{
    internal class EntityGetAttributesCallHook : IHook
    {
        public void ExecuteHook(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame)
        {
            storageFrame.Stack.Push(parameters[0]);
        }

        public bool HookApplicable(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame) =>
            method.FullName == "Microsoft.Xrm.Sdk.AttributeCollection Microsoft.Xrm.Sdk.Entity::get_Attributes()";
    }
}
