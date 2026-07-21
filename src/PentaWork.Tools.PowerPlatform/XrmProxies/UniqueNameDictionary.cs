using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PentaWork.Tools.PowerPlatform.XrmProxies
{
    public class UniqueNameDictionary : Dictionary<string, string>
    {
        public string GetUniqueName(EntityMetadata entityMetadata, int languageCode)
        {
            return GetUniqueName(entityMetadata.DisplayName.GetLabel(languageCode, entityMetadata.LogicalName), entityMetadata.LogicalName);
        }

        public string GetUniqueName(AttributeMetadata attributeMetadata, int languageCode, string suffix = "")
        {
            return GetUniqueName(attributeMetadata.DisplayName.GetLabel(languageCode, attributeMetadata.LogicalName), attributeMetadata.LogicalName, string.Empty, suffix);
        }

        public string GetUniqueName(OneToManyRelationshipMetadata metadata)
        {
            return GetUniqueName(metadata.SchemaName, metadata.SchemaName);
        }

        public string GetUniqueName(ManyToManyRelationshipMetadata metadata)
        {
            return GetUniqueName(metadata.SchemaName, metadata.SchemaName);
        }

        public string GetUniqueName(OptionSetMetadata optionSetMetadata, int languageCode)
        {
            var prefix = optionSetMetadata.IsGlobal.Value ? "eg" : "e";
            return GetUniqueName(optionSetMetadata.DisplayName.GetLabel(languageCode, optionSetMetadata.Name), "os_" + optionSetMetadata.Name, prefix);
        }

        public string GetUniqueName(OptionMetadata optionMetadata, int languageCode)
        {
            var suffix = string.Empty;
            var statusOptionMetadata = optionMetadata as StatusOptionMetadata;
            if (statusOptionMetadata != null)
            {
                if (statusOptionMetadata.State == 0) suffix = "_Active";
                else if (statusOptionMetadata.State == 1) suffix = "_Inactive";
            }
            return GetUniqueName(optionMetadata.Label.GetLabel(languageCode, optionMetadata.Value.ToString()), "osv_" + optionMetadata.Value.ToString(), string.Empty, suffix);
        }

        public string GetUniqueName(string name, string key, string prefix = "", string suffix = "", int count = 1)
        {
            if (!ContainsKey(key))
            {
                var uniqueName = name.AsValidVariableName();
                uniqueName = Regex.Replace(uniqueName, $"({suffix})$", "", RegexOptions.IgnoreCase); //Remove suffix at end to avoid double suffix
                uniqueName = Regex.Replace(uniqueName, $"^({prefix})", ""); //Remove prefix at end to avoid double prefix
                uniqueName = prefix + uniqueName + suffix;
                uniqueName = count > 1 ? uniqueName + count : uniqueName;
                uniqueName = Regex.Replace(uniqueName, @"\t|\n|\r", "");

                if (ContainsValue(uniqueName)) GetUniqueName(name, key, prefix, suffix, ++count);
                else Add(key, uniqueName);
            }
            return this[key];
        }
    }
}
