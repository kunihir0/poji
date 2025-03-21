using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace poji
{
    public partial class SettingsForm : Form
    {
        private readonly ConfigurationManager _configManager;
        private readonly HotkeyManager _hotkeyManager;
        private Dictionary<string, HotkeyBinding> _currentBindings = new Dictionary<string, HotkeyBinding>();
        private HotkeyControl _activeHotkeyControl;

        // Control references
        private ComboBox scaleDropdown;
        private ComboBox monitorDropdown;
        private TextBox crosshairTextBox;
        private TableLayoutPanel hotkeysLayout;
        private Button saveButton;
        private Button resetButton;
        private Button cancelButton;
        private Button applyButton;

        // Theme colors
        private readonly Color _primaryColor = Color.FromArgb(42, 42, 42);
        private readonly Color _secondaryColor = Color.FromArgb(60, 60, 60);
        private readonly Color _accentColor = Color.FromArgb(0, 120, 215);
        private readonly Color _textColor = Color.FromArgb(240, 240, 240);
        private readonly Color _controlBackColor = Color.FromArgb(70, 70, 70);
        private readonly Color _controlForeColor = Color.FromArgb(240, 240, 240);

        public SettingsForm(ConfigurationManager configManager, HotkeyManager hotkeyManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));

            InitializeComponent();
            ApplyCustomTheme();

            // Prevent design-time execution
            if (!DesignMode)
            {
                LoadSettings();
                LoadMonitorDropdown();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Ensure all controls are properly sized and positioned
            PerformLayout();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form settings
            this.Text = "Crosshair Overlay Settings";
            this.Size = new Size(550, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ShowIcon = true;
            this.ShowInTaskbar = true;
            this.Font = new Font("Segoe UI", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Padding = new Padding(10);

            // Create the controls
            CreateControls();
            SetupEventHandlers();

            this.ResumeLayout(false);
        }

        private void ApplyCustomTheme()
        {
            // Apply theme to the form
            this.BackColor = _primaryColor;
            this.ForeColor = _textColor;

            // Apply theme to all controls recursively
            ApplyThemeToControls(this.Controls);
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                // Apply specific styling based on control type
                if (control is Button button)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = _accentColor;
                    button.BackColor = _secondaryColor;
                    button.ForeColor = _textColor;
                    button.Cursor = Cursors.Hand;

                    // Special handling for action buttons
                    if (button == saveButton)
                    {
                        button.BackColor = _accentColor;
                        button.FlatAppearance.BorderColor = Color.White;
                    }
                }
                else if (control is TextBox textBox)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = _controlBackColor;
                    textBox.ForeColor = _controlForeColor;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.BackColor = _controlBackColor;
                    comboBox.ForeColor = _controlForeColor;
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.ForeColor = _textColor;
                    groupBox.BackColor = _primaryColor;
                }
                else if (control is HotkeyControl hotkeyControl)
                {
                    hotkeyControl.BackColor = _controlBackColor;
                    hotkeyControl.ForeColor = _controlForeColor;
                    hotkeyControl.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is Label)
                {
                    control.ForeColor = _textColor;
                }
                else
                {
                    control.BackColor = _primaryColor;
                    control.ForeColor = _textColor;
                }

                // Recursively apply to child controls
                if (control.Controls.Count > 0)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }

        private void CreateControls()
        {
            // Create main container with padding for better spacing
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Better row proportions for more visual balance
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            // Create hotkeys panel with improved styling
            GroupBox hotkeysGroup = CreateStyledGroupBox("Hotkeys", 14);

            hotkeysLayout = new TableLayoutPanel
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

            // Better spacing between rows
            for (int i = 0; i < 4; i++)
            {
                hotkeysLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Add hotkey controls with proper spacing
            AddHotkeyControl(hotkeysLayout, "Toggle Visibility", "ToggleVisibility", 0);
            AddHotkeyControl(hotkeysLayout, "Exit Application", "Exit", 1);
            AddHotkeyControl(hotkeysLayout, "Reload Crosshair", "ReloadCrosshair", 2);
            AddHotkeyControl(hotkeysLayout, "Switch Monitor", "SwitchMonitor", 3);

            hotkeysGroup.Controls.Add(hotkeysLayout);

            // Create general settings panel with improved styling
            GroupBox generalGroup = CreateStyledGroupBox("General Settings", 14);

            TableLayoutPanel generalLayout = new TableLayoutPanel
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

            // Better spacing between rows
            for (int i = 0; i < 3; i++)
            {
                generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                generalLayout.SetRowPadding(i, 5);
            }

            // Scale control with better styling
            Label scaleLabel = CreateStyledLabel("Crosshair Scale:");

            scaleDropdown = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "scaleDropdown",
                Tag = "Scale",
                Margin = new Padding(3, 3, 3, 8)
            };

            foreach (float scale in new[] { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f })
            {
                scaleDropdown.Items.Add($"{scale}x");
            }

            // Monitor control with better styling
            Label monitorLabel = CreateStyledLabel("Display Monitor:");

            monitorDropdown = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "monitorDropdown",
                Tag = "Monitor",
                Margin = new Padding(3, 3, 3, 8)
            };

            // Crosshair code control with better styling
            Label crosshairLabel = CreateStyledLabel("Crosshair Code:");

            TableLayoutPanel crosshairPanel = new TableLayoutPanel
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

            crosshairTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Name = "crosshairTextBox",
                Margin = new Padding(3, 3, 3, 8)
            };

            applyButton = new Button
            {
                Text = "Apply",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 3, 3, 8),
                Padding = new Padding(0, 5, 0, 5)
            };

            crosshairPanel.Controls.Add(crosshairTextBox, 0, 0);
            crosshairPanel.Controls.Add(applyButton, 1, 0);

            // Add controls to general layout
            generalLayout.Controls.Add(scaleLabel, 0, 0);
            generalLayout.Controls.Add(scaleDropdown, 1, 0);
            generalLayout.Controls.Add(monitorLabel, 0, 1);
            generalLayout.Controls.Add(monitorDropdown, 1, 1);
            generalLayout.Controls.Add(crosshairLabel, 0, 2);
            generalLayout.Controls.Add(crosshairPanel, 1, 2);

            generalGroup.Controls.Add(generalLayout);

            // Button panel with improved styling
            TableLayoutPanel buttonPanel = new TableLayoutPanel
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

            resetButton = new Button
            {
                Text = "Reset to Default",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Padding = new Padding(0, 8, 0, 8)
            };

            saveButton = new Button
            {
                Text = "Save",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Padding = new Padding(0, 8, 0, 8)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Padding = new Padding(0, 8, 0, 8)
            };

            buttonPanel.Controls.Add(resetButton, 0, 0);
            buttonPanel.Controls.Add(saveButton, 1, 0);
            buttonPanel.Controls.Add(cancelButton, 2, 0);

            // Add panels to main layout with spacing
            mainLayout.Controls.Add(hotkeysGroup, 0, 0);
            mainLayout.Controls.Add(generalGroup, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private GroupBox CreateStyledGroupBox(string text, float fontSize = 9f)
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

        private Label CreateStyledLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3, 8, 3, 3)
            };
        }

        private void SetupEventHandlers()
        {
            // Direct event handlers for buttons
            resetButton.Click += ResetButton_Click;
            saveButton.Click += SaveButton_Click;
            cancelButton.Click += CancelButton_Click;
            applyButton.Click += LoadCrosshairButton_Click;

            // Direct event handlers for dropdowns
            scaleDropdown.SelectedIndexChanged += SettingChanged;
            monitorDropdown.SelectedIndexChanged += SettingChanged;

            // Form events
            this.FormClosing += SettingsForm_FormClosing;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if settings were changed but not saved
            if (AreSettingsChanged() && DialogResult != DialogResult.OK)
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

        private bool AreSettingsChanged()
        {
            // Check if any settings have been changed
            if (_configManager.CurrentShareCode != crosshairTextBox.Text)
                return true;

            string scaleText = scaleDropdown.SelectedItem?.ToString() ?? "1.0x";
            scaleText = scaleText.Replace("x", "");
            if (float.TryParse(scaleText, out float scale) && _configManager.CurrentScale != scale)
                return true;

            if (_configManager.CurrentMonitor != monitorDropdown.SelectedIndex)
                return true;

            // Check hotkeys
            Dictionary<string, HotkeyBinding> currentBindings = new Dictionary<string, HotkeyBinding>();
            foreach (HotkeyControl hotkeyControl in GetAllHotkeyControls())
            {
                string actionName = hotkeyControl.Tag.ToString();
                HotkeyBinding binding = hotkeyControl.GetBinding();

                // Check if this binding has changed
                if (_currentBindings.TryGetValue(actionName, out var originalBinding))
                {
                    if (!AreBindingsEqual(binding, originalBinding))
                        return true;
                }
                else if (binding != null) // New binding
                {
                    return true;
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
                // Load current hotkeys
                _currentBindings = _configManager.GetHotkeyBindings() ?? new Dictionary<string, HotkeyBinding>();

                // Set values for hotkey controls
                foreach (HotkeyControl hotkeyControl in GetAllHotkeyControls())
                {
                    string actionName = hotkeyControl.Tag.ToString();
                    if (_currentBindings.TryGetValue(actionName, out HotkeyBinding binding))
                    {
                        hotkeyControl.SetBinding(binding);
                    }
                }

                // Set scale dropdown
                string scaleString = $"{_configManager.CurrentScale}x";
                int scaleIndex = scaleDropdown.Items.IndexOf(scaleString);
                if (scaleIndex >= 0)
                {
                    scaleDropdown.SelectedIndex = scaleIndex;
                }
                else
                {
                    scaleDropdown.SelectedIndex = 2; // Default to 1.0x
                }

                // Set crosshair code
                crosshairTextBox.Text = _configManager.CurrentShareCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadMonitorDropdown()
        {
            try
            {
                monitorDropdown.Items.Clear();

                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    var screen = Screen.AllScreens[i];
                    string primaryIndicator = screen.Primary ? " (Primary)" : "";
                    monitorDropdown.Items.Add($"Monitor {i + 1}{primaryIndicator} ({screen.Bounds.Width}x{screen.Bounds.Height})");
                }

                if (monitorDropdown.Items.Count > 0)
                {
                    monitorDropdown.SelectedIndex = Math.Min(_configManager.CurrentMonitor, monitorDropdown.Items.Count - 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading monitor list: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingChanged(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string settingName)
            {
                switch (settingName)
                {
                    case "Scale":
                        if (comboBox.SelectedItem is string scaleText)
                        {
                            scaleText = scaleText.Replace("x", "");
                            if (float.TryParse(scaleText, out float scale))
                            {
                                _configManager.CurrentScale = scale;
                            }
                        }
                        break;

                    case "Monitor":
                        if (comboBox.SelectedIndex >= 0)
                        {
                            _configManager.CurrentMonitor = comboBox.SelectedIndex;
                        }
                        break;
                }
            }
        }

        private void LoadCrosshairButton_Click(object sender, EventArgs e)
        {
            string crosshairCode = crosshairTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(crosshairCode))
            {
                MessageBox.Show("Please enter a valid crosshair code.", "Invalid Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var decoder = new CsgoCrosshairDecoder();
                var crosshairInfo = decoder.DecodeShareCodeToCrosshairInfo(crosshairCode);

                // If we got this far, the code is valid
                _configManager.CurrentShareCode = crosshairCode;

                MessageBox.Show("Crosshair code has been validated and will be applied when you save settings.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid crosshair code: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                // Save hotkey bindings
                Dictionary<string, HotkeyBinding> newBindings = new Dictionary<string, HotkeyBinding>();

                foreach (HotkeyControl hotkeyControl in GetAllHotkeyControls())
                {
                    string actionName = hotkeyControl.Tag.ToString();
                    HotkeyBinding binding = hotkeyControl.GetBinding();
                    if (binding != null)
                    {
                        newBindings[actionName] = binding;
                    }
                }

                // Save settings
                _configManager.SaveHotkeyBindings(newBindings);
                _configManager.SaveAllSettings();

                // Notify hotkey manager of changes
                _hotkeyManager.UpdateBindings(newBindings);

                MessageBox.Show("Settings saved successfully.", "Settings Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
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
                    MessageBox.Show($"Error resetting settings: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private IEnumerable<HotkeyControl> GetAllHotkeyControls()
        {
            return hotkeysLayout.Controls.OfType<HotkeyControl>();
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

        // Event handler implementations
        private void HotkeyControl_HotkeyFocusEntered(object sender, EventArgs e)
        {
            _activeHotkeyControl = (HotkeyControl)sender;
        }

        private void HotkeyControl_HotkeyFocusLeft(object sender, EventArgs e)
        {
            _activeHotkeyControl = null;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AddHotkeyControl(TableLayoutPanel layout, string labelText, string actionName, int row)
        {
            Label label = CreateStyledLabel(labelText + ":");

            HotkeyControl hotkeyControl = new HotkeyControl
            {
                Dock = DockStyle.Fill,
                Name = actionName + "Hotkey",
                Tag = actionName,
                Margin = new Padding(3, 3, 3, 8),
                Height = 28
            };

            hotkeyControl.HotkeyFocusEntered += HotkeyControl_HotkeyFocusEntered;
            hotkeyControl.HotkeyFocusLeft += HotkeyControl_HotkeyFocusLeft;

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(hotkeyControl, 1, row);
        }
    }
}