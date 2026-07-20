namespace PentaWork.Tools.PowerPlatform.Dtos
{
    /// <summary>One user-owned view, dashboard or chart.</summary>
    public class UserObjectInfo
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public Guid ObjectId { get; set; }
        public string ObjectName { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
    }
}
