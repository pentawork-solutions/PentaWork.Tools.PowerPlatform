using dnlib.DotNet;
using PentaWork.Tools.PluginGraph.Model;
using PentaWork.Tools.PluginGraph.Model.VMObjects;

namespace PentaWork.Tools.PluginGraph.Hooks.Calls.ServiceContact
{
    internal class OranizationServiceContextClearChangesHook : IHook
    {
        public void ExecuteHook(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame)
        {
            var serviceContext = (ServiceContextObj)parameters[0];
            serviceContext.ClearQueue();
        }

        public bool HookApplicable(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame) =>
            method.FullName == "System.Void Microsoft.Xrm.Sdk.Client.OrganizationServiceContext::ClearChanges()";
    }
}
