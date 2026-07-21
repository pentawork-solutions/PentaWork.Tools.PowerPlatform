using Microsoft.Xrm.Sdk.Metadata;

namespace PentaWork.Tools.PowerPlatform.XrmProxies.Model
{
    public class OptionInfo
    {
        public OptionInfo(OptionMetadata optionMetadata, string uniqueValueName, int languageCode)
        {
            DisplayName = optionMetadata.Label.GetLabel(languageCode);
            UniqueValueName = uniqueValueName;
            Value = optionMetadata.Value.Value;
            Description = optionMetadata.Description?.GetLabel(languageCode) ?? string.Empty;
        }

        public string DisplayName { get; }
        public string UniqueValueName { get; }
        public string Description { get; }
        public int Value { get; }
    }
}
