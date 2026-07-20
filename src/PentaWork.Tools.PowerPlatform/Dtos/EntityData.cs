using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace PentaWork.Tools.PowerPlatform.Dtos
{
    /// <summary>Holds exported entities of one entity type plus export metadata. Round-trips through
    /// `entity export`/`entity import`/`relation import`/`sharing import`.</summary>
    public class EntityData
    {
        public string EntityName { get; set; } = string.Empty;
        public string? PrimaryIdField { get; set; }
        public string? PrimaryNameField { get; set; }
        public string? ExportedFrom { get; set; }
        public DateTime ExportedOn { get; set; }
        public EntityInfo[] Entities { get; set; } = [];
    }

    /// <summary>Holds all writable attribute values, the id and name of one exported entity instance.</summary>
    public class EntityInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AttributeInfo[] Attributes { get; set; } = [];
        public RelationInfo[] Relations { get; set; } = [];
        public ShareInfo[] Sharings { get; set; } = [];
    }

    /// <summary>Holds metadata for one exported attribute plus its serialized value.</summary>
    public class AttributeInfo
    {
        public string Name { get; set; } = string.Empty;
        public AttributeTypeCode? Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool ValidForCreate { get; set; }
        public bool ValidForUpdate { get; set; }
    }

    /// <summary>Holds metadata for one exported N:M relation.</summary>
    public class RelationInfo
    {
        public string SchemaName { get; set; } = string.Empty;
        public string IntersectEntityName { get; set; } = string.Empty;
        public string ToLogicalName { get; set; } = string.Empty;
        public string? ToPrimaryNameAttribute { get; set; }
        public string ToPrimaryIdAttribute { get; set; } = string.Empty;
        public string FromPrimaryIdAttribute { get; set; } = string.Empty;
        public RelatedEntityInfo[] Entities { get; set; } = [];
    }

    /// <summary>A related entity reference (id + display name).</summary>
    public class RelatedEntityInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Holds one sharing (POA) entry for an exported entity instance.</summary>
    public class ShareInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LogicalName { get; set; } = string.Empty;
        public AccessRights AccessMask { get; set; }
    }
}
