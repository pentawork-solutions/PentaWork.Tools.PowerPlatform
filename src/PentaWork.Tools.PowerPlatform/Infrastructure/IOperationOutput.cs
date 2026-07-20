namespace PentaWork.Tools.PowerPlatform.Infrastructure
{
    /// <summary>
    /// Small host-neutral output abstraction: operations report results/warnings/progress through this
    /// instead of writing to the console directly, so the same operation logic can run under any host
    /// (the CLI today, tests tomorrow).
    /// </summary>
    internal interface IOperationOutput
    {
        /// <summary>Emits a result object. Can be called once per produced item - the host decides how to
        /// fold buffered items into the final payload.</summary>
        void WriteObject(object? value);

        void WriteWarning(string message);

        void WriteVerbose(string message);

        void WriteProgress(string activity, string status, int? percentComplete = null, bool completed = false);
    }
}
