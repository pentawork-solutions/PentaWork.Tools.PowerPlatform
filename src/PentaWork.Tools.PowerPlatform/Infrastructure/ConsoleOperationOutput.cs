namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// Buffers WriteObject payloads (flushed as a single JSON document by the caller once the operation
    /// completes) and writes warnings/verbose/progress straight to stderr, keeping stdout reserved for
    /// pure result data as required by the CLI contract.
    /// </summary>
    internal sealed class ConsoleOperationOutput : IOperationOutput
    {
        private readonly bool _verbose;
        private readonly TextWriter _error;
        private readonly List<object?> _objects = new();

        public ConsoleOperationOutput(bool verbose, TextWriter? error = null)
        {
            _verbose = verbose;
            _error = error ?? Console.Error;
        }

        public IReadOnlyList<object?> Objects => _objects;

        public void WriteObject(object? value) => _objects.Add(value);

        public void WriteWarning(string message) => _error.WriteLine($"WARNING: {message}");

        public void WriteVerbose(string message)
        {
            if (_verbose) _error.WriteLine($"VERBOSE: {message}");
        }

        public void WriteProgress(string activity, string status, int? percentComplete = null, bool completed = false)
        {
            var percent = percentComplete.HasValue ? $" ({percentComplete}%)" : string.Empty;
            _error.WriteLine($"PROGRESS: {activity} - {status}{percent}");
        }
    }
}
