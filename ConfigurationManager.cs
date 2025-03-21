using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using poji.Properties;

namespace poji
{
    public class ConfigurationManager
    {
        // Current settings
        public string CurrentShareCode { get; set; }
        public float CurrentScale { get; set; } = 1.0f;
        public int CurrentMonitor { get; set; }
        
        // Paths
        private readonly string _settingsFile;
        private readonly string _hotkeysFile;
        
        // Default values
        private const float DefaultScale = 1.0f;
        private const int DefaultMonitor = 0;
        private const string DefaultShareCode = "";
        
        public ConfigurationManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrosshairOverlay");
                
            _settingsFile = Path.Combine(appDataPath, "settings.xml");
            _hotkeysFile = Path.Combine(appDataPath, "hotkeys.xml");
            
            // Ensure directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            // Set defaults
            CurrentShareCode = DefaultShareCode;
            CurrentScale = DefaultScale;
            CurrentMonitor = DefaultMonitor;
        }
        
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(_settingsFile);
                    
                    XmlNode root = doc.DocumentElement;
                    
                    // Load sharecode
                    XmlNode shareCodeNode = root.SelectSingleNode("//ShareCode");
                    if (shareCodeNode != null)
                    {
                        CurrentShareCode = shareCodeNode.InnerText;
                    }
                    
                    // Load scale
                    XmlNode scaleNode = root.SelectSingleNode("//Scale");
                    if (scaleNode != null && float.TryParse(scaleNode.InnerText, out float scale))
                    {
                        CurrentScale = scale;
                    }
                    
                    // Load monitor
                    XmlNode monitorNode = root.SelectSingleNode("//Monitor");
                    if (monitorNode != null && int.TryParse(monitorNode.InnerText, out int monitor))
                    {
                        CurrentMonitor = monitor;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Console.WriteLine(Resources.ConfigurationManager_LoadSettings_Error_loading_settings___0_, ex.Message);
                
                // Fall back to defaults
                CurrentScale = DefaultScale;
                CurrentMonitor = DefaultMonitor;
                CurrentShareCode = DefaultShareCode;
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
        
        public void SaveAllSettings()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(xmlDeclaration);
                
                XmlElement root = doc.CreateElement("Settings");
                doc.AppendChild(root);
                
                // ShareCode
                XmlElement shareCode = doc.CreateElement("ShareCode");
                shareCode.InnerText = CurrentShareCode ?? "";
                root.AppendChild(shareCode);
                
                // Scale
                XmlElement scale = doc.CreateElement("Scale");
                scale.InnerText = CurrentScale.ToString();
                root.AppendChild(scale);
                
                // Monitor
                XmlElement monitor = doc.CreateElement("Monitor");
                monitor.InnerText = CurrentMonitor.ToString();
                root.AppendChild(monitor);
                
                // Save the document
                doc.Save(_settingsFile);
            }
            catch (Exception ex)
            {
                // Log error but don't disrupt the application
                Console.WriteLine(Resources.ConfigurationManager_SaveAllSettings_Error_saving_settings___0_, ex.Message);
            }
        }
        
        public Dictionary<string, HotkeyBinding> GetHotkeyBindings()
        {
            Dictionary<string, HotkeyBinding> bindings = new Dictionary<string, HotkeyBinding>();
            
            try
            {
                if (File.Exists(_hotkeysFile))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(_hotkeysFile);
                    
                    XmlNodeList hotkeyNodes = doc.SelectNodes("//Hotkey");
                    if (hotkeyNodes != null)
                    {
                        foreach (XmlNode node in hotkeyNodes)
                        {
                            string action = node.Attributes["action"]?.Value;
                            if (string.IsNullOrEmpty(action))
                            {
                                continue;
                            }
                            
                            XmlNode keyNode = node.SelectSingleNode("Key");
                            XmlNode altNode = node.SelectSingleNode("Alt");
                            XmlNode shiftNode = node.SelectSingleNode("Shift");
                            XmlNode ctrlNode = node.SelectSingleNode("Ctrl");
                            
                            if (keyNode != null && 
                                Enum.TryParse(keyNode.InnerText, out Keys key) && 
                                key != Keys.None)
                            {
                                bool alt = altNode != null && bool.TryParse(altNode.InnerText, out bool a) && a;
                                bool shift = shiftNode != null && bool.TryParse(shiftNode.InnerText, out bool s) && s;
                                bool ctrl = ctrlNode != null && bool.TryParse(ctrlNode.InnerText, out bool c) && c;
                                
                                bindings[action] = new HotkeyBinding
                                {
                                    Key = key,
                                    Alt = alt,
                                    Shift = shift,
                                    Ctrl = ctrl
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading hotkey bindings: {ex.Message}");
            }
            
            // Set default bindings for any missing actions
            if (!bindings.ContainsKey("ToggleVisibility"))
            {
                bindings["ToggleVisibility"] = new HotkeyBinding { Key = Keys.H, Alt = true, Shift = true };
            }
            
            if (!bindings.ContainsKey("Exit"))
            {
                bindings["Exit"] = new HotkeyBinding { Key = Keys.W, Alt = true, Shift = true };
            }
            
            if (!bindings.ContainsKey("ReloadCrosshair"))
            {
                bindings["ReloadCrosshair"] = new HotkeyBinding { Key = Keys.R, Alt = true, Shift = true };
            }
            
            if (!bindings.ContainsKey("SwitchMonitor"))
            {
                bindings["SwitchMonitor"] = new HotkeyBinding { Key = Keys.D1, Alt = true, Shift = true };
            }
            
            return bindings;
        }
        
        public void SaveHotkeyBindings(Dictionary<string, HotkeyBinding> bindings)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(xmlDeclaration);
                
                XmlElement root = doc.CreateElement("Hotkeys");
                doc.AppendChild(root);
                
                foreach (var binding in bindings)
                {
                    if (binding.Value == null)
                    {
                        continue;
                    }
                    
                    XmlElement hotkeyElem = doc.CreateElement("Hotkey");
                    hotkeyElem.SetAttribute("action", binding.Key);
                    
                    XmlElement keyElem = doc.CreateElement("Key");
                    keyElem.InnerText = binding.Value.Key.ToString();
                    hotkeyElem.AppendChild(keyElem);
                    
                    XmlElement altElem = doc.CreateElement("Alt");
                    altElem.InnerText = binding.Value.Alt.ToString();
                    hotkeyElem.AppendChild(altElem);
                    
                    XmlElement shiftElem = doc.CreateElement("Shift");
                    shiftElem.InnerText = binding.Value.Shift.ToString();
                    hotkeyElem.AppendChild(shiftElem);
                    
                    XmlElement ctrlElem = doc.CreateElement("Ctrl");
                    ctrlElem.InnerText = binding.Value.Ctrl.ToString();
                    hotkeyElem.AppendChild(ctrlElem);
                    
                    root.AppendChild(hotkeyElem);
                }
                
                doc.Save(_hotkeysFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving hotkey bindings: {ex.Message}");
            }
        }
        
        public void RestoreDefaultSettings()
        {
            // Restore default settings
            CurrentScale = DefaultScale;
            CurrentMonitor = DefaultMonitor;
            CurrentShareCode = DefaultShareCode;
            
            // Restore default hotkey bindings
            Dictionary<string, HotkeyBinding> defaultBindings = new Dictionary<string, HotkeyBinding>
            {
                ["ToggleVisibility"] = new HotkeyBinding { Key = Keys.H, Alt = true, Shift = true },
                ["Exit"] = new HotkeyBinding { Key = Keys.W, Alt = true, Shift = true },
                ["ReloadCrosshair"] = new HotkeyBinding { Key = Keys.R, Alt = true, Shift = true },
                ["SwitchMonitor"] = new HotkeyBinding { Key = Keys.D1, Alt = true, Shift = true }
            };
            
            SaveHotkeyBindings(defaultBindings);
            SaveAllSettings();
        }
    }
}