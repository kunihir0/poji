using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using poji.Models;
using poji.Properties;
using poji.Enums;
using poji.Interfaces;
using System.Windows.Forms;
using poji.Services;

namespace poji.Configuration
{
    /// <summary>
    /// Manages application configuration settings, including user preferences and hotkey bindings.
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        private readonly IFileService _fileService;
        private readonly ILogService _logService;
        private readonly ConfigurationPaths _paths;

        // Default configuration values
        private static class Defaults
        {
            public const float Scale = 1.0f;
            public const int Monitor = 0;
            public const string ShareCode = "CSGO-TpORA-p9Ley-TLQ3P-HzXJY-U9z6A";
        }

        #region Properties

        public string ShareCode { get; private set; }
        public float Scale { get; private set; }
        public int MonitorIndex { get; private set; }

        #endregion

        public ConfigurationManager(IFileService fileService = null, ILogService logService = null)
        {
            _fileService = fileService ?? new FileService();
            _logService = logService ?? new ConsoleLogService();
            _paths = InitializeConfigurationPaths();

            InitializeDefaultSettings();
        }

        #region Public Methods

        public void LoadSettings()
        {
            if (!_fileService.FileExists(_paths.SettingsFile))
            {
                return;
            }

            try
            {
                var document = LoadXmlDocument(_paths.SettingsFile);

                if (document?.DocumentElement == null)
                {
                    return;
                }

                LoadShareCode(document);
                LoadScale(document);
                LoadMonitorIndex(document);
            }
            catch (Exception ex)
            {
                _logService.LogError(Resources.ConfigurationManager_LoadSettings_Error_loading_settings___0_, ex.Message);
                ResetToDefaults();
            }
        }

        public void SaveShareCode(string shareCode)
        {
            ShareCode = shareCode;
            SaveAllSettings();
        }

        public void SaveScale(float scale)
        {
            Scale = scale;
            SaveAllSettings();
        }

        public void SaveMonitorIndex(int monitorIndex)
        {
            MonitorIndex = monitorIndex;
            SaveAllSettings();
        }

        public void SaveAllSettings()
        {
            try
            {
                var document = CreateSettingsXmlDocument();
                document.Save(_paths.SettingsFile);
            }
            catch (Exception ex)
            {
                _logService.LogError(Resources.ConfigurationManager_SaveAllSettings_Error_saving_settings___0_, ex.Message);
            }
        }

        public Dictionary<HotkeyAction, HotkeyBinding> GetHotkeyBindings()
        {
            var bindings = new Dictionary<HotkeyAction, HotkeyBinding>();

            try
            {
                if (_fileService.FileExists(_paths.HotkeysFile))
                {
                    LoadHotkeyBindingsFromFile(bindings);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("Error loading hotkey bindings: {0}", ex.Message);
            }

            EnsureDefaultHotkeyBindings(bindings);

            return bindings;
        }

        public void SaveHotkeyBindings(Dictionary<HotkeyAction, HotkeyBinding> bindings)
        {
            try
            {
                var document = CreateHotkeysXmlDocument(bindings);
                document.Save(_paths.HotkeysFile);
            }
            catch (Exception ex)
            {
                _logService.LogError("Error saving hotkey bindings: {0}", ex.Message);
            }
        }

        public void RestoreDefaultSettings()
        {
            ResetToDefaults();
            SaveHotkeyBindings(CreateDefaultHotkeyBindings());
            SaveAllSettings();
        }

        #endregion

        #region Private Methods

        private ConfigurationPaths InitializeConfigurationPaths()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrosshairOverlay");

            var paths = new ConfigurationPaths
            {
                AppDataPath = appDataPath,
                SettingsFile = Path.Combine(appDataPath, "settings.xml"),
                HotkeysFile = Path.Combine(appDataPath, "hotkeys.xml")
            };

            EnsureDirectoryExists(paths.AppDataPath);

            return paths;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!_fileService.DirectoryExists(path))
            {
                _fileService.CreateDirectory(path);
            }
        }

        private void InitializeDefaultSettings()
        {
            ShareCode = Defaults.ShareCode;
            Scale = Defaults.Scale;
            MonitorIndex = Defaults.Monitor;
        }

        private void ResetToDefaults()
        {
            ShareCode = Defaults.ShareCode;
            Scale = Defaults.Scale;
            MonitorIndex = Defaults.Monitor;
        }

        private XmlDocument LoadXmlDocument(string path)
        {
            var document = new XmlDocument();
            document.Load(path);
            return document;
        }

        private void LoadShareCode(XmlDocument document)
        {
            var shareCodeNode = document.DocumentElement.SelectSingleNode("//ShareCode");
            if (shareCodeNode != null)
            {
                ShareCode = shareCodeNode.InnerText;
            }
        }

        private void LoadScale(XmlDocument document)
        {
            var scaleNode = document.DocumentElement.SelectSingleNode("//Scale");
            if (scaleNode != null && float.TryParse(scaleNode.InnerText, out float scale))
            {
                Scale = scale;
            }
        }

        private void LoadMonitorIndex(XmlDocument document)
        {
            var monitorNode = document.DocumentElement.SelectSingleNode("//Monitor");
            if (monitorNode != null && int.TryParse(monitorNode.InnerText, out int monitor))
            {
                MonitorIndex = monitor;
            }
        }

        private XmlDocument CreateSettingsXmlDocument()
        {
            var document = new XmlDocument();

            var declaration = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            document.AppendChild(declaration);

            var root = document.CreateElement("Settings");
            document.AppendChild(root);

            // ShareCode
            var shareCodeElement = document.CreateElement("ShareCode");
            shareCodeElement.InnerText = ShareCode ?? string.Empty;
            root.AppendChild(shareCodeElement);

            // Scale
            var scaleElement = document.CreateElement("Scale");
            scaleElement.InnerText = Scale.ToString();
            root.AppendChild(scaleElement);

            // Monitor
            var monitorElement = document.CreateElement("Monitor");
            monitorElement.InnerText = MonitorIndex.ToString();
            root.AppendChild(monitorElement);

            return document;
        }

        private void LoadHotkeyBindingsFromFile(Dictionary<HotkeyAction, HotkeyBinding> bindings)
        {
            var document = LoadXmlDocument(_paths.HotkeysFile);
            var hotkeyNodes = document.SelectNodes("//Hotkey");

            if (hotkeyNodes == null)
            {
                return;
            }

            foreach (XmlNode node in hotkeyNodes)
            {
                var actionAttribute = node.Attributes?["action"]?.Value;
                if (string.IsNullOrEmpty(actionAttribute) ||
                    !Enum.TryParse(actionAttribute, out HotkeyAction action))
                {
                    continue;
                }

                var binding = CreateHotkeyBindingFromNode(node);
                if (binding != null)
                {
                    bindings[action] = binding;
                }
            }
        }

        private HotkeyBinding CreateHotkeyBindingFromNode(XmlNode node)
        {
            var keyNode = node.SelectSingleNode("Key");
            if (keyNode == null || !Enum.TryParse(keyNode.InnerText, out Keys key) || key == Keys.None)
            {
                return null;
            }

            return new HotkeyBinding
            {
                Key = key,
                Alt = ParseBooleanNode(node.SelectSingleNode("Alt")),
                Shift = ParseBooleanNode(node.SelectSingleNode("Shift")),
                Ctrl = ParseBooleanNode(node.SelectSingleNode("Ctrl"))
            };
        }

        private bool ParseBooleanNode(XmlNode node)
        {
            return node != null && bool.TryParse(node.InnerText, out bool value) && value;
        }

        private void EnsureDefaultHotkeyBindings(Dictionary<HotkeyAction, HotkeyBinding> bindings)
        {
            var defaults = CreateDefaultHotkeyBindings();

            foreach (var action in Enum.GetValues(typeof(HotkeyAction)))
            {
                var hotkeyAction = (HotkeyAction)action;
                if (!bindings.ContainsKey(hotkeyAction) && defaults.ContainsKey(hotkeyAction))
                {
                    bindings[hotkeyAction] = defaults[hotkeyAction];
                }
            }
        }

        private Dictionary<HotkeyAction, HotkeyBinding> CreateDefaultHotkeyBindings()
        {
            return new Dictionary<HotkeyAction, HotkeyBinding>
            {
                [HotkeyAction.ToggleVisibility] = new HotkeyBinding { Key = Keys.H, Alt = true, Shift = true },
                [HotkeyAction.Exit] = new HotkeyBinding { Key = Keys.W, Alt = true, Shift = true },
                [HotkeyAction.ReloadCrosshair] = new HotkeyBinding { Key = Keys.R, Alt = true, Shift = true },
                [HotkeyAction.SwitchMonitor] = new HotkeyBinding { Key = Keys.D1, Alt = true, Shift = true }
            };
        }

        private XmlDocument CreateHotkeysXmlDocument(Dictionary<HotkeyAction, HotkeyBinding> bindings)
        {
            var document = new XmlDocument();

            var declaration = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            document.AppendChild(declaration);

            var root = document.CreateElement("Hotkeys");
            document.AppendChild(root);

            foreach (var binding in bindings)
            {
                if (binding.Value == null)
                {
                    continue;
                }

                var hotkeyElement = document.CreateElement("Hotkey");
                hotkeyElement.SetAttribute("action", binding.Key.ToString());

                AppendHotkeyBindingElements(document, hotkeyElement, binding.Value);

                root.AppendChild(hotkeyElement);
            }

            return document;
        }

        private void AppendHotkeyBindingElements(XmlDocument document, XmlElement parent, HotkeyBinding binding)
        {
            var keyElement = document.CreateElement("Key");
            keyElement.InnerText = binding.Key.ToString();
            parent.AppendChild(keyElement);

            var altElement = document.CreateElement("Alt");
            altElement.InnerText = binding.Alt.ToString();
            parent.AppendChild(altElement);

            var shiftElement = document.CreateElement("Shift");
            shiftElement.InnerText = binding.Shift.ToString();
            parent.AppendChild(shiftElement);

            var ctrlElement = document.CreateElement("Ctrl");
            ctrlElement.InnerText = binding.Ctrl.ToString();
            parent.AppendChild(ctrlElement);
        }

        #endregion
    }
}