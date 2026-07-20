using System.Text.Json;

namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// Shared JSON contract for the whole CLI: PascalCase property names (no naming policy), SDK enums
    /// serialized as their numeric value (System.Text.Json's default for enums, so no converter is
    /// registered), invariant-culture numbers/dates (System.Text.Json's default), and stdout-friendly
    /// pretty printing.
    /// </summary>
    internal static class JsonSettings
    {
        public static JsonSerializerOptions Default { get; } = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
        };
    }
}
