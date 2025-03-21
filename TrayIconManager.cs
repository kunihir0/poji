using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Properties;

namespace poji
{
    public class MonitorChangeEventArgs : EventArgs
    {
        public int MonitorIndex { get; }
        
        public MonitorChangeEventArgs(int monitorIndex)
        {
            MonitorIndex = monitorIndex;
        }
    }
    
    public class ScaleChangeEventArgs : EventArgs
    {
        public float ScaleFactor { get; }
        
        public ScaleChangeEventArgs(float scaleFactor)
        {
            ScaleFactor = scaleFactor;
        }
    }
    
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        public Form OwnerForm { get; }

        // Events
        public event EventHandler ExitRequested;
        public event EventHandler ToggleVisibilityRequested;
        public event EventHandler LoadCrosshairRequested;
        public event EventHandler<MonitorChangeEventArgs> MonitorChangeRequested;
        public event EventHandler<ScaleChangeEventArgs> ScaleChangeRequested;
        public event EventHandler SettingsRequested;
        
        public TrayIconManager(Form ownerForm)
        {
            OwnerForm = ownerForm;
            InitializeTrayIcon();
        }
        
        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            
            // Add monitor selection submenu
            var monitorsMenu = new ToolStripMenuItem("Select Monitor");
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var screen = Screen.AllScreens[i];
                var index = i; // Capture the index for the lambda
                var item = new ToolStripMenuItem($"Monitor {i+1} ({screen.Bounds.Width}x{screen.Bounds.Height})", 
                    null, (s, e) => OnMonitorSelected(index));
                monitorsMenu.DropDownItems.Add(item);
            }
            _trayMenu.Items.Add(monitorsMenu);
            
            // Scale submenu
            var scaleMenu = new ToolStripMenuItem("Scale");
            foreach (float scale in new[] { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f })
            {
                var scaleVal = scale; // Capture for lambda
                var item = new ToolStripMenuItem($"{scale}x", null, (s, e) => OnScaleSelected(scaleVal));
                scaleMenu.DropDownItems.Add(item);
            }
            _trayMenu.Items.Add(scaleMenu);
            
            // Other menu items
            _trayMenu.Items.Add("Show/Hide", null, (s, e) => ToggleVisibilityRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add(Resources.TrayIconManager_OnAboutClicked_About, null, OnAboutClicked);
            _trayMenu.Items.Add("Load Crosshair Code", null, (s, e) => LoadCrosshairRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add("Settings", null, (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty));
            _trayMenu.Items.Add("-"); // Separator
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _trayIcon = new NotifyIcon
            {
                Text = Resources.TrayIconManager_InitializeTrayIcon_Crosshair_Overlay,
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
            MessageBox.Show(
                Resources.TrayIconManager_OnAboutClicked_, 
                Resources.TrayIconManager_OnAboutClicked_About, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        public void Dispose()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayMenu.Dispose();
        }
    }
}