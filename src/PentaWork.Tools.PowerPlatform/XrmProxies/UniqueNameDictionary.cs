using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PentaWork.Tools.PowerPlatform.XrmProxies
{
    public class UniqueNameDictionary : Dictionary<string, string>
    {
        private readonly int _languageCode;
        private readonly bool _useLogicalNames;

        /// <param name="languageCode">Language used to resolve display name labels.</param>
        /// <param name="useLogicalNames">
        /// If true, entity/attribute/option set names are taken from their logical names as-is
        /// (no casing changes, no "Id" suffix) instead of their localized display names.
        /// </param>
        public UniqueNameDictionary(int languageCode = -1, bool useLogicalNames = false)
        {
            _languageCode = languageCode;
            _useLogicalNames = useLogicalNames;
        }

        public string GetUniqueName(EntityMetadata entityMetadata)
        {
            var name = _useLogicalNames
                ? entityMetadata.LogicalName
                : entityMetadata.DisplayName.GetLabel(_languageCode, entityMetadata.LogicalName);
            return GetUniqueName(name, entityMetadata.LogicalName);
        }

        public string GetUniqueName(AttributeMetadata attributeMetadata, string suffix = "")
        {
            if (_useLogicalNames)
                return GetUniqueName(attributeMetadata.LogicalName, attributeMetadata.LogicalName);

            return GetUniqueName(attributeMetadata.DisplayName.GetLabel(_languageCode, attributeMetadata.LogicalName), attributeMetadata.LogicalName, string.Empty, suffix);
        }

        public string GetUniqueName(OneToManyRelationshipMetadata metadata)
        {
            return GetUniqueName(metadata.SchemaName, metadata.SchemaName);
        }

        public string GetUniqueName(ManyToManyRelationshipMetadata metadata)
        {
            return GetUniqueName(metadata.SchemaName, metadata.SchemaName);
        }

        public string GetUniqueName(OptionSetMetadata optionSetMetadata)
        {
            if (_useLogicalNames)
            {
                // Logical names are lower case - separate the prefix with "_" for readability
                var logicalPrefix = optionSetMetadata.IsGlobal.Value ? "eg_" : "e_";
                return GetUniqueName(optionSetMetadata.Name, "os_" + optionSetMetadata.Name, logicalPrefix);
            }

            var prefix = optionSetMetadata.IsGlobal.Value ? "eg" : "e";
            return GetUniqueName(optionSetMetadata.DisplayName.GetLabel(_languageCode, optionSetMetadata.Name), "os_" + optionSetMetadata.Name, prefix);
        }

        public string GetUniqueName(OptionMetadata optionMetadata)
        {
            var suffix = string.Empty;
            var statusOptionMetadata = optionMetadata as StatusOptionMetadata;
            if (statusOptionMetadata != null)
            {
                if (statusOptionMetadata.State == 0) suffix = "_Active";
                else if (statusOptionMetadata.State == 1) suffix = "_Inactive";
            }
            return GetUniqueName(optionMetadata.Label.GetLabel(_languageCode, optionMetadata.Value.ToString()), "osv_" + optionMetadata.Value.ToString(), string.Empty, suffix);
        }

        public string GetUniqueName(string name, string key, string prefix = "", string suffix = "", int count = 1)
        {
            if (!ContainsKey(key))
            {
                string uniqueName;
                if (_useLogicalNames)
                {
                    // Keep the name as-is - only ensure it is a valid identifier
                    uniqueName = (prefix + name + suffix).AsValidIdentifier();
                }
                else
                {
                    uniqueName = name.AsValidVariableName();
                    uniqueName = Regex.Replace(uniqueName, $"({suffix})$", "", RegexOptions.IgnoreCase); //Remove suffix at end to avoid double suffix
                    uniqueName = Regex.Replace(uniqueName, $"^({prefix})", ""); //Remove prefix at end to avoid double prefix
                    uniqueName = prefix + uniqueName + suffix;
                }
                uniqueName = count > 1 ? uniqueName + count : uniqueName;
                uniqueName = Regex.Replace(uniqueName, @"\t|\n|\r", "");

                if (ContainsValue(uniqueName)) GetUniqueName(name, key, prefix, suffix, ++count);
                else Add(key, uniqueName);
            }
            return this[key];
        }
    }
}
