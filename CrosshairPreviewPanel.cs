using System;
using System.Drawing;
using System.Windows.Forms;

namespace poji
{
    public sealed class CrosshairPreviewPanel : UserControl
    {
        private CsgoCrosshairDecoder.CrosshairInfo _crosshairInfo;
        private readonly CrosshairRenderer _renderer;
        private bool _isValid = false;
        
        public bool IsValid => _isValid;
        
        public string ValidationMessage { get; private set; } = string.Empty;
        
        public event EventHandler ValidationStatusChanged;
        
        public enum BackgroundType
        {
            Neutral,
            Dust2,
            Mirage,
            Inferno,
            Dark,
            Light
        }

        private BackgroundType _currentBackground = BackgroundType.Neutral;
        private Bitmap _backgroundImage = null;

        public BackgroundType Background
        {
            get => _currentBackground;
            set
            {
                if (_currentBackground != value)
                {
                    _currentBackground = value;
                    LoadBackgroundImage();
                    Invalidate();
                }
            }
        }

        private void LoadBackgroundImage()
        {
            // Dispose of previous image if it exists
            _backgroundImage?.Dispose();
    
            // Load new background based on selection
            switch (_currentBackground)
            {
                case BackgroundType.Dust2:
                    //_backgroundImage = Properties.Resources.bg_dust2;
                    break;
                case BackgroundType.Mirage:
                    //_backgroundImage = Properties.Resources.bg_mirage; // add these images later
                    break;
                case BackgroundType.Inferno:
                    //_backgroundImage = Properties.Resources.bg_inferno;
                    break;
                case BackgroundType.Dark:
                    _backgroundImage = null;
                    BackColor = Color.Black;
                    break;
                case BackgroundType.Light:
                    _backgroundImage = null;
                    BackColor = Color.LightGray;
                    break;
                case BackgroundType.Neutral:
                default:
                    _backgroundImage = null;
                    BackColor = Color.DimGray;
                    break;
            }
        }

        public CrosshairPreviewPanel()
        {
            // Set up the control
            DoubleBuffered = true;
            BackColor = Color.DimGray; // Neutral background to show crosshair clearly
            MinimumSize = new Size(200, 200);
            
            // Initialize the renderer
            _renderer = new CrosshairRenderer();
        }
        
        public bool TrySetCrosshair(string shareCode)
        {
            try
            {
                // Clear previous validation state
                _isValid = false;
                ValidationMessage = string.Empty;
                
                // Check for empty input
                if (string.IsNullOrWhiteSpace(shareCode))
                {
                    ValidationMessage = "Please enter a crosshair share code.";
                    ValidationStatusChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                    return false;
                }
                
                // Try to decode the share code
                var decoder = new CsgoCrosshairDecoder();
                var crosshairInfo = decoder.DecodeShareCodeToCrosshairInfo(shareCode);
                
                // Validate the crosshair properties
                if (!ValidateCrosshairProperties(crosshairInfo))
                {
                    ValidationStatusChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                    return false;
                }
                
                // If we got here, the crosshair is valid
                _crosshairInfo = crosshairInfo;
                _renderer.CrosshairInfo = _crosshairInfo;
                _renderer.ScaleFactor = 1.0f; // Default scale for preview
                _isValid = true;
                ValidationMessage = "Valid crosshair code!";
                
                ValidationStatusChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
                return true;
            }
            catch (ArgumentException ex)
            {
                _isValid = false;
                ValidationMessage = $"Invalid format: {ex.Message}";
                ValidationStatusChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
                return false;
            }
            catch (Exception ex)
            {
                _isValid = false;
                ValidationMessage = $"Error: {ex.Message}";
                ValidationStatusChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
                return false;
            }
        }
        
        private bool ValidateCrosshairProperties(CsgoCrosshairDecoder.CrosshairInfo crosshairInfo)
        {
            // Check for extreme values that might cause rendering issues
            if (crosshairInfo.Length > 100)
            {
                ValidationMessage = "Warning: Crosshair length is very large.";
                return false;
            }
            
            if (crosshairInfo.Thickness > 20)
            {
                ValidationMessage = "Warning: Crosshair thickness is very large.";
                return false;
            }
            
            if (crosshairInfo.Gap < -20 || crosshairInfo.Gap > 20)
            {
                ValidationMessage = "Warning: Crosshair gap value is extreme.";
                return false;
            }
            
            // More validations can be added here as needed
            
            return true;
        }
        
        public CsgoCrosshairDecoder.CrosshairInfo GetCrosshairInfo()
        {
            return _crosshairInfo?.Clone();
        }
        
        public void SetScale(float scale)
        {
            if (_renderer != null)
            {
                _renderer.ScaleFactor = scale;
                Invalidate();
            }
        }
        
        // Modify the OnPaint method to include background drawing
        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw background
            if (_backgroundImage != null)
            {
                // Center and scale the background image
                e.Graphics.DrawImage(_backgroundImage, 0, 0, Width, Height);
            }
            else
            {
                // Use solid background color (set in LoadBackgroundImage)
                e.Graphics.Clear(BackColor);
            }
    
            // Draw crosshair
            if (_isValid && _crosshairInfo != null)
            {
                _renderer.Draw(e.Graphics, Width / 2, Height / 2);
            }
            else
            {
                // Draw a message
                using (var brush = new SolidBrush(Color.White))
                using (var font = new Font("Arial", 10))
                {
                    string message = _isValid ? "" : "Preview not available";
                    SizeF textSize = e.Graphics.MeasureString(message, font);
                    e.Graphics.DrawString(message, font, brush, 
                        (Width - textSize.Width) / 2, 
                        (Height - textSize.Height) / 2);
                }
            }
        }
        
        // Clean up resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }
        
    }
}