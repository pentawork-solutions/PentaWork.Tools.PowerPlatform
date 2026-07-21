using Microsoft.Xrm.Sdk;
using System.Linq;

namespace PentaWork.Tools.PowerPlatform.XrmProxies
{
    public static class LabelExtensions
    {
        /// <summary>
        /// Returns the localized label of the given XRM Label.
        /// </summary>
        /// <param name="label">The label to get the localized value from.</param>
        /// <param name="fallback">The fallback label, if no label is existing.</param>
        /// <returns>The localized label.</returns>
        public static string GetLabel(this Label label, int languageCode, string fallback = "")
        {
            if (label.LocalizedLabels.Count == 0) return fallback;

            // Priority 1: exact match with target language code
            var localizedLabel = label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == languageCode);
            if (localizedLabel != null)
                return localizedLabel.Label!.Replace("\"", "'").Trim();

            // Priority 2: fallback to English (1033)
            localizedLabel = label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == 1033);
            if (localizedLabel != null)
                return localizedLabel.Label!.Replace("\"", "'").Trim();

            // Priority 3: deterministic fallback to lowest LanguageCode
            localizedLabel = label.LocalizedLabels.OrderBy(l => l.LanguageCode).FirstOrDefault();
            return localizedLabel!.Label.Replace("\"", "'").Trim();
        }
    }
}
