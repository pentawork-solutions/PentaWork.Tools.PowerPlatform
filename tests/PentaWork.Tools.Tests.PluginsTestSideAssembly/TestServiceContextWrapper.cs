using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace PentaWork.Tools.Tests.PluginsTestSideAssembly
{
    public class TestServiceContextWrapper
    {
        public TestServiceContextWrapper(IOrganizationService service)
        {
            Context = new OrganizationServiceContext(service);
        }

        public OrganizationServiceContext Context { get; }
    }
}
