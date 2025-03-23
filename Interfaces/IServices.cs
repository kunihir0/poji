namespace poji.Interfaces
{
    /// <summary>
    /// Interface for file system operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Checks if a file exists
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// Creates a directory
        /// </summary>
        void CreateDirectory(string path);
    }

    /// <summary>
    /// Interface for logging services
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Logs an error message
        /// </summary>
        void LogError(string message, params object[] args);

        /// <summary>
        /// Logs an informational message
        /// </summary>
        void LogInfo(string message, params object[] args);
    }
}