namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// `--clear` recursively deletes a directory before regenerating it (proxy generate, plugin-graph
    /// export). That is destructive enough that it must never be pointed - accidentally or via a bad
    /// script - at the filesystem root or the current workspace root.
    /// </summary>
    internal static class ClearGuard
    {
        public static void EnsureSafeToClear(string path)
        {
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var root = Path.GetPathRoot(fullPath)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty;
            if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
                throw new CliValidationException($"Refusing to --clear '{path}': it resolves to the filesystem root.");

            var workspaceRoot = Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(fullPath, workspaceRoot, StringComparison.OrdinalIgnoreCase))
                throw new CliValidationException($"Refusing to --clear '{path}': it resolves to the current workspace root.");
        }
    }
}
