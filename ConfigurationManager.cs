using System;
using System.IO;
using poji.Properties;

namespace poji
{
    public class ConfigurationManager
    {
        // Current settings
        public string CurrentShareCode { get; private set; }
        public float CurrentScale { get; private set; } = 1.0f;
        public int CurrentMonitor { get; private set; }
        
        // Paths
        private readonly string _settingsFile;
        
        public ConfigurationManager()
        {
            var appDataPath =
                // Setup paths
                Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrosshairOverlay");
                
            _settingsFile = Path.Combine(appDataPath, "settings.txt");
            
            // Ensure directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }
        
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    // Read settings line by line
                    foreach (string line in File.ReadAllLines(_settingsFile))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        
                        switch (key)
                        {
                            case "ShareCode":
                                CurrentShareCode = value;
                                break;
                                
                            case "Scale":
                                if (float.TryParse(value, out float scale))
                                {
                                    CurrentScale = scale;
                                }
                                break;
                                
                            case "Monitor":
                                if (int.TryParse(value, out int monitor))
                                {
                                    CurrentMonitor = monitor;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Console.WriteLine(Resources.ConfigurationManager_LoadSettings_Error_loading_settings___0_, ex.Message);
                
                // Fall back to defaults
                CurrentScale = 1.0f;
                CurrentMonitor = 0;
            }
        }
        
        public void SaveShareCode(string shareCode)
        {
            CurrentShareCode = shareCode;
            SaveAllSettings();
        }
        
        public void SaveScaleSetting(float scale)
        {
            CurrentScale = scale;
            SaveAllSettings();
        }
        
        public void SaveMonitorSetting(int monitorIndex)
        {
            CurrentMonitor = monitorIndex;
            SaveAllSettings();
        }
        
        private void SaveAllSettings()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_settingsFile))
                {
                    writer.WriteLine($"ShareCode={CurrentShareCode}");
                    writer.WriteLine($"Scale={CurrentScale}");
                    writer.WriteLine($"Monitor={CurrentMonitor}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't disrupt the application
                Console.WriteLine(Resources.ConfigurationManager_SaveAllSettings_Error_saving_settings___0_, ex.Message);
            }
        }
    }
}