using Microsoft.Xrm.Sdk;
using PentaWork.Tools.PowerPlatform.XrmProxies;

namespace PentaWork.Tools.PowerPlatform.Tests;

/// <summary>Covers the three fallback behaviors of GetLabel introduced in task 01:
/// target language priority, fallback to English (1033) when target is missing, and
/// deterministic fallback to the lowest LanguageCode when neither target nor English are present.</summary>
[TestClass]
public class LabelExtensionsTests
{
    [TestMethod]
    public void GetLabel_PrefersTargetLanguageOverEnglish()
    {
        var label = BuildLabel(("Company", 1033), ("Firma", 1031));

        var result = label.GetLabel(1031);

        Assert.AreEqual("Firma", result);
    }

    [TestMethod]
    public void GetLabel_FallsBackToEnglishWhenTargetLanguageMissing()
    {
        var label = BuildLabel(("Company", 1033), ("Firma", 1031));

        var result = label.GetLabel(1036); // French (not present in the label)

        Assert.AreEqual("Company", result);
    }

    [TestMethod]
    public void GetLabel_FallsBackDeterministicallyWhenNeitherTargetNorEnglishPresent()
    {
        var label = BuildLabel(("Société", 1036), ("Firma", 1031));

        var result = label.GetLabel(1040); // Italian (not present in the label)

        Assert.AreEqual("Firma", result);
    }

    // Label's (LocalizedLabel, LocalizedLabel[]) constructor only fills UserLocalizedLabel, not
    // LocalizedLabels - building a Label with multiple LocalizedLabels needs the default
    // constructor plus the (public) LocalizedLabelCollection.Add.
    private static Label BuildLabel(params (string Text, int LanguageCode)[] labels)
    {
        var label = new Label();
        foreach (var (text, languageCode) in labels)
            label.LocalizedLabels.Add(new LocalizedLabel(text, languageCode));
        return label;
    }
}
