using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Configuration;
using poji.Interfaces;
using poji.Models.Events;
using poji.Rendering;

namespace poji.Services.TrayManagement
{
    /// <summary>
    /// Windows system tray implementation of ITrayManager
    /// </summary>
    public class SystemTrayManager : ITrayManager
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private readonly Form _ownerForm;
        private readonly TrayIconConfiguration _configuration;

        // Existing events
        public event EventHandler ExitRequested;
        public event EventHandler ToggleVisibilityRequested;
        public event EventHandler LoadCrosshairRequested;
        public event EventHandler<MonitorChangeEventArgs> MonitorChangeRequested;
        public event EventHandler<ScaleChangeEventArgs> ScaleChangeRequested;
        public event EventHandler SettingsRequested;

        // New events
        public event EventHandler<RenderModeChangeEventArgs> RenderModeChangeRequested;
        public event EventHandler ToggleDebugModeRequested;
        public event EventHandler ShowColorCustomizationRequested;
        public event EventHandler<OpacityChangeEventArgs> OpacityChangeRequested;
        public event EventHandler ToggleRecoilSimulationRequested;

        public SystemTrayManager(Form ownerForm, TrayIconConfiguration configuration)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Initialize()
        {
            CreateContextMenu();
            CreateTrayIcon();
        }

        public void SetIcon(Icon icon)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Icon = icon;
            }
        }

        public void UpdateMonitorList()
        {
            if (_trayMenu == null) return;

            var monitorsMenu = FindMonitorsMenu();
            if (monitorsMenu == null) return;

            monitorsMenu.DropDownItems.Clear();
            AddMonitorItems(monitorsMenu);
        }

        private ToolStripMenuItem FindMonitorsMenu()
        {
            foreach (ToolStripItem item in _trayMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Text == "Select Monitor")
                {
                    return menuItem;
                }
            }
            return null;
        }

        private void CreateContextMenu()
        {
            _trayMenu = new ContextMenuStrip();

            // Add monitor selection submenu
            var monitorsMenu = new ToolStripMenuItem("Select Monitor");
            AddMonitorItems(monitorsMenu);
            _trayMenu.Items.Add(monitorsMenu);

            // Scale submenu
            AddScaleMenu();

            // Add new crosshair functionality
            AddCrosshairMenuItems();

            // Other menu items
            AddStandardMenuItems();
        }

        private void AddMonitorItems(ToolStripMenuItem monitorsMenu)
        {
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var screen = Screen.AllScreens[i];
                var index = i;
                var menuText = $"Monitor {i + 1} ({screen.Bounds.Width}x{screen.Bounds.Height})";
                var item = new ToolStripMenuItem(menuText, null, (s, e) => OnMonitorSelected(index));
                monitorsMenu.DropDownItems.Add(item);
            }
        }

        private void AddScaleMenu()
        {
            var scaleMenu = new ToolStripMenuItem("Scale");
            foreach (float scale in _configuration.ScaleOptions)
            {
                var scaleVal = scale;
                var item = new ToolStripMenuItem($"{scale}x", null, (s, e) => OnScaleSelected(scaleVal));
                scaleMenu.DropDownItems.Add(item);
            }
            _trayMenu.Items.Add(scaleMenu);
        }

        private void AddCrosshairMenuItems()
        {
            // Render Mode submenu
            var renderModeMenu = new ToolStripMenuItem("Render Mode");
            foreach (var mode in Enum.GetValues(typeof(CrosshairRenderer.RenderMode)))
            {
                var modeVal = (CrosshairRenderer.RenderMode)mode;
                var item = new ToolStripMenuItem(modeVal.ToString(), null, (s, e) => OnRenderModeSelected(modeVal));
                renderModeMenu.DropDownItems.Add(item);
            }
            _trayMenu.Items.Add(renderModeMenu);

            // Opacity submenu
            var opacityMenu = new ToolStripMenuItem("Opacity");
            float[] opacityOptions = { 0.25f, 0.5f, 0.75f, 1.0f };
            foreach (float opacity in opacityOptions)
            {
                var item = new ToolStripMenuItem($"{opacity * 100}%", null, (s, e) => OnOpacitySelected(opacity));
                opacityMenu.DropDownItems.Add(item);
            }
            _trayMenu.Items.Add(opacityMenu);

            // Toggle and dialog items
            _trayMenu.Items.Add("Toggle Debug Mode", null, (s, e) => ToggleDebugModeRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add("Customize Color", null, (s, e) => ShowColorCustomizationRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add("Toggle Recoil Simulation", null, (s, e) => ToggleRecoilSimulationRequested?.Invoke(this, EventArgs.Empty));
        }

        private void AddStandardMenuItems()
        {
            _trayMenu.Items.Add("Show/Hide", null,
                (s, e) => ToggleVisibilityRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add(_configuration.AboutTitle ?? "About", null, OnAboutClicked);
            _trayMenu.Items.Add("Load Crosshair Code", null,
                (s, e) => LoadCrosshairRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add("Settings", null,
                (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add("-"); // Separator
            _trayMenu.Items.Add("Exit", null,
                (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Text = _configuration.IconText ?? "Crosshair Overlay",
                Icon = SystemIcons.Application,
                ContextMenuStrip = _trayMenu,
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => ToggleVisibilityRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnMonitorSelected(int index)
        {
            MonitorChangeRequested?.Invoke(this, new MonitorChangeEventArgs(index));
        }

        private void OnScaleSelected(float scale)
        {
            ScaleChangeRequested?.Invoke(this, new ScaleChangeEventArgs(scale));
        }

        private void OnRenderModeSelected(CrosshairRenderer.RenderMode mode)
        {
            RenderModeChangeRequested?.Invoke(this, new RenderModeChangeEventArgs(mode));
        }

        private void OnOpacitySelected(float opacity)
        {
            OpacityChangeRequested?.Invoke(this, new OpacityChangeEventArgs(opacity));
        }

        private void OnAboutClicked(object sender, EventArgs e)
        {
            var message = _configuration.AboutText ?? "Crosshair Overlay Application";
            var title = _configuration.AboutTitle ?? "About";
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
                _trayMenu = null;
            }
        }
    }
}