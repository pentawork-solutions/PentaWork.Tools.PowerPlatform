namespace PentaWork.Tools.PowerPlatform.Dtos
{
    /// <summary>Name, unique name, version and publisher of one installed, visible solution.</summary>
    public class SolutionInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string UniqueName { get; set; } = string.Empty;
        public Version Version { get; set; } = new(0, 0, 0, 0);
        public string Publisher { get; set; } = string.Empty;
    }
}
