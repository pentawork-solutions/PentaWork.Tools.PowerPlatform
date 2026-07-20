using PentaWork.Tools.PowerPlatform.Infrastructure;
using PentaWork.Tools.PowerPlatform.Workflows;

namespace PentaWork.Tools.PowerPlatform.Tests.Workflows;

[TestClass]
public class DataWorkflowConfigurationTests
{
    private static string WriteTempJson(string json)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        return path;
    }

    [TestMethod]
    public void ValidConfiguration_LoadsSuccessfully()
    {
        var json = """
        {
            "Items": [
                {
                    "FileName": "accounts",
                    "LogicalName": "account",
                    "Filters": ["statecode eq 0"],
                    "IgnoreColumns": ["revenue"],
                    "Relations": ["account_contacts"],
                    "Sharings": true,
                    "MapByName": true,
                    "MapByNameLookups": ["ownerid"],
                    "CreateOnly": ["name"],
                    "ImpersonateOwner": false,
                    "PurgeTarget": false
                }
            ]
        }
        """;

        var path = WriteTempJson(json);
        try
        {
            var config = DataWorkflowConfigurationParser.Load(path);
            Assert.IsNotNull(config);
            Assert.AreEqual(1, config.Items.Count);
            Assert.AreEqual("accounts", config.Items[0].FileName);
            Assert.AreEqual("account", config.Items[0].LogicalName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void MissingOptionalArrays_AreNormalizedToEmptyLists()
    {
        var path = WriteTempJson("""{ "Items": [{ "FileName": "accounts", "LogicalName": "account" }] }""");
        try
        {
            var item = DataWorkflowConfigurationParser.Load(path).Items.Single();

            Assert.HasCount(0, item.Filters);
            Assert.HasCount(0, item.IgnoreColumns);
            Assert.HasCount(0, item.Relations);
            Assert.HasCount(0, item.MapByNameLookups);
            Assert.HasCount(0, item.CreateOnly);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void EmptyItems_ThrowsValidationError()
    {
        var json = """{ "Items": [] }""";
        var path = WriteTempJson(json);
        try
        {
            var ex = Assert.ThrowsExactly<CliValidationException>(
                () => DataWorkflowConfigurationParser.Load(path));
            Assert.AreEqual(
                "Data workflow configuration must contain at least one item.",
                ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ItemWithEmptyFileName_ThrowsValidationError()
    {
        var json = """
        {
            "Items": [
                {
                    "FileName": "",
                    "LogicalName": "account"
                }
            ]
        }
        """;
        var path = WriteTempJson(json);
        try
        {
            var ex = Assert.ThrowsExactly<CliValidationException>(
                () => DataWorkflowConfigurationParser.Load(path));
            Assert.AreEqual(
                "Data workflow item at index 0 has an empty FileName.",
                ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void FileNameWithDirectorySeparator_ThrowsValidationError()
    {
        var json = """
        {
            "Items": [
                {
                    "FileName": "subdir/accounts",
                    "LogicalName": "account"
                }
            ]
        }
        """;
        var path = WriteTempJson(json);
        try
        {
            var ex = Assert.ThrowsExactly<CliValidationException>(
                () => DataWorkflowConfigurationParser.Load(path));
            Assert.AreEqual(
                "Data workflow item FileName 'subdir/accounts' must not contain a directory separator.",
                ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void DuplicateFileNames_CaseInsensitive_ThrowsValidationError()
    {
        var json = """
        {
            "Items": [
                {
                    "FileName": "Accounts",
                    "LogicalName": "account"
                },
                {
                    "FileName": "accounts",
                    "LogicalName": "contact"
                }
            ]
        }
        """;
        var path = WriteTempJson(json);
        try
        {
            var ex = Assert.ThrowsExactly<CliValidationException>(
                () => DataWorkflowConfigurationParser.Load(path));
            Assert.AreEqual(
                "Data workflow configuration contains duplicate FileName 'accounts'.",
                ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
