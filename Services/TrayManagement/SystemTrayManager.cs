using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Configuration;
using poji.Interfaces;
using poji.Models.Events;

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

        // Events
        public event EventHandler ExitRequested;
        public event EventHandler ToggleVisibilityRequested;
        public event EventHandler LoadCrosshairRequested;
        public event EventHandler<MonitorChangeEventArgs> MonitorChangeRequested;
        public event EventHandler<ScaleChangeEventArgs> ScaleChangeRequested;
        public event EventHandler SettingsRequested;

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

            // Find monitors menu
            var monitorsMenu = FindMonitorsMenu();
            if (monitorsMenu == null) return;

            // Clear existing items
            monitorsMenu.DropDownItems.Clear();

            // Add monitor items
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

            // Other menu items
            AddStandardMenuItems();
        }

        private void AddMonitorItems(ToolStripMenuItem monitorsMenu)
        {
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var screen = Screen.AllScreens[i];
                var index = i; // Capture the index for the lambda
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
                var scaleVal = scale; // Capture for lambda
                var item = new ToolStripMenuItem($"{scale}x", null, (s, e) => OnScaleSelected(scaleVal));
                scaleMenu.DropDownItems.Add(item);
            }
            _trayMenu.Items.Add(scaleMenu);
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