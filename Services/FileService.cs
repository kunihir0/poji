using System.IO;
using poji.Interfaces;

namespace poji.Services
{
    /// <summary>
    /// Service for file system operations
    /// </summary>
    public class FileService : IFileService
    {
        /// <summary>
        /// Checks if a file exists
        /// </summary>
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Creates a directory
        /// </summary>
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}