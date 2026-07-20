using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

/// <summary>Covers the one guard rail `--clear` (proxy generate, plugin-graph export) keeps even though
/// it otherwise deletes without confirmation: it must refuse to point at the filesystem root or the
/// current workspace root.</summary>
[TestClass]
public class ClearGuardTests
{
    [TestMethod]
    public void EnsureSafeToClear_RejectsFilesystemRoot()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        Assert.ThrowsExactly<CliValidationException>(() => ClearGuard.EnsureSafeToClear(root));
    }

    [TestMethod]
    public void EnsureSafeToClear_RejectsCurrentWorkspaceRoot()
    {
        Assert.ThrowsExactly<CliValidationException>(() => ClearGuard.EnsureSafeToClear(Directory.GetCurrentDirectory()));
    }

    [TestMethod]
    public void EnsureSafeToClear_RejectsCurrentWorkspaceRoot_ViaDotPath()
    {
        Assert.ThrowsExactly<CliValidationException>(() => ClearGuard.EnsureSafeToClear("."));
    }

    [TestMethod]
    public void EnsureSafeToClear_AllowsAnOrdinarySubdirectory()
    {
        var subdirectory = Path.Combine(Path.GetTempPath(), "pw-pp-cleartest-" + Guid.NewGuid());
        // Should not throw.
        ClearGuard.EnsureSafeToClear(subdirectory);
    }

    [TestMethod]
    public void EnsureSafeToClear_AllowsWorkspaceSubdirectory()
    {
        var subdirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");
        // Should not throw - only the root itself is rejected, not paths under it.
        ClearGuard.EnsureSafeToClear(subdirectory);
    }
}
