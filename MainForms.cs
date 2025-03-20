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

        // Constants for window styles
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

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

        public MainForm()
        {
            InitializeComponent();
            InitializeForm();
            InitializeTrayIcon();
            SetupHotkey();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "MainForm";
            this.Text = "Crosshair Overlay";
            this.ResumeLayout(false);
        }

        private void InitializeForm()
        {
            // Basic form setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;

            // Set form to cover entire primary screen
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.Location = new Point(0, 0);

            // Make the form background completely transparent
            this.BackColor = Color.LimeGreen; // Use a color that's not in your UI
            this.TransparencyKey = Color.LimeGreen;

            // Set window styles for click-through
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("About", null, OnAboutClicked);
            trayMenu.Items.Add("Load Crosshair Code", null, OnLoadCrosshairClicked);
            trayMenu.Items.Add("Use Simple Dot", null, OnSimpleDotClicked);
            trayMenu.Items.Add("Config", null, OnConfigClicked);
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
            // Check for Alt+Shift+W
            bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
            bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;

            if (altPressed && shiftPressed && e.KeyCode == Keys.W)
            {
                ExitApplication();
            }
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            // Toggle visibility
            if (this.Visible)
                this.Hide();
            else
                this.Show();
        }

        private void OnAboutClicked(object sender, EventArgs e)
        {
            MessageBox.Show("CS:GO Crosshair Overlay\nVersion 1.0", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        useCustomCrosshair = true;
                        
                        // Get the color from the crosshair info
                        dotColor = crosshairInfo.GetColor();
                        
                        // Save to settings if you want
                        SaveCrosshairToSettings(shareCode);
                        
                        // Force redraw
                        this.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error decoding crosshair: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnSimpleDotClicked(object sender, EventArgs e)
        {
            useCustomCrosshair = false;
            dotColor = Color.Red;
            dotSize = 10;
            this.Invalidate();
        }

        private void OnConfigClicked(object sender, EventArgs e)
        {
            using (var configDialog = new ConfigDialog(dotColor, dotSize))
            {
                if (configDialog.ShowDialog() == DialogResult.OK)
                {
                    dotColor = configDialog.SelectedColor;
                    dotSize = configDialog.SelectedSize;
                    this.Invalidate();
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
            // Simple implementation - you might want to use Properties.Settings instead
            try
            {
                File.WriteAllText("crosshair_code.txt", shareCode);
            }
            catch
            {
                // Ignore errors
            }
        }

        private void LoadCrosshairFromSettings()
        {
            try
            {
                if (File.Exists("crosshair_code.txt"))
                {
                    string shareCode = File.ReadAllText("crosshair_code.txt");
                    var decoder = new CSGOCrosshairDecoder();
                    byte[] bytes = decoder.DecodeShareCode(shareCode);
                    crosshairInfo = decoder.DecodeCrosshairInfo(bytes);
                    useCustomCrosshair = true;
                    dotColor = crosshairInfo.GetColor();
                }
            }
            catch
            {
                // Fallback to default dot
                useCustomCrosshair = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Calculate center position
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            
            // Enable anti-aliasing
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (useCustomCrosshair && crosshairInfo != null)
            {
                DrawCrosshair(e.Graphics, centerX, centerY);
            }
            else
            {
                // Draw simple dot
                using (var brush = new SolidBrush(dotColor))
                {
                    e.Graphics.FillEllipse(
                        brush,
                        centerX - dotSize / 2,
                        centerY - dotSize / 2,
                        dotSize,
                        dotSize
                    );
                }
            }
        }

        private void DrawCrosshair(Graphics g, int centerX, int centerY)
        {
            // Set up pens for drawing
            using (var mainPen = new Pen(Color.FromArgb(crosshairInfo.Alpha, crosshairInfo.Red, crosshairInfo.Green, crosshairInfo.Blue), crosshairInfo.Thickness))
            using (var outlinePen = new Pen(Color.Black, crosshairInfo.Thickness + (crosshairInfo.HasOutline ? crosshairInfo.Outline * 2 : 0)))
            using (var dotBrush = new SolidBrush(Color.FromArgb(crosshairInfo.Alpha, crosshairInfo.Red, crosshairInfo.Green, crosshairInfo.Blue)))
            {
                // Calculate dimensions
                float halfLength = crosshairInfo.Length / 2;
                float halfThickness = crosshairInfo.Thickness / 2;
                float halfGap = crosshairInfo.Gap / 2;

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
                    float dotSize = crosshairInfo.Thickness * 1.5f;
                    g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadCrosshairFromSettings();
        }

        // Allow form to close when exit triggered
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

    // Configuration dialog for simple dot mode
    public class ConfigDialog : Form
    {
        private Button okButton;
        private Button cancelButton;
        private Label colorLabel;
        private Label sizeLabel;
        private NumericUpDown sizeNumeric;
        private Button colorButton;
        private ColorDialog colorDialog;

        public Color SelectedColor { get; private set; }
        public int SelectedSize { get; private set; }

        public ConfigDialog(Color initialColor, int initialSize)
        {
            this.Text = "Crosshair Configuration";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(300, 200);

            SelectedColor = initialColor;
            SelectedSize = initialSize;

            colorLabel = new Label
            {
                Text = "Color:",
                Location = new Point(10, 20),
                Size = new Size(100, 20)
            };

            colorButton = new Button
            {
                BackColor = initialColor,
                Location = new Point(120, 20),
                Size = new Size(50, 20)
            };
            colorButton.Click += ColorButton_Click;

            sizeLabel = new Label
            {
                Text = "Size:",
                Location = new Point(10, 50),
                Size = new Size(100, 20)
            };

            sizeNumeric = new NumericUpDown
            {
                Location = new Point(120, 50),
                Size = new Size(50, 20),
                Minimum = 1,
                Maximum = 50,
                Value = initialSize
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(120, 120),
                Size = new Size(75, 23)
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(200, 120),
                Size = new Size(75, 23)
            };

            colorDialog = new ColorDialog
            {
                AnyColor = true,
                FullOpen = true,
                Color = initialColor
            };

            this.Controls.Add(colorLabel);
            this.Controls.Add(colorButton);
            this.Controls.Add(sizeLabel);
            this.Controls.Add(sizeNumeric);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                colorButton.BackColor = colorDialog.Color;
                SelectedColor = colorDialog.Color;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SelectedSize = (int)sizeNumeric.Value;
        }
    }
}