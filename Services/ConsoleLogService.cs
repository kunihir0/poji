using System;
using poji.Interfaces;

namespace poji.Services
{
    /// <summary>
    /// Console-based logging service implementation
    /// </summary>
    public class ConsoleLogService : ILogService
    {
        /// <summary>
        /// Logs an error message
        /// </summary>
        public void LogError(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public void LogInfo(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }
    }
}