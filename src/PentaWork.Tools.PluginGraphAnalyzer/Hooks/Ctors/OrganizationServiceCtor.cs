using dnlib.DotNet;
using PentaWork.Tools.PluginGraph.Model;
using PentaWork.Tools.PluginGraph.Model.VMObjects;

namespace PentaWork.Tools.PluginGraph.Hooks.Calls
{
    internal class OrganizationServiceCtor : IHook
    {
        public void ExecuteHook(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame)
        {
            var genObj = (GenericObj)parameters[0];
            genObj.Fields["OrganizationServiceContext.Service"] = parameters[1];
        }

        public bool HookApplicable(IMethod method, MethodDef? methodDef, List<object> parameters, StorageFrame storageFrame) =>
            method.FullName == "System.Void Microsoft.Xrm.Sdk.Client.OrganizationServiceContext::.ctor(Microsoft.Xrm.Sdk.IOrganizationService)";
    }
}
