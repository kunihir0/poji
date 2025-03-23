using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using poji.Models;
using Color = System.Drawing.Color;
using poji.UI.Dialogs;

namespace poji
{
    public class DirectXOverlayForm : Form
    {
        // DirectX renderer
        private readonly DirectXRenderer _directXRenderer;

        // Simple tray icon for DirectX mode
        private NotifyIcon _trayIcon;

        // Window state
        private bool _isVisible = true;
        private int _currentMonitorIndex = 0;

        // Crosshair information
        private CrosshairInfo _crosshairInfo;
        private float _scaleFactor = 1.0f;

        // Window tracking
        private System.Windows.Forms.Timer _windowTrackingTimer;
        private const int TRACKING_INTERVAL_MS = 100;

        // Game Overlay Injection
        private GameOverlayInjection.Core.Injector _injector;
        private bool _isInjectionActive = false;
        private int _injectedProcessId = -1;
        /// <summary>
        /// Gets or sets the scale factor for the crosshair
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        public DirectXOverlayForm()
        {
            InitializeComponent();
            InitializeForm();

            // Initialize DirectX renderer
            _directXRenderer = new DirectXRenderer();
            bool dxInitialized = _directXRenderer.Initialize(this);

            if (!dxInitialized)
            {
                MessageBox.Show("Failed to initialize DirectX rendering. The application will now exit.",
                    "DirectX Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new ApplicationException("DirectX initialization failed");
            }

            // Initialize game overlay injector
            _injector = new GameOverlayInjection.Core.Injector();

            // Initialize minimal tray support
            InitializeTray();

            // Set up a render timer for DirectX
            var renderTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            renderTimer.Tick += (s, e) =>
            {
                if (_isVisible && _directXRenderer.IsInitialized)
                {
                    // Ensure we have a crosshair to display
                    if (_crosshairInfo == null)
                    {
                        // Create a default crosshair with known working values
                        _crosshairInfo = new CrosshairInfo();
                        _crosshairInfo.Size = 5;
                        _crosshairInfo.Gap = 2;
                        _crosshairInfo.Thickness = 1;
                        _crosshairInfo.T = true; // Ensure T is true to draw horizontal and vertical lines
                        _crosshairInfo.Dot = true;
                        _crosshairInfo.ShowDebugText = true; // Enable debug text to see values

                        // Set default color (bright green)
                        _crosshairInfo.ColorR = 0;
                        _crosshairInfo.ColorG = 255;
                        _crosshairInfo.ColorB = 0;
                        _crosshairInfo.Alpha = 255;

                        Console.WriteLine("Created default crosshair");
                    }

                    // Log crosshair properties for debugging
                    Console.WriteLine($"Rendering crosshair - Size: {_crosshairInfo.Size}, Gap: {_crosshairInfo.Gap}, " +
                                     $"Thickness: {_crosshairInfo.Thickness}, T: {_crosshairInfo.T}, Dot: {_crosshairInfo.Dot}, " +
                                     $"DotOnly: {_crosshairInfo.DotOnly}, Scale: {_scaleFactor}");

                    _directXRenderer.CrosshairInfo = _crosshairInfo;
                    _directXRenderer.ScaleFactor = _scaleFactor;
                    _directXRenderer.Render();
                }
            };
            renderTimer.Start();

            // Setup window tracking timer to ensure we stay on top in fullscreen
            _windowTrackingTimer = new System.Windows.Forms.Timer { Interval = TRACKING_INTERVAL_MS };
            _windowTrackingTimer.Tick += (s, e) => EnsureTopMost();
            _windowTrackingTimer.Start();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Name = "DirectXOverlayForm";
            Text = "DirectX Crosshair Overlay";
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

            // Set extended window styles
            SetExtendedStyleFlags();
        }

        private void InitializeTray()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "DirectX Crosshair Overlay",
                Visible = true,
                Icon = SystemIcons.Application
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            // Add toggle debug text option
            var debugTextItem = new ToolStripMenuItem("Toggle Debug Text");
            debugTextItem.Click += (s, e) =>
            {
                if (_crosshairInfo != null)
                {
                    _crosshairInfo.ShowDebugText = !_crosshairInfo.ShowDebugText;
                    Console.WriteLine($"Debug text: {_crosshairInfo.ShowDebugText}");
                }
            };
            contextMenu.Items.Add(debugTextItem);

            // Add experimental overlay injection option
            var injectionItem = new ToolStripMenuItem("Experimental: Inject Into Game");
            injectionItem.Click += (s, e) => ShowInjectIntoGameDialog();
            contextMenu.Items.Add(injectionItem);

            // Add option to stop injection
            var stopInjectionItem = new ToolStripMenuItem("Stop Active Injection");
            stopInjectionItem.Click += (s, e) => StopActiveInjection();
            stopInjectionItem.Enabled = false;
            contextMenu.Items.Add(stopInjectionItem);

            // Add always on top enforcer option
            var forceTopMostItem = new ToolStripMenuItem("Force Always On Top");
            forceTopMostItem.Click += (s, e) => ForceWindowToTop();
            contextMenu.Items.Add(forceTopMostItem);

            // Add exit option
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            // Assign context menu to tray icon
            _trayIcon.ContextMenuStrip = contextMenu;
        }

        #region Game Overlay Injection

        private void ShowInjectIntoGameDialog()
        {
            using (var processSelector = new ProcessSelectorDialog())
            {
                if (processSelector.ShowDialog() == DialogResult.OK)
                {
                    int processId = processSelector.SelectedProcessId;
                    if (processId > 0)
                    {
                        InjectIntoGame(processId);
                    }
                }
            }
        }

        private void InjectIntoGame(int processId)
        {
            try
            {
                // Check if injection library exists
                string injectionLibraryPath = Path.Combine(
                    Path.GetDirectoryName(typeof(DirectXOverlayForm).Assembly.Location),
                    "GameOverlayInjection.dll");

                if (!File.Exists(injectionLibraryPath))
                {
                    MessageBox.Show(
                        "The required injection library (GameOverlayInjection.dll) was not found in the application directory.",
                        "Missing Library",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Convert our crosshair settings to format expected by injector
                SharpDX.Mathematics.Interop.RawColor4 crosshairColor = new SharpDX.Mathematics.Interop.RawColor4(
                    _crosshairInfo.ColorR / 255.0f,
                    _crosshairInfo.ColorG / 255.0f,
                    _crosshairInfo.ColorB / 255.0f,
                    _crosshairInfo.Alpha / 255.0f);

                // Perform the injection
                _injector.Inject(processId);

                // Set the crosshair settings in the injected process
                _injector.SetCrosshairSettings(
                    (int)_crosshairInfo.Size,  // Cast to int if Size is float
                    (int)_crosshairInfo.Gap,   // Cast to int if Gap is float
                    (int)_crosshairInfo.Thickness,  // Cast to int if Thickness is float
                    _crosshairInfo.Dot,
                    new SharpDX.Color4(
                        _crosshairInfo.ColorR / 255.0f,
                        _crosshairInfo.ColorG / 255.0f,
                        _crosshairInfo.ColorB / 255.0f,
                        _crosshairInfo.Alpha / 255.0f));

                // Hide our own overlay when using injection
                SetOverlayVisibility(false);

                // Update injection status
                _isInjectionActive = true;
                _injectedProcessId = processId;

                // Update menu items
                UpdateInjectionMenuItems();

                MessageBox.Show(
                    $"Successfully injected crosshair overlay into process with ID {processId}.\n\n" +
                    "The overlay form will be hidden while injection is active.",
                    "Injection Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to inject into process: {ex.Message}\n\n{ex.StackTrace}",
                    "Injection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void StopActiveInjection()
        {
            if (_isInjectionActive)
            {
                try
                {
                    // Set the injected overlay to not visible
                    _injector.SetVisible(false);

                    // Notify user
                    MessageBox.Show(
                        "Injection has been disabled. Note that the DLL will remain loaded in the target process until it exits.",
                        "Injection Stopped",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error stopping injection: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    // Update state regardless of success/failure
                    _isInjectionActive = false;
                    _injectedProcessId = -1;

                    // Update menu items
                    UpdateInjectionMenuItems();

                    // Show our overlay again
                    SetOverlayVisibility(true);
                }
            }
        }

        private void UpdateInjectionMenuItems()
        {
            // Find the menu items
            ToolStripMenuItem stopInjectionItem = null;

            foreach (ToolStripItem item in _trayIcon.ContextMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text == "Stop Active Injection")
                    {
                        stopInjectionItem = menuItem;
                    }
                }
            }

            // Update enabled state
            if (stopInjectionItem != null)
            {
                stopInjectionItem.Enabled = _isInjectionActive;
            }
        }

        private void SetOverlayVisibility(bool visible)
        {
            _isVisible = visible;
            if (visible)
            {
                Show();
                Activate();
                ForceWindowToTop();
            }
            else
            {
                Hide();
            }
        }

        #endregion

        #region Process Selector Dialog

        private class ProcessSelectorDialog : Form
        {
            private ListView _processListView;
            private Button _okButton;
            private Button _cancelButton;
            private Button _refreshButton;

            public int SelectedProcessId { get; private set; } = -1;

            public ProcessSelectorDialog()
            {
                InitializeComponent();
                PopulateProcessList();
            }

            private void InitializeComponent()
            {
                _processListView = new ListView
                {
                    Dock = DockStyle.Fill,
                    FullRowSelect = true,
                    MultiSelect = false,
                    View = View.Details,
                    Sorting = SortOrder.Ascending
                };

                _processListView.Columns.Add("Process ID", 80);
                _processListView.Columns.Add("Process Name", 200);
                _processListView.Columns.Add("Window Title", 300);

                _okButton = new Button
                {
                    Text = "Inject",
                    DialogResult = DialogResult.OK,
                    Enabled = false
                };

                _cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel
                };

                _refreshButton = new Button
                {
                    Text = "Refresh List"
                };

                _refreshButton.Click += (s, e) => PopulateProcessList();

                _processListView.SelectedIndexChanged += (s, e) =>
                {
                    if (_processListView.SelectedItems.Count > 0)
                    {
                        ListViewItem item = _processListView.SelectedItems[0];
                        SelectedProcessId = int.Parse(item.Text);
                        _okButton.Enabled = true;
                    }
                    else
                    {
                        SelectedProcessId = -1;
                        _okButton.Enabled = false;
                    }
                };

                var buttonPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 40,
                    Padding = new Padding(10)
                };

                buttonPanel.Controls.Add(_cancelButton);
                buttonPanel.Controls.Add(_okButton);
                buttonPanel.Controls.Add(_refreshButton);

                Controls.Add(_processListView);
                Controls.Add(buttonPanel);

                AcceptButton = _okButton;
                CancelButton = _cancelButton;

                Text = "Select Process to Inject Into";
                Size = new Size(600, 500);
                StartPosition = FormStartPosition.CenterScreen;
            }

            private void PopulateProcessList()
            {
                _processListView.Items.Clear();

                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        // Skip processes without a window
                        if (process.MainWindowHandle == IntPtr.Zero)
                            continue;

                        // Skip system processes
                        if (process.Id <= 4)
                            continue;

                        // Skip our own process
                        if (process.Id == Process.GetCurrentProcess().Id)
                            continue;

                        ListViewItem item = new ListViewItem(process.Id.ToString());
                        item.SubItems.Add(process.ProcessName);
                        item.SubItems.Add(process.MainWindowTitle);

                        _processListView.Items.Add(item);
                    }
                    catch
                    {
                        // Skip any process we can't access
                    }
                }
            }
        }

        #endregion

        #region Window Management

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOOWNERZORDER = 0x0200;

        private void SetExtendedStyleFlags()
        {
            // Set extended window style flags to ensure the window behaves properly as an overlay
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLong(Handle, GWL_EXSTYLE, exStyle);

            // Apply the changes
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        private void SetClickThroughEnabled(bool enabled)
        {
            var exStyle = GetWindowLong(Handle, GWL_EXSTYLE);

            if (enabled)
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            else
                exStyle &= ~WS_EX_TRANSPARENT;

            SetWindowLong(Handle, GWL_EXSTYLE, exStyle);
        }

        private void SetTopMost(bool topMost)
        {
            if (topMost)
            {
                SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        private void EnsureTopMost()
        {
            // Check if we need to re-establish topmost
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private void ForceWindowToTop()
        {
            // Save current foreground window
            IntPtr foregroundWindow = GetForegroundWindow();

            // Set our window to topmost again
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            // Force a redraw
            Invalidate();

            // Restore foreground window if it wasn't our window
            if (foregroundWindow != Handle && foregroundWindow != IntPtr.Zero)
            {
                SetForegroundWindow(foregroundWindow);
            }

            Console.WriteLine("Forced window to top");
        }

        private void SetFormToScreen(Screen screen)
        {
            Bounds = screen.Bounds;
            Location = new Point(screen.Bounds.X, screen.Bounds.Y);
            Size = new Size(screen.Bounds.Width, screen.Bounds.Height);
            Invalidate();
        }

        #endregion

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // In DirectX mode, immediately prompt for crosshair code
            ShowLoadCrosshairDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up any active injections
            if (_isInjectionActive)
            {
                StopActiveInjection();
            }

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
                _trayIcon?.Dispose();
                _directXRenderer?.Dispose();
                _windowTrackingTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ShowLoadCrosshairDialog()
        {
            using (var inputDialog = new CrosshairInputDialog("Enter CS:GO Crosshair Share Code", "Share Code:"))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // The crosshair is already validated in the dialog
                        _crosshairInfo = inputDialog.CrosshairInfo;

                        // Force debug text to be on for testing
                        _crosshairInfo.ShowDebugText = true;

                        // Make sure T is true to ensure lines are drawn
                        if (!_crosshairInfo.T)
                        {
                            Console.WriteLine("Warning: T was false, setting to true for testing");
                            _crosshairInfo.T = true;
                        }

                        // Log the loaded crosshair info
                        Console.WriteLine($"Loaded crosshair - Size: {_crosshairInfo.Size}, Gap: {_crosshairInfo.Gap}, " +
                                         $"Thickness: {_crosshairInfo.Thickness}, T: {_crosshairInfo.T}, Dot: {_crosshairInfo.Dot}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error applying crosshair: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);

                        // Create a default crosshair in case of error
                        _crosshairInfo = new CrosshairInfo();
                        _crosshairInfo.ShowDebugText = true;
                        Console.WriteLine("Created default crosshair due to error");
                    }
                }
                else
                {
                    // User cancelled without entering a crosshair code
                    // For DirectX mode, we'll use a default crosshair instead of exiting
                    if (_crosshairInfo == null)
                    {
                        _crosshairInfo = new CrosshairInfo();
                        _crosshairInfo.ShowDebugText = true;
                        Console.WriteLine("Created default crosshair after dialog cancel");

                        MessageBox.Show("Using default crosshair settings.",
                            "Default Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
    }
}