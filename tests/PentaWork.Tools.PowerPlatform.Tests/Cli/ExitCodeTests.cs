using CliFx;
using CliFx.Infrastructure;
using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

[TestClass]
public class ExitCodeTests
{
    [TestMethod]
    public void Run_MapsCliExceptionsToTheirDocumentedExitCodes()
    {
        using var console = new FakeInMemoryConsole();

        var exception = Assert.ThrowsExactly<CommandException>(() =>
        {
            CommandRunner.Run(false, console, null, _ => throw new CliPartialFailureException("partial failure"));
        });

        Assert.AreEqual((int)ExitCode.PartialBatchFailure, exception.ExitCode);
    }

    [TestMethod]
    public void Run_MapsUnexpectedExceptionsToDataverseOrFileExitCode()
    {
        using var console = new FakeInMemoryConsole();

        var exception = Assert.ThrowsExactly<CommandException>(() =>
        {
            CommandRunner.Run(false, console, null, _ => throw new InvalidOperationException("boom"));
        });

        Assert.AreEqual((int)ExitCode.DataverseOrFile, exception.ExitCode);
    }
}
