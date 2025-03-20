using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace poji
{
    public partial class MainForm : Form
    {
        // Win32 API imports for window styling
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        // For multi-monitor support
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        // Constants for window styles
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        
        // SetWindowPos constants
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // System tray components
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // Hotkey manager
        private GlobalKeyboardHook keyboardHook;

        // Crosshair properties
        private CSGOCrosshairDecoder.CrosshairInfo crosshairInfo;
        private bool useCustomCrosshair = false;
        private Color dotColor = Color.Red;
        private int dotSize = 10;
        
        // Configuration settings
        private int currentMonitorIndex = 0;
        private bool isVisible = true;
        private float scaleFactor = 1.0f;

        public MainForm()
        {
            InitializeForm();
            InitializeTrayIcon();
            SetupHotkey();
        }

        private void InitializeForm()
        {
            // Basic form setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;

            // Cover the entire primary screen initially
            SetFormToScreen(Screen.PrimaryScreen);

            // Make the form background completely transparent
            this.BackColor = Color.LimeGreen; // Use a color not present in your UI
            this.TransparencyKey = Color.LimeGreen;

            // Set window styles for click-through (layered + transparent)
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
            
            // Ensure the form stays on top
            SetWindowPos(this.Handle, HWND_TOPMOST, this.Left, this.Top, this.Width, this.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
        
        private void SetFormToScreen(Screen screen)
        {
            // Set form size to cover the entire screen
            this.Bounds = screen.Bounds;
            this.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
            this.Size = new Size(screen.Bounds.Width, screen.Bounds.Height);
            
            // Force redraw
            this.Invalidate();
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            
            // Add monitor selection submenu
            var monitorsMenu = new ToolStripMenuItem("Select Monitor");
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                var screen = Screen.AllScreens[i];
                var index = i; // Capture the index for the lambda
                var item = new ToolStripMenuItem($"Monitor {i+1} ({screen.Bounds.Width}x{screen.Bounds.Height})", 
                    null, (s, e) => SwitchToMonitor(index));
                monitorsMenu.DropDownItems.Add(item);
            }
            trayMenu.Items.Add(monitorsMenu);
            
            // Scale submenu
            var scaleMenu = new ToolStripMenuItem("Scale");
            foreach (float scale in new[] { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f })
            {
                var scaleVal = scale; // Capture for lambda
                var item = new ToolStripMenuItem($"{scale}x", null, (s, e) => SetScale(scaleVal));
                scaleMenu.DropDownItems.Add(item);
            }
            trayMenu.Items.Add(scaleMenu);
            
            // Other menu items
            trayMenu.Items.Add("Show/Hide", null, OnToggleVisibility);
            trayMenu.Items.Add("About", null, OnAboutClicked);
            trayMenu.Items.Add("Load Crosshair Code", null, OnLoadCrosshairClicked);
            trayMenu.Items.Add("-"); // Separator
            trayMenu.Items.Add("Exit", null, OnExitClicked);

            trayIcon = new NotifyIcon
            {
                Text = "Crosshair Overlay",
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += OnTrayIconDoubleClick;
        }

        private void SetupHotkey()
        {
            keyboardHook = new GlobalKeyboardHook();
            keyboardHook.KeyDown += KeyboardHook_KeyDown;
            keyboardHook.Start();
        }

        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for hotkeys
            bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
            bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;
            bool ctrlPressed = (Control.ModifierKeys & Keys.Control) != 0;

            // Alt+Shift+W to exit
            if (altPressed && shiftPressed && e.KeyCode == Keys.W)
            {
                ExitApplication();
            }
            
            // Alt+Shift+H to toggle visibility
            else if (altPressed && shiftPressed && e.KeyCode == Keys.H)
            {
                ToggleVisibility();
                e.Handled = true;
            }
            
            // Alt+Shift+R to reload crosshair
            else if (altPressed && shiftPressed && e.KeyCode == Keys.R)
            {
                ReloadCrosshair();
                e.Handled = true;
            }
            
            // Alt+Shift+[1-9] to switch monitors
            else if (altPressed && shiftPressed && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                int screenIndex = e.KeyCode - Keys.D1;
                if (screenIndex < Screen.AllScreens.Length)
                {
                    SwitchToMonitor(screenIndex);
                }
                e.Handled = true;
            }
        }
        
        private void SwitchToMonitor(int index)
        {
            if (index >= 0 && index < Screen.AllScreens.Length)
            {
                currentMonitorIndex = index;
                SetFormToScreen(Screen.AllScreens[index]);
            }
        }
        
        private void SetScale(float scale)
        {
            scaleFactor = scale;
            if (crosshairInfo != null)
            {
                crosshairInfo.ScaleFactor = scale;
                this.Invalidate();
            }
        }
        
        private void ToggleVisibility()
        {
            isVisible = !isVisible;
            this.Visible = isVisible;
        }
        
        private void ReloadCrosshair()
        {
            LoadCrosshairFromSettings();
            this.Invalidate();
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            ToggleVisibility();
        }
        
        private void OnToggleVisibility(object sender, EventArgs e)
        {
            ToggleVisibility();
        }

        private void OnAboutClicked(object sender, EventArgs e)
        {
            MessageBox.Show("CS:GO Crosshair Overlay\nVersion 1.0\n\nHotkeys:\nAlt+Shift+W: Exit\nAlt+Shift+H: Toggle visibility\nAlt+Shift+R: Reload crosshair\nAlt+Shift+[1-9]: Switch monitor", 
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnLoadCrosshairClicked(object sender, EventArgs e)
        {
            using (var inputDialog = new InputDialog("Enter CS:GO Crosshair Share Code", "Share Code:"))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string shareCode = inputDialog.InputText;
                    try
                    {
                        var decoder = new CSGOCrosshairDecoder();
                        byte[] bytes = decoder.DecodeShareCode(shareCode);
                        crosshairInfo = decoder.DecodeCrosshairInfo(bytes);
                        crosshairInfo.ScaleFactor = scaleFactor; // Apply current scale factor
                        SaveCrosshairToSettings(shareCode);
                        this.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error decoding crosshair: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        private void SaveCrosshairToSettings(string shareCode)
        {
            try
            {
                // Create a settings directory if it doesn't exist
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CrosshairOverlay");
                    
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                
                string settingsFile = Path.Combine(appDataPath, "settings.txt");
                
                // Save all settings
                using (StreamWriter writer = new StreamWriter(settingsFile))
                {
                    writer.WriteLine($"ShareCode={shareCode}");
                    writer.WriteLine($"Scale={scaleFactor}");
                    writer.WriteLine($"Monitor={currentMonitorIndex}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't disrupt the application
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void LoadCrosshairFromSettings()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CrosshairOverlay");
                string settingsFile = Path.Combine(appDataPath, "settings.txt");
                
                if (File.Exists(settingsFile))
                {
                    string shareCode = null;
                    
                    // Read settings line by line
                    foreach (string line in File.ReadAllLines(settingsFile))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        
                        if (key == "ShareCode")
                        {
                            shareCode = value;
                        }
                        else if (key == "Scale")
                        {
                            if (float.TryParse(value, out float scale))
                            {
                                scaleFactor = scale;
                            }
                        }
                        else if (key == "Monitor")
                        {
                            if (int.TryParse(value, out int monitor) && 
                                monitor >= 0 && monitor < Screen.AllScreens.Length)
                            {
                                currentMonitorIndex = monitor;
                                SetFormToScreen(Screen.AllScreens[monitor]);
                            }
                        }
                    }
                    
                    // Load crosshair if we found a share code
                    if (!string.IsNullOrEmpty(shareCode))
                    {
                        var decoder = new CSGOCrosshairDecoder();
                        byte[] bytes = decoder.DecodeShareCode(shareCode);
                        crosshairInfo = decoder.DecodeCrosshairInfo(bytes);
                        crosshairInfo.ScaleFactor = scaleFactor;
                        useCustomCrosshair = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to default dot if an error occurs
                useCustomCrosshair = false;
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!isVisible) return;

            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (crosshairInfo != null)
            {
                DrawCrosshair(e.Graphics, centerX, centerY);
            }
            else
            {
                // Draw fallback tiny dot
                using (var brush = new SolidBrush(dotColor))
                {
                    int size = (int)(dotSize * scaleFactor);
                    e.Graphics.FillEllipse(brush, centerX - size/2, centerY - size/2, size, size);
                }
            }
        }

        private void DrawCrosshair(Graphics g, int centerX, int centerY)
        {
            // Apply scale factor to all crosshair dimensions
            float halfLength = crosshairInfo.Length * scaleFactor / 2;
            float halfThickness = crosshairInfo.Thickness * scaleFactor / 2;
            float halfGap = crosshairInfo.Gap * scaleFactor / 2;
            float outlineThickness = crosshairInfo.Outline * scaleFactor;
            
            // Create colors with proper alpha
            Color mainColor = Color.FromArgb(
                crosshairInfo.Alpha, 
                crosshairInfo.Red, 
                crosshairInfo.Green, 
                crosshairInfo.Blue);
                
            Color outlineColor = Color.FromArgb(
                crosshairInfo.Alpha, 
                0, 0, 0); // Black outline with same alpha
            
            // Set up pens for drawing
            using (var mainPen = new Pen(mainColor, crosshairInfo.Thickness * scaleFactor))
            using (var outlinePen = new Pen(outlineColor, crosshairInfo.Thickness * scaleFactor + (crosshairInfo.HasOutline ? outlineThickness * 2 : 0)))
            using (var dotBrush = new SolidBrush(mainColor))
            {
                // Draw outline first if enabled
                if (crosshairInfo.HasOutline)
                {
                    // Horizontal lines
                    g.DrawLine(outlinePen, centerX - halfLength - halfGap, centerY, centerX - halfGap, centerY);
                    g.DrawLine(outlinePen, centerX + halfGap, centerY, centerX + halfLength + halfGap, centerY);

                    // Vertical lines
                    if (!crosshairInfo.IsTStyle)
                    {
                        g.DrawLine(outlinePen, centerX, centerY - halfLength - halfGap, centerX, centerY - halfGap);
                    }
                    g.DrawLine(outlinePen, centerX, centerY + halfGap, centerX, centerY + halfLength + halfGap);
                }

                // Draw main crosshair
                // Horizontal lines
                g.DrawLine(mainPen, centerX - halfLength - halfGap, centerY, centerX - halfGap, centerY);
                g.DrawLine(mainPen, centerX + halfGap, centerY, centerX + halfLength + halfGap, centerY);

                // Vertical lines
                if (!crosshairInfo.IsTStyle)
                {
                    g.DrawLine(mainPen, centerX, centerY - halfLength - halfGap, centerX, centerY - halfGap);
                }
                g.DrawLine(mainPen, centerX, centerY + halfGap, centerX, centerY + halfLength + halfGap);

                // Draw center dot if enabled
                if (crosshairInfo.HasCenterDot)
                {
                    float dotSize = crosshairInfo.Thickness * scaleFactor * 1.5f;
                    g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadCrosshairFromSettings();

            // Prompt user if no crosshair is loaded
            if (crosshairInfo == null)
            {
                OnLoadCrosshairClicked(this, EventArgs.Empty);
            }
        }

        // Allow form to close when exit is triggered
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only cancel if it's not an explicit exit
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            // Clean up resources
            if (keyboardHook != null)
            {
                keyboardHook.Stop();
                keyboardHook.Dispose();
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon?.Dispose();
                trayMenu?.Dispose();
                keyboardHook?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Simple input dialog for entering share code
    public class InputDialog : Form
    {
        private TextBox textBox;
        private Button okButton;
        private Button cancelButton;
        private Label promptLabel;

        public string InputText { get { return textBox.Text; } }

        public InputDialog(string title, string prompt)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(350, 150);

            promptLabel = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(300, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(320, 20)
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(165, 70),
                Size = new Size(75, 23)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(245, 70),
                Size = new Size(75, 23)
            };

            this.Controls.Add(promptLabel);
            this.Controls.Add(textBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}