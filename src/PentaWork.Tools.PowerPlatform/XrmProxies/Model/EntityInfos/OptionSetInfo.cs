using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Metadata;

namespace PentaWork.Tools.PowerPlatform.XrmProxies.Model
{
    public class OptionSetInfo
    {
        private readonly UniqueNameDictionary _optionNameDic = new UniqueNameDictionary();

        public OptionSetInfo(OptionSetMetadata osMetadata, string uniqueDisplayName, int languageCode)
        {
            LogicalName = osMetadata.Name;
            UniqueDisplayName = uniqueDisplayName;
            Options = osMetadata.Options.Select(optionMetadata => new OptionInfo(optionMetadata, _optionNameDic.GetUniqueName(optionMetadata, languageCode), languageCode)).ToList();
        }

        public string LogicalName { get; }
        public string UniqueDisplayName { get; }
        public List<OptionInfo> Options { get; }
    }
}
