using System;

namespace HaruhiChokuretsuLib.Util
{
    /// <summary>
    /// Logger interface for logging
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log basic message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void Log(string message);
        /// <summary>
        /// Log error message
        /// </summary>
        /// <param name="message">Error message to log</param>
        /// <param name="lookForWarnings">If true, looks for WARN or WARNING and treats those as warnings rather than errors</param>
        public void LogError(string message, bool lookForWarnings = false);
        /// <summary>
        /// Log warning message
        /// </summary>
        /// <param name="message">Warning message to log</param>
        /// <param name="lookForErrors">If true, looks for ERROR and treats those messages as errors rather than warnings</param>
        public void LogWarning(string message, bool lookForErrors = false);
        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception to log</param>
        public void LogException(string message, Exception exception);
    }
}
