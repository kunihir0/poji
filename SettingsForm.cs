using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using poji.Configuration;
using poji.Controls;
using poji.Models;
using poji.Enums;
using poji.Controls.Extensions;

namespace poji
{
    public partial class SettingsForm : Form
    {
        private ConfigurationManager _configManager;
        private HotkeyManager _hotkeyManager;
        private Dictionary<poji.Enums.HotkeyAction, HotkeyBinding> _originalBindings = new Dictionary<poji.Enums.HotkeyAction, HotkeyBinding>();
        private HotkeyControl _activeHotkeyControl;

        // Control references
        private ComboBox _scaleDropdown;
        private ComboBox _monitorDropdown;
        private TextBox _crosshairTextBox;
        private TableLayoutPanel _hotkeysLayout;
        private Button _saveButton;
        private Button _resetButton;
        private Button _cancelButton;
        private Button _applyButton;

        private const float DEFAULT_SCALE = 1.0f;
        private const string TOGGLE_VISIBILITY = "ToggleVisibility";
        private const string EXIT_ACTION = "Exit";
        private const string RELOAD_CROSSHAIR = "ReloadCrosshair";
        private const string SWITCH_MONITOR = "SwitchMonitor";

        public SettingsForm(ConfigurationManager configManager, HotkeyManager hotkeyManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));

            InitializeComponent();

            // Prevent design-time execution
            if (!DesignMode)
            {
                LoadSettings();
                PopulateMonitorDropdown();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PerformLayout();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ConfigureFormProperties();
            CreateControls();
            RegisterEventHandlers();
            ResumeLayout(false);
        }

        private void ConfigureFormProperties()
        {
            Text = "Crosshair Overlay Settings";
            Size = new Size(550, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowIcon = true;
            ShowInTaskbar = true;
            Font = new Font("Segoe UI", 9F);
            AutoScaleMode = AutoScaleMode.Font;
            Padding = new Padding(10);
        }

        private void CreateControls()
        {
            TableLayoutPanel mainLayout = CreateMainLayout();
            GroupBox hotkeysGroup = CreateHotkeysGroup();
            GroupBox generalGroup = CreateGeneralSettingsGroup();
            TableLayoutPanel buttonPanel = CreateButtonPanel();

            // Add panels to main layout
            mainLayout.Controls.Add(hotkeysGroup, 0, 0);
            mainLayout.Controls.Add(generalGroup, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        private TableLayoutPanel CreateMainLayout()
        {
            return new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RowStyles = {
                    new RowStyle(SizeType.Percent, 50F),
                    new RowStyle(SizeType.Percent, 30F),
                    new RowStyle(SizeType.Percent, 20F)
                }
            };
        }

        private GroupBox CreateHotkeysGroup()
        {
            var hotkeysGroup = CreateGroupBox("Hotkeys", 14);
            _hotkeysLayout = CreateHotkeyLayout();

            AddHotkeyControls();

            hotkeysGroup.Controls.Add(_hotkeysLayout);
            return hotkeysGroup;
        }

        private TableLayoutPanel CreateHotkeyLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(5),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 40F),
                    new ColumnStyle(SizeType.Percent, 60F)
                }
            };

            // Add row styles
            for (int i = 0; i < 4; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            return layout;
        }

        private void AddHotkeyControls()
        {
            AddHotkeyControl(_hotkeysLayout, "Toggle Visibility", TOGGLE_VISIBILITY, 0);
            AddHotkeyControl(_hotkeysLayout, "Exit Application", EXIT_ACTION, 1);
            AddHotkeyControl(_hotkeysLayout, "Reload Crosshair", RELOAD_CROSSHAIR, 2);
            AddHotkeyControl(_hotkeysLayout, "Switch Monitor", SWITCH_MONITOR, 3);
        }

        private GroupBox CreateGeneralSettingsGroup()
        {
            var generalGroup = CreateGroupBox("General Settings", 14);
            var generalLayout = CreateGeneralSettingsLayout();

            // Scale control
            var scaleLabel = CreateLabel("Crosshair Scale:");
            _scaleDropdown = CreateScaleDropdown();

            // Monitor control
            var monitorLabel = CreateLabel("Display Monitor:");
            _monitorDropdown = CreateMonitorDropdown();

            // Crosshair code control
            var crosshairLabel = CreateLabel("Crosshair Code:");
            var crosshairPanel = CreateCrosshairPanel();

            // Add controls to general layout
            generalLayout.Controls.Add(scaleLabel, 0, 0);
            generalLayout.Controls.Add(_scaleDropdown, 1, 0);
            generalLayout.Controls.Add(monitorLabel, 0, 1);
            generalLayout.Controls.Add(_monitorDropdown, 1, 1);
            generalLayout.Controls.Add(crosshairLabel, 0, 2);
            generalLayout.Controls.Add(crosshairPanel, 1, 2);

            generalGroup.Controls.Add(generalLayout);
            return generalGroup;
        }

        private TableLayoutPanel CreateGeneralSettingsLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(5),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 35F),
                    new ColumnStyle(SizeType.Percent, 65F)
                }
            };

            // Add row styles with padding
            for (int i = 0; i < 3; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.SetRowPadding(i, 5);
            }

            return layout;
        }

        private ComboBox CreateScaleDropdown()
        {
            var dropdown = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "scaleDropdown",
                Tag = "Scale",
                Margin = new Padding(3, 3, 3, 8)
            };

            PopulateScaleOptions(dropdown);
            return dropdown;
        }

        private void PopulateScaleOptions(ComboBox dropdown)
        {
            float[] scaleOptions = { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f };
            foreach (float scale in scaleOptions)
            {
                dropdown.Items.Add($"{scale}x");
            }
        }

        private ComboBox CreateMonitorDropdown()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "monitorDropdown",
                Tag = "Monitor",
                Margin = new Padding(3, 3, 3, 8)
            };
        }

        private TableLayoutPanel CreateCrosshairPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 75F),
                    new ColumnStyle(SizeType.Percent, 25F)
                }
            };

            _crosshairTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Name = "crosshairTextBox",
                Margin = new Padding(3, 3, 3, 8)
            };

            _applyButton = new Button
            {
                Text = "Apply",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 3, 3, 8),
                Padding = new Padding(0, 5, 0, 5)
            };

            panel.Controls.Add(_crosshairTextBox, 0, 0);
            panel.Controls.Add(_applyButton, 1, 0);

            return panel;
        }

        private TableLayoutPanel CreateButtonPanel()
        {
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(5, 15, 5, 5),
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 100F/3),
                    new ColumnStyle(SizeType.Percent, 100F/3),
                    new ColumnStyle(SizeType.Percent, 100F/3)
                }
            };

            _resetButton = CreateButton("Reset to Default");
            _saveButton = CreateButton("Save");
            _cancelButton = CreateButton("Cancel");

            buttonPanel.Controls.Add(_resetButton, 0, 0);
            buttonPanel.Controls.Add(_saveButton, 1, 0);
            buttonPanel.Controls.Add(_cancelButton, 2, 0);

            return buttonPanel;
        }

        private Button CreateButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Padding = new Padding(0, 8, 0, 8)
            };
        }

        private GroupBox CreateGroupBox(string text, float fontSize = 9f)
        {
            return new GroupBox
            {
                Text = text,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(5),
                Font = new Font("Segoe UI", fontSize, FontStyle.Regular)
            };
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3, 8, 3, 3)
            };
        }

        private void RegisterEventHandlers()
        {
            // Button event handlers
            _resetButton.Click += OnResetButtonClick;
            _saveButton.Click += OnSaveButtonClick;
            _cancelButton.Click += OnCancelButtonClick;
            _applyButton.Click += OnApplyCrosshairButtonClick;

            // Dropdown event handlers
            _scaleDropdown.SelectedIndexChanged += OnSettingChanged;
            _monitorDropdown.SelectedIndexChanged += OnSettingChanged;

            // Form events
            FormClosing += OnFormClosing;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (HasUnsavedChanges() && DialogResult != DialogResult.OK)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    SaveSettings();
                    DialogResult = DialogResult.OK;
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private bool HasUnsavedChanges()
        {
            if (HasCrosshairCodeChanged() || HasScaleChanged() || HasMonitorChanged())
                return true;

            return HaveHotkeyBindingsChanged();
        }

        private bool HasCrosshairCodeChanged()
        {
            return _configManager.ShareCode != _crosshairTextBox.Text;
        }

        private bool HasScaleChanged()
        {
            if (_scaleDropdown.SelectedItem == null)
                return false;

            string scaleText = _scaleDropdown.SelectedItem.ToString().Replace("x", "");
            if (float.TryParse(scaleText, out float scale))
                return _configManager.Scale != scale;

            return false;
        }

        private bool HasMonitorChanged()
        {
            return _configManager.MonitorIndex != _monitorDropdown.SelectedIndex;
        }

        private bool HaveHotkeyBindingsChanged()
        {
            foreach (HotkeyControl hotkeyControl in GetAllHotkeyControls())
            {
                string actionName = hotkeyControl.Tag.ToString();
                HotkeyBinding currentBinding = hotkeyControl.GetBinding();

                // Parse the string to enum
                if (Enum.TryParse<poji.Enums.HotkeyAction>(actionName, out var action))
                {
                    if (_originalBindings.TryGetValue(action, out var originalBinding))
                    {
                        if (!AreBindingsEqual(currentBinding, originalBinding))
                            return true;
                    }
                    else if (currentBinding != null) // New binding
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool AreBindingsEqual(HotkeyBinding a, HotkeyBinding b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;

            return a.Key == b.Key && a.Alt == b.Alt && a.Shift == b.Shift && a.Ctrl == b.Ctrl;
        }

        private void LoadSettings()
        {
            try
            {
                LoadHotkeyBindings();
                LoadScaleSetting();
                LoadCrosshairSetting();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading settings", ex);
            }
        }

        private void LoadHotkeyBindings()
        {
            // Get all hotkey bindings from the configuration manager
            _originalBindings = _configManager.GetHotkeyBindings();

            foreach (HotkeyControl hotkeyControl in GetAllHotkeyControls())
            {
                string actionName = hotkeyControl.Tag.ToString();

                // Parse the string to enum
                if (Enum.TryParse<poji.Enums.HotkeyAction>(actionName, out var action) &&
                    _originalBindings.TryGetValue(action, out HotkeyBinding binding))
                {
                    hotkeyControl.SetBinding(binding);
                }
            }
        }

        private void LoadScaleSetting()
        {
            string scaleString = $"{_configManager.Scale}x";
            int scaleIndex = _scaleDropdown.Items.IndexOf(scaleString);

            _scaleDropdown.SelectedIndex = scaleIndex >= 0
                ? scaleIndex
                : GetDefaultScaleIndex();
        }

        private int GetDefaultScaleIndex()
        {
            return _scaleDropdown.Items.IndexOf($"{DEFAULT_SCALE}x");
        }

        private void LoadCrosshairSetting()
        {
            _crosshairTextBox.Text = _configManager.ShareCode;
        }

        private void PopulateMonitorDropdown()
        {
            try
            {
                _monitorDropdown.Items.Clear();
                AddMonitorsToDropdown();
                SelectCurrentMonitor();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading monitor list", ex);
            }
        }

        private void AddMonitorsToDropdown()
        {
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var screen = Screen.AllScreens[i];
                string primaryIndicator = screen.Primary ? " (Primary)" : "";
                _monitorDropdown.Items.Add($"Monitor {i + 1}{primaryIndicator} ({screen.Bounds.Width}x{screen.Bounds.Height})");
            }
        }

        private void SelectCurrentMonitor()
        {
            if (_monitorDropdown.Items.Count > 0)
            {
                int monitorIndex = Math.Min(_configManager.MonitorIndex, _monitorDropdown.Items.Count - 1);
                _monitorDropdown.SelectedIndex = monitorIndex;
            }
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string settingName)
            {
                switch (settingName)
                {
                    case "Scale":
                        UpdateScaleSetting(comboBox);
                        break;

                    case "Monitor":
                        UpdateMonitorSetting(comboBox);
                        break;
                }
            }
        }

        private void UpdateScaleSetting(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is string scaleText)
            {
                scaleText = scaleText.Replace("x", "");
                if (float.TryParse(scaleText, out float scale))
                {
                    _configManager.SaveScale(scale);
                }
            }
        }

        private void UpdateMonitorSetting(ComboBox comboBox)
        {
            if (comboBox.SelectedIndex >= 0)
            {
                _configManager.SaveMonitorIndex(comboBox.SelectedIndex);
            }
        }

        private void OnApplyCrosshairButtonClick(object sender, EventArgs e)
        {
            string crosshairCode = _crosshairTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(crosshairCode))
            {
                MessageBox.Show("Please enter a valid crosshair code.", "Invalid Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ValidateAndApplyCrosshairCode(crosshairCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid crosshair code: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ValidateAndApplyCrosshairCode(string crosshairCode)
        {
            var decoder = new CsgoCrosshairDecoder();
            var crosshairInfo = decoder.DecodeShareCodeToCrosshairInfo(crosshairCode);

            _configManager.SaveShareCode(crosshairCode);

            MessageBox.Show("Crosshair code has been validated and will be applied when you save settings.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                // Collect the bindings as enum keys
                Dictionary<poji.Enums.HotkeyAction, HotkeyBinding> enumBindings = CollectHotkeyBindings();

                // Save these to the config manager
                _configManager.SaveHotkeyBindings(enumBindings);
                _configManager.SaveAllSettings();

                // Convert to string keys for the hotkey manager
                Dictionary<string, HotkeyBinding> stringBindings = enumBindings.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value
                );

                // Update the hotkey manager with string keys
                _hotkeyManager.UpdateBindings(stringBindings);

                MessageBox.Show("Settings saved successfully.", "Settings Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error saving settings", ex);
            }
        }

        private Dictionary<poji.Enums.HotkeyAction, HotkeyBinding> CollectHotkeyBindings()
        {
            Dictionary<poji.Enums.HotkeyAction, HotkeyBinding> newBindings = new Dictionary<poji.Enums.HotkeyAction, HotkeyBinding>();

            foreach (HotkeyControl hotkeyControl in GetAllHotkeyControls())
            {
                string actionName = hotkeyControl.Tag.ToString();
                HotkeyBinding binding = hotkeyControl.GetBinding();
                if (binding != null)
                {
                    // Parse the string action name to the corresponding enum value
                    if (Enum.TryParse<poji.Enums.HotkeyAction>(actionName, out var action))
                    {
                        newBindings[action] = binding;
                    }
                }
            }

            return newBindings;
        }

        private void OnResetButtonClick(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings to default?",
                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    _configManager.RestoreDefaultSettings();
                    LoadSettings();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Error resetting settings", ex);
                }
            }
        }

        private IEnumerable<HotkeyControl> GetAllHotkeyControls()
        {
            return _hotkeysLayout.Controls.OfType<HotkeyControl>();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_activeHotkeyControl != null)
            {
                _activeHotkeyControl.SetKey(keyData);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnHotkeyFocusEntered(object sender, EventArgs e)
        {
            _activeHotkeyControl = (HotkeyControl)sender;
        }

        private void OnHotkeyFocusLeft(object sender, EventArgs e)
        {
            _activeHotkeyControl = null;
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void AddHotkeyControl(TableLayoutPanel layout, string labelText, string actionName, int row)
        {
            Label label = CreateLabel(labelText + ":");

            HotkeyControl hotkeyControl = new HotkeyControl
            {
                Dock = DockStyle.Fill,
                Name = actionName + "Hotkey",
                Tag = actionName,
                Margin = new Padding(3, 3, 3, 8),
                Height = 28
            };

            hotkeyControl.HotkeyFocusEntered += OnHotkeyFocusEntered;
            hotkeyControl.HotkeyFocusLeft += OnHotkeyFocusLeft;

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(hotkeyControl, 1, row);
        }

        private void ShowErrorMessage(string message, Exception ex)
        {
            MessageBox.Show($"{message}: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}