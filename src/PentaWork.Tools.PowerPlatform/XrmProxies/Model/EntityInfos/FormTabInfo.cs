using PentaWork.Tools.PowerPlatform.XrmProxies;

namespace PentaWork.Tools.PowerPlatform.XrmProxies.Model
{
    public class FormTabInfo
    {
        public FormTabInfo(string name)
        {
            Name = name;
            UniqueName = name.AsValidVariableName();
        }

        public string Name { get; }
        public string UniqueName { get; }
    }
}
