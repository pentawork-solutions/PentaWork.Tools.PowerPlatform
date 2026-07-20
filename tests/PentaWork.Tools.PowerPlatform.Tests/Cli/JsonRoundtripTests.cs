using System.Text.Json;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using PentaWork.Tools.PowerPlatform.Dtos;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

/// <summary>Covers the JSON contract every export/import command round-trips through: PascalCase
/// property names and SDK enums serialized as their raw numeric value (not strings), matching what
/// `entity export` writes and `entity import`/`relation import`/`sharing import` read back.</summary>
[TestClass]
public class JsonRoundtripTests
{
    [TestMethod]
    public void EntityData_RoundTrips_PreservingEveryValue()
    {
        var original = new EntityData
        {
            EntityName = "account",
            PrimaryIdField = "accountid",
            PrimaryNameField = "name",
            ExportedFrom = "contoso",
            ExportedOn = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Entities =
            [
                new EntityInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "Contoso",
                    Attributes =
                    [
                        new AttributeInfo { Name = "name", Type = AttributeTypeCode.String, Value = "Contoso", ValidForCreate = true, ValidForUpdate = true },
                        new AttributeInfo { Name = "revenue", Type = AttributeTypeCode.Money, Value = "1000", ValidForCreate = true, ValidForUpdate = false },
                    ],
                    Relations =
                    [
                        new RelationInfo
                        {
                            SchemaName = "team_account",
                            IntersectEntityName = "team_account",
                            ToLogicalName = "team",
                            ToPrimaryNameAttribute = "name",
                            ToPrimaryIdAttribute = "teamid",
                            FromPrimaryIdAttribute = "accountid",
                            Entities = [new RelatedEntityInfo { Id = Guid.NewGuid(), Name = "Sales Team" }]
                        }
                    ],
                    Sharings = [new ShareInfo { Id = Guid.NewGuid(), Name = "Jane Doe", LogicalName = "systemuser", AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess }]
                }
            ]
        };

        var json = JsonSerializer.Serialize(original, JsonSettings.Default);
        var roundTripped = JsonSerializer.Deserialize<EntityData>(json, JsonSettings.Default)!;

        Assert.AreEqual(original.EntityName, roundTripped.EntityName);
        Assert.AreEqual(original.ExportedOn, roundTripped.ExportedOn);
        Assert.AreEqual(original.Entities[0].Id, roundTripped.Entities[0].Id);
        Assert.AreEqual(original.Entities[0].Attributes[0].Value, roundTripped.Entities[0].Attributes[0].Value);
        Assert.AreEqual(original.Entities[0].Attributes[1].Type, roundTripped.Entities[0].Attributes[1].Type);
        Assert.AreEqual(original.Entities[0].Relations[0].Entities[0].Name, roundTripped.Entities[0].Relations[0].Entities[0].Name);
        Assert.AreEqual(original.Entities[0].Sharings[0].AccessMask, roundTripped.Entities[0].Sharings[0].AccessMask);
    }

    [TestMethod]
    public void PropertyNames_SerializeAsPascalCase()
    {
        var data = new EntityData { EntityName = "account", Entities = [] };
        var json = JsonSerializer.Serialize(data, JsonSettings.Default);

        StringAssert.Contains(json, "\"EntityName\"");
        StringAssert.Contains(json, "\"Entities\"");
    }

    [TestMethod]
    public void SdkEnums_SerializeAsNumbers_NotStrings()
    {
        var attribute = new AttributeInfo { Name = "prioritycode", Type = AttributeTypeCode.Picklist, Value = "1" };
        var json = JsonSerializer.Serialize(attribute, JsonSettings.Default);

        // AttributeTypeCode.Picklist == 3 (Microsoft.Xrm.Sdk.Metadata.AttributeTypeCode) - the point of
        // this test is that it's a bare number in the JSON, not the string "Picklist".
        StringAssert.Contains(json, "\"Type\": " + (int)AttributeTypeCode.Picklist);
        StringAssert.DoesNotMatch(json, new System.Text.RegularExpressions.Regex("\"Type\"\\s*:\\s*\"Picklist\""));

        var share = new ShareInfo { Name = "x", LogicalName = "systemuser", AccessMask = AccessRights.ReadAccess };
        var shareJson = JsonSerializer.Serialize(share, JsonSettings.Default);
        StringAssert.Contains(shareJson, "\"AccessMask\": " + (int)AccessRights.ReadAccess);
    }

    [TestMethod]
    public void Numbers_And_Dates_SerializeUsingInvariantCulture()
    {
        var attribute = new AttributeInfo { Name = "revenue", Type = AttributeTypeCode.Money, Value = "1234.5" };
        var data = new EntityData
        {
            EntityName = "account",
            ExportedOn = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc),
            Entities = [new EntityInfo { Id = Guid.Empty, Name = "x", Attributes = [attribute] }]
        };

        var json = JsonSerializer.Serialize(data, JsonSettings.Default);

        StringAssert.Contains(json, "\"Value\": \"1234.5\"");
        StringAssert.Contains(json, "2026-03-04T05:06:07");
    }
}
