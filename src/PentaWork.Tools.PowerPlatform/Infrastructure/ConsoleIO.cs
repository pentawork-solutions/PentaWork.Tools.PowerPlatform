using System.Text.Json;

namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// stdin/stdout/file plumbing shared by every command: JSON exports default to stdout, JSON imports
    /// default to stdin, and "-" is always an explicit alias for the matching stream.
    /// </summary>
    internal static class ConsoleIO
    {
        public const string StdStreamAlias = "-";

        public static string ReadInput(string? inputPath)
        {
            if (string.IsNullOrEmpty(inputPath) || inputPath == StdStreamAlias)
                return Console.In.ReadToEnd();

            if (!File.Exists(inputPath))
                throw new CliValidationException($"Input file not found: {inputPath}");

            return File.ReadAllText(inputPath);
        }

        public static T ReadInputJson<T>(string? inputPath)
        {
            var json = ReadInput(inputPath);
            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonSettings.Default)
                       ?? throw new CliValidationException("Input JSON deserialized to null.");
            }
            catch (JsonException ex)
            {
                throw new CliValidationException($"Failed to parse input JSON: {ex.Message}");
            }
        }

        public static void WriteOutput(string? outputPath, string content)
        {
            if (string.IsNullOrEmpty(outputPath) || outputPath == StdStreamAlias)
            {
                Console.Out.Write(content);
                if (!content.EndsWith('\n')) Console.Out.WriteLine();
                return;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, content);
        }

        /// <summary>Folds buffered result objects into the single JSON document that goes to stdout/--output:
        /// zero objects -> "null"; one object -> that object; more -> a JSON array.</summary>
        public static void WriteResult(string? outputPath, IReadOnlyList<object?> objects)
        {
            var payload = objects.Count switch
            {
                0 => null,
                1 => objects[0],
                _ => objects
            };

            WriteOutput(outputPath, JsonSerializer.Serialize(payload, JsonSettings.Default));
        }

        public static void WriteResult(string? outputPath, object? payload) =>
            WriteOutput(outputPath, JsonSerializer.Serialize(payload, JsonSettings.Default));
    }
}
