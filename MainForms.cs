using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using poji.Properties;

namespace poji
{
    public class MainForm : Form
    {
        private RecoilPatternManager _recoilPatternManager;
        private RecoilPatternRenderer _recoilPatternRenderer;
        private bool _showRecoilPattern = false;
        private RecoilPattern _currentRecoilPattern = null;
        // Managers for different responsibilities
        private readonly TrayIconManager _trayManager;
        private readonly CrosshairRenderer _crosshairRenderer;
        private readonly ConfigurationManager _configManager;
        private readonly HotkeyManager _hotkeyManager;

        // Window state
        private bool _isVisible = true;
        private int _currentMonitorIndex;

        public MainForm()
        {
            InitializeComponent();
            InitializeForm();
            
            // Initialize managers
            _configManager = new ConfigurationManager();
            _crosshairRenderer = new CrosshairRenderer();
            _hotkeyManager = new HotkeyManager();
            _trayManager = new TrayIconManager(this);
            
            // Connect events
            _hotkeyManager.HotkeyTriggered += HotkeyManager_HotkeyTriggered;
            _trayManager.ExitRequested += (s, e) => ExitApplication();
            _trayManager.ToggleVisibilityRequested += (s, e) => ToggleVisibility();
            _trayManager.LoadCrosshairRequested += (s, e) => ShowLoadCrosshairDialog();
            _trayManager.MonitorChangeRequested += (s, e) => SwitchToMonitor(e.MonitorIndex);
            _trayManager.ScaleChangeRequested += (s, e) => SetScale(e.ScaleFactor);
            _trayManager.SettingsRequested += (s, e) => ShowSettingsDialog();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Name = "MainForm";
            Text = Resources.MainForm_InitializeComponent_Crosshair_Overlay;
            ResumeLayout(false);
        }

        private void InitializeForm()
        {
            // Basic form setup
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            // Cover the entire primary screen initially
            SetFormToScreen(Screen.PrimaryScreen);

            // Make the form background completely transparent
            BackColor = Color.Black;
            TransparencyKey = Color.Black;

            // Set window styles for click-through
            SetClickThroughEnabled(true);
            
            // Ensure the form stays on top
            SetTopMost(true);
        }

        #region Window Management

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        
        private const int GwlExstyle = -20;
        private const int WsExLayered = 0x80000;
        private const int WsExTransparent = 0x20;
        
        private static readonly IntPtr HwndTopmost = new IntPtr(-1);
        private const uint SwpNoactivate = 0x0010;
        private const uint SwpShowwindow = 0x0040;

        private void SetClickThroughEnabled(bool enabled)
        {
            var exStyle = GetWindowLong(Handle, GwlExstyle);
            
            if (enabled)
                exStyle |= WsExLayered | WsExTransparent;
            else
                exStyle &= ~WsExTransparent;
                
            SetWindowLong(Handle, GwlExstyle, exStyle);
        }

        private void SetTopMost(bool topMost)
        {
            if (topMost)
            {
                SetWindowPos(Handle, HwndTopmost, Left, Top, Width, Height, SwpNoactivate | SwpShowwindow);
            }
        }

        private void SetFormToScreen(Screen screen)
        {
            Bounds = screen.Bounds;
            Location = new Point(screen.Bounds.X, screen.Bounds.Y);
            Size = new Size(screen.Bounds.Width, screen.Bounds.Height);
            Invalidate();
        }

        private void SwitchToMonitor(int index)
        {
            if (index < 0 || index >= Screen.AllScreens.Length) return;
            _currentMonitorIndex = index;
            SetFormToScreen(Screen.AllScreens[index]);
            _configManager.SaveMonitorSetting(_currentMonitorIndex);
        }
        
        #endregion

        #region Event Handlers

        private void HotkeyManager_HotkeyTriggered(object sender, HotkeyEventArgs e)
        {
            switch (e.Action)
            {
                case HotkeyAction.Exit:
                    ExitApplication();
                    break;
                    
                case HotkeyAction.ToggleVisibility:
                    ToggleVisibility();
                    break;
                    
                case HotkeyAction.ReloadCrosshair:
                    ReloadCrosshair();
                    break;
                    
                case HotkeyAction.SwitchMonitor:
                    if (e.Data < Screen.AllScreens.Length)
                    {
                        SwitchToMonitor(e.Data);
                    }
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Load settings
            LoadSettings();

            // If we don't have a crosshair, prompt the user to enter one
            if (_crosshairRenderer.CrosshairInfo == null)
            {
                ShowLoadCrosshairDialog();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!_isVisible) return;

            int centerX = Width / 2;
            int centerY = Height / 2;
            
            _crosshairRenderer.Draw(e.Graphics, centerX, centerY);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only cancel if it's not an explicit exit
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayManager?.Dispose();
                _hotkeyManager?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Actions

        private void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            Visible = _isVisible;
        }
        
        private void ReloadCrosshair()
        {
            LoadCrosshairFromSettings();
            Invalidate();
        }

        private void SetScale(float scale)
        {
            _crosshairRenderer.ScaleFactor = scale;
            _configManager.SaveScaleSetting(scale);
            Invalidate();
        }

        private void ExitApplication()
        {
            _trayManager.Dispose();
            Application.Exit();
        }
        
        public CsgoCrosshairDecoder.CrosshairInfo GetCurrentCrosshair()
        {
            return _crosshairRenderer?.CrosshairInfo?.Clone();
        }

        private void ShowLoadCrosshairDialog()
        {
            using (var inputDialog = new CrosshairInputDialog("Enter CS:GO Crosshair Share Code", "Share Code:"))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string shareCode = inputDialog.InputText;
                    try
                    {
                        // The crosshair is already validated in the dialog
                        var crosshairInfo = inputDialog.CrosshairInfo;
                
                        if (crosshairInfo != null)
                        {
                            // Update crosshair renderer with new info
                            _crosshairRenderer.CrosshairInfo = crosshairInfo;
                            _crosshairRenderer.ScaleFactor = _configManager.CurrentScale;
                    
                            // Save to settings
                            _configManager.SaveShareCode(shareCode);
                    
                            // Refresh display
                            Invalidate();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error applying crosshair: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowSettingsDialog()
        {
            // Temporarily disable click-through to interact with the dialog
            SetClickThroughEnabled(false);
            
            using (var settingsForm = new SettingsForm(_configManager, _hotkeyManager))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Apply changes immediately
                    LoadSettings();
                    
                    // Update hotkey bindings
                    _hotkeyManager.UpdateBindings(_configManager.GetHotkeyBindings());
                }
            }
            
            // Re-enable click-through
            SetClickThroughEnabled(true);
        }

        private void LoadSettings()
        {
            // Load configurations
            _configManager.LoadSettings();
            
            // Apply monitor setting
            int monitorIndex = _configManager.CurrentMonitor;
            if (monitorIndex >= 0 && monitorIndex < Screen.AllScreens.Length)
            {
                _currentMonitorIndex = monitorIndex;
                SetFormToScreen(Screen.AllScreens[monitorIndex]);
            }
            
            // Load crosshair
            LoadCrosshairFromSettings();
        }

        private void LoadCrosshairFromSettings()
        {
            try
            {
                string shareCode = _configManager.CurrentShareCode;
                float scale = _configManager.CurrentScale;
                
                if (!string.IsNullOrEmpty(shareCode))
                {
                    var decoder = new CsgoCrosshairDecoder();
                    var crosshairInfo = decoder.DecodeShareCodeToCrosshairInfo(shareCode);
                    
                    _crosshairRenderer.CrosshairInfo = crosshairInfo;
                    _crosshairRenderer.ScaleFactor = scale;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Resources.MainForm_LoadCrosshairFromSettings_Error_loading_crosshair_settings___0_, ex.Message);
            }
        }
        
        #endregion
    }
}