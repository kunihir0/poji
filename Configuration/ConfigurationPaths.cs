namespace poji.Configuration
{
    /// <summary>
    /// Contains paths required for the configuration system
    /// </summary>
    internal class ConfigurationPaths
    {
        /// <summary>
        /// Base directory for application data
        /// </summary>
        public string AppDataPath { get; set; }

        /// <summary>
        /// Path to settings XML file
        /// </summary>
        public string SettingsFile { get; set; }

        /// <summary>
        /// Path to hotkeys XML file
        /// </summary>
        public string HotkeysFile { get; set; }
    }
}