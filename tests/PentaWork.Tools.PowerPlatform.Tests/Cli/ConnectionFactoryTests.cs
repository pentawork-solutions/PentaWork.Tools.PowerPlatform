using PentaWork.Tools.PowerPlatform.Infrastructure;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

/// <summary>Covers --connection-string vs PW_PP_CONNECTION_STRING precedence and secret redaction for
/// logging, without needing a live Dataverse connection.</summary>
[TestClass]
public class ConnectionFactoryTests
{
    [TestInitialize]
    [TestCleanup]
    public void ClearEnvironmentVariable() =>
        Environment.SetEnvironmentVariable(ConnectionFactory.ConnectionStringEnvironmentVariable, null);

    [TestMethod]
    public void ResolveConnectionString_PrefersExplicitOptionOverEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(ConnectionFactory.ConnectionStringEnvironmentVariable, "AuthType=OAuth;Url=https://env.crm.dynamics.com");

        var resolved = ConnectionFactory.ResolveConnectionString("AuthType=OAuth;Url=https://explicit.crm.dynamics.com");

        Assert.AreEqual("AuthType=OAuth;Url=https://explicit.crm.dynamics.com", resolved);
    }

    [TestMethod]
    public void ResolveConnectionString_FallsBackToEnvironmentVariable_WhenOptionOmitted()
    {
        Environment.SetEnvironmentVariable(ConnectionFactory.ConnectionStringEnvironmentVariable, "AuthType=OAuth;Url=https://env.crm.dynamics.com");

        var resolved = ConnectionFactory.ResolveConnectionString(null);

        Assert.AreEqual("AuthType=OAuth;Url=https://env.crm.dynamics.com", resolved);
    }

    [TestMethod]
    public void ResolveConnectionString_FallsBackToEnvironmentVariable_WhenOptionIsWhitespace()
    {
        Environment.SetEnvironmentVariable(ConnectionFactory.ConnectionStringEnvironmentVariable, "AuthType=OAuth;Url=https://env.crm.dynamics.com");

        var resolved = ConnectionFactory.ResolveConnectionString("   ");

        Assert.AreEqual("AuthType=OAuth;Url=https://env.crm.dynamics.com", resolved);
    }

    [TestMethod]
    public void ResolveConnectionString_ThrowsValidationException_WhenNeitherIsSet()
    {
        var ex = Assert.ThrowsExactly<CliValidationException>(() => ConnectionFactory.ResolveConnectionString(null));
        StringAssert.Contains(ex.Message, "PW_PP_CONNECTION_STRING");
    }

    [TestMethod]
    [DataRow("AuthType=OAuth;Url=https://org.crm.dynamics.com;Username=me@org.com;Password=hunter2",
              "AuthType=OAuth;Url=https://org.crm.dynamics.com;Username=me@org.com;Password=***")]
    [DataRow("AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=abc;ClientSecret=topsecret",
              "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=abc;ClientSecret=***")]
    public void Redact_MasksOnlySecretBearingSegments(string input, string expected)
    {
        Assert.AreEqual(expected, ConnectionFactory.Redact(input));
    }

    [TestMethod]
    public void Redact_LeavesNonSecretConnectionStringsUnchanged()
    {
        const string connectionString = "AuthType=OAuth;Url=https://org.crm.dynamics.com";
        Assert.AreEqual(connectionString, ConnectionFactory.Redact(connectionString));
    }
}
