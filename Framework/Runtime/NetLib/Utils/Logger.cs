using System;
using UnityEngine;

namespace NetLib.Utils
{
    /// <summary>
    /// Logger used by the framework to dispatch Unity logs.
    /// </summary>
    /// <remarks>
    /// If you do not wish to show log messages from the framework in the unity console you can set the log level
    /// with the <see cref="Verbosity"/> option. Error logs are always shown.
    /// </remarks>
    public static class Logger
    {
        /// <summary>
        /// The verbosity of the logger.
        /// </summary>
        /// <remarks>
        /// <see cref="LogLevel.Debug"/> will show all log messages,
        /// <see cref="LogLevel.Warning"/> will show warning and error messages,
        /// <see cref="LogLevel.Error"/> will only show error messages.
        /// <see cref="LogLevel.None"/> will disable all messages (not recommended).
        /// </remarks>
        public static LogLevel Verbosity { set; private get; }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="caller">The object which called the logger.</param>
        /// <param name="text">The debug message.</param>
        public static void Log(string caller, string text)
        {
            if (Verbosity == LogLevel.Debug)
                Debug.LogFormat("LOG | {2} | {0:hh:mm:ss}: {1}", DateTime.Now, text, caller);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="caller">The object which called the logger.</param>
        /// <param name="text">The warning message.</param>
        public static void LogWarning(string caller, string text)
        {
            if (Verbosity == LogLevel.Debug || Verbosity == LogLevel.Warning)
                Debug.LogWarningFormat("WARNING | {2} | {0:hh:mm:ss}: {1}", DateTime.Now, text, caller);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="caller">The object which called the logger.</param>
        /// <param name="text">The error message.</param>
        public static void LogError(string caller, string text)
        {
            if (Verbosity == LogLevel.Debug || Verbosity == LogLevel.Warning || Verbosity == LogLevel.Error)
                Debug.LogErrorFormat("ERROR | {2} | {0:hh:mm:ss}: {1}", DateTime.Now, text, caller);
        }

        public enum LogLevel
        {
            Debug,
            Warning,
            Error,
            None
        }
    }
}
