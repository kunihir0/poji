﻿using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using poji.Configuration;
using poji.Enums;
using poji.Interfaces;
using poji.Models;
using poji.Models.Events;
using poji.Properties;
using poji.Rendering;
using poji.Services.TrayManagement;
using poji.UI;
using poji.UI.Dialogs;
using poji.Utils;
using Color = System.Drawing.Color;

namespace poji
{
    public class MainForm : Form
    {
        // Managers for different responsibilities
        private ITrayManager _trayManager;
        private CrosshairRenderer _crosshairRenderer;
        private ConfigurationManager _configManager;
        private HotkeyManager _hotkeyManager;

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

            // Initialize tray manager
            InitializeTrayManager();

            // Connect events
            _hotkeyManager.HotkeyTriggered += HotkeyManager_HotkeyTriggered;
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

        private void InitializeTrayManager()
        {
            var trayConfig = new TrayIconConfiguration
            {
                IconText = Resources.TrayIconManager_InitializeTrayIcon_Crosshair_Overlay,
                AboutText = Resources.TrayIconManager_OnAboutClicked_,
                AboutTitle = Resources.TrayIconManager_OnAboutClicked_About,
            };

            _trayManager = TrayManagerFactory.CreateTrayManager(this, trayConfig);

            // Connect existing tray events
            _trayManager.ExitRequested += (s, e) => ExitApplication();
            _trayManager.ToggleVisibilityRequested += (s, e) => ToggleVisibility();
            _trayManager.LoadCrosshairRequested += (s, e) => ShowLoadCrosshairDialog();
            _trayManager.MonitorChangeRequested += (s, e) => SwitchToMonitor(e.MonitorIndex);
            _trayManager.ScaleChangeRequested += (s, e) => SetScale(e.ScaleFactor);
            _trayManager.SettingsRequested += (s, e) => ShowSettingsDialog();

            // Connect new tray events
            _trayManager.RenderModeChangeRequested += (s, e) => SetRenderMode(e.Mode);
            _trayManager.ToggleDebugModeRequested += (s, e) => ToggleDebugMode();
            _trayManager.ShowColorCustomizationRequested += (s, e) => ShowColorCustomizationDialog();
            _trayManager.OpacityChangeRequested += (s, e) => SetCrosshairOpacity(e.Opacity);
            _trayManager.ToggleRecoilSimulationRequested += (s, e) => ToggleRecoilSimulation();

            _trayManager.Initialize();
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
            _configManager.SaveMonitorIndex(_currentMonitorIndex);
        }

        #endregion

        #region New Functionality Methods

        private void SetRenderMode(CrosshairRenderer.RenderMode mode)
        {
            _crosshairRenderer.Mode = mode;
            Invalidate();
        }

        private void ToggleDebugMode()
        {
            if (_crosshairRenderer.CrosshairInfo != null)
            {
                _crosshairRenderer.CrosshairInfo.ShowDebugText = !_crosshairRenderer.CrosshairInfo.ShowDebugText;
                Invalidate();
            }
        }

        private void ShowColorCustomizationDialog()
        {
            SetClickThroughEnabled(false);

            if (_crosshairRenderer.CrosshairInfo != null)
            {
                using (var colorDialog = new ColorDialog())
                {
                    colorDialog.Color = Color.FromArgb(
                        _crosshairRenderer.CrosshairInfo.ColorR,
                        _crosshairRenderer.CrosshairInfo.ColorG,
                        _crosshairRenderer.CrosshairInfo.ColorB);

                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        _crosshairRenderer.CrosshairInfo.ColorR = colorDialog.Color.R;
                        _crosshairRenderer.CrosshairInfo.ColorG = colorDialog.Color.G;
                        _crosshairRenderer.CrosshairInfo.ColorB = colorDialog.Color.B;
                        Invalidate();
                    }
                }
            }

            SetClickThroughEnabled(true);
        }

        private void SetCrosshairOpacity(float opacity)
        {
            if (_crosshairRenderer.CrosshairInfo != null)
            {
                // Convert float (0.0-1.0) to byte (0-255)
                _crosshairRenderer.CrosshairInfo.Alpha = (byte)(opacity * 255);
                Invalidate();
            }
        }

        private void ToggleRecoilSimulation()
        {
            _crosshairRenderer.SimulateRecoil = !_crosshairRenderer.SimulateRecoil;
            Invalidate();
        }

        private void CycleRenderMode()
        {
            if (_crosshairRenderer != null)
            {
                var modes = Enum.GetValues(typeof(CrosshairRenderer.RenderMode));
                int currentIndex = Array.IndexOf(modes, _crosshairRenderer.Mode);
                int nextIndex = (currentIndex + 1) % modes.Length;
                _crosshairRenderer.Mode = (CrosshairRenderer.RenderMode)modes.GetValue(nextIndex);
                Invalidate();
            }
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

                case HotkeyAction.ToggleDebugMode:
                    ToggleDebugMode();
                    break;

                case HotkeyAction.ToggleRecoilSimulation:
                    ToggleRecoilSimulation();
                    break;

                case HotkeyAction.CycleRenderMode:
                    CycleRenderMode();
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSettings();
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
            _configManager.SaveScale(scale);
            Invalidate();
        }

        private void ExitApplication()
        {
            _trayManager?.Dispose();
            Application.Exit();
        }

        public CrosshairInfo GetCurrentCrosshair()
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
                        var crosshairInfo = inputDialog.CrosshairInfo;
                        if (crosshairInfo != null)
                        {
                            _crosshairRenderer.CrosshairInfo = crosshairInfo;
                            _crosshairRenderer.ScaleFactor = _configManager.Scale;
                            _configManager.SaveShareCode(shareCode);
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
            SetClickThroughEnabled(false);

            using (var settingsForm = new SettingsForm(_configManager, _hotkeyManager))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    LoadSettings();
                    var convertedBindings = _configManager.GetHotkeyBindings().ToDictionary(
                        pair => pair.Key.ToString(),
                        pair => pair.Value
                    );
                    _hotkeyManager.UpdateBindings(convertedBindings);
                }
            }

            SetClickThroughEnabled(true);
        }

        private void LoadSettings()
        {
            _configManager.LoadSettings();
            int monitorIndex = _configManager.MonitorIndex;
            if (monitorIndex >= 0 && monitorIndex < Screen.AllScreens.Length)
            {
                _currentMonitorIndex = monitorIndex;
                SetFormToScreen(Screen.AllScreens[monitorIndex]);
            }
            LoadCrosshairFromSettings();
        }

        private void LoadCrosshairFromSettings()
        {
            try
            {
                string shareCode = _configManager.ShareCode;
                float scale = _configManager.Scale;

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