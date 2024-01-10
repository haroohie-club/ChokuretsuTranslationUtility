using System;

namespace HaruhiChokuretsuLib.Util
{
    /// <summary>
    /// ILogger implementation for logging directly to the console
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <inheritdoc/>
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        /// <inheritdoc/>
        public void LogError(string message, bool lookForWarnings = false)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (lookForWarnings && message.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    LogWarning(message);
                    return;
                }
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"ERROR: {message}");
                Console.ForegroundColor = oldColor;
            }
            else
            {
                Console.WriteLine();
            }
        }

        /// <inheritdoc/>
        public void LogWarning(string message, bool lookForErrors = false)
        {
            {
                if (!string.IsNullOrEmpty(message))
                {
                    if (lookForErrors && message.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        LogError(message);
                        return;
                    }
                    ConsoleColor oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"WARNING: {message}");
                    Console.ForegroundColor = oldColor;
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }

        /// <inheritdoc/>
        public void LogException(string message, Exception exception)
        {
            LogError($"{message}: {exception.Message}\n\n{exception.StackTrace}");
        }
    }
}
