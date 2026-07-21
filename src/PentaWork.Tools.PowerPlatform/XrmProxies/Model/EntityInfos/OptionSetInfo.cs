using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Metadata;

namespace PentaWork.Tools.PowerPlatform.XrmProxies.Model
{
    public class OptionSetInfo
    {
        private readonly UniqueNameDictionary _optionNameDic;

        public OptionSetInfo(OptionSetMetadata osMetadata, string uniqueDisplayName, int languageCode)
        {
            _optionNameDic = new UniqueNameDictionary(languageCode);

            LogicalName = osMetadata.Name;
            UniqueDisplayName = uniqueDisplayName;
            Options = osMetadata.Options.Select(optionMetadata => new OptionInfo(optionMetadata, _optionNameDic.GetUniqueName(optionMetadata), languageCode)).ToList();
        }

        public string LogicalName { get; }
        public string UniqueDisplayName { get; }
        public List<OptionInfo> Options { get; }
    }
}
