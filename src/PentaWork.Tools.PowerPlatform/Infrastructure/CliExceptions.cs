namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>Exit codes for the pw-pp CLI. 0 success, 2 validation, 3 connection, 4 Dataverse/file, 5 partial batch failure.</summary>
    internal enum ExitCode
    {
        Success = 0,
        Validation = 2,
        Connection = 3,
        DataverseOrFile = 4,
        PartialBatchFailure = 5
    }

    /// <summary>Base for exceptions that carry a specific CLI exit code.</summary>
    internal abstract class CliException : Exception
    {
        protected CliException(ExitCode exitCode, string message, Exception? inner = null) : base(message, inner)
        {
            ExitCode = exitCode;
        }

        public ExitCode ExitCode { get; }
    }

    /// <summary>Bad arguments/options - a request that never should have reached Dataverse. Exit code 2.</summary>
    internal sealed class CliValidationException : CliException
    {
        public CliValidationException(string message) : base(ExitCode.Validation, message)
        {
        }
    }

    /// <summary>Could not establish or use the Dataverse connection itself. Exit code 3.</summary>
    internal sealed class CliConnectionException : CliException
    {
        public CliConnectionException(string message, Exception? inner = null) : base(ExitCode.Connection, message, inner)
        {
        }
    }

    /// <summary>A Dataverse operation or file-system operation failed. Exit code 4.</summary>
    internal sealed class CliDataException : CliException
    {
        public CliDataException(string message, Exception? inner = null) : base(ExitCode.DataverseOrFile, message, inner)
        {
        }
    }

    /// <summary>A batch operation completed, but some items failed (ExecuteMultiple-style partial failure). Exit code 5.</summary>
    internal sealed class CliPartialFailureException : CliException
    {
        public CliPartialFailureException(string message) : base(ExitCode.PartialBatchFailure, message)
        {
        }
    }
}
