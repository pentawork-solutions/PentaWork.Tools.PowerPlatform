namespace PentaWork.Tools.PowerPlatform.Dtos
{
    /// <summary>An additional mapping condition used by `relation import --map-related-by-name`, e.g. to
    /// require the related entity's business unit to match the relating entity's business unit.</summary>
    public class RelationCondition
    {
        public string SchemaName { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string SourceField { get; set; } = string.Empty;
    }
}
