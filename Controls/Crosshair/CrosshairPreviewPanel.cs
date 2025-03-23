using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Models;
using poji.Rendering;
using poji.Utils;
using Color = System.Drawing.Color;

namespace poji.Controls.Crosshair
{
    /// <summary>
    /// UserControl for displaying and previewing crosshair configurations.
    /// </summary>
    public sealed class CrosshairPreviewPanel : UserControl
    {
        private CrosshairInfo _crosshairInfo;
        private readonly CrosshairRenderer _renderer;
        private bool _isValid;
        private Bitmap _backgroundImage;
        private BackgroundType _currentBackground = BackgroundType.Neutral;

        /// <summary>
        /// Gets whether the currently loaded crosshair configuration is valid.
        /// </summary>
        public bool IsValid => _isValid;

        /// <summary>
        /// Gets the validation message for the current crosshair configuration.
        /// </summary>
        public string ValidationMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Event triggered when validation status changes.
        /// </summary>
        public event EventHandler ValidationStatusChanged;

        /// <summary>
        /// Background types available for crosshair preview.
        /// </summary>
        public enum BackgroundType
        {
            Neutral,
            Dust2,
            Mirage,
            Inferno,
            Dark,
            Light
        }

        /// <summary>
        /// Gets or sets the background type for the crosshair preview.
        /// </summary>
        public BackgroundType Background
        {
            get => _currentBackground;
            set
            {
                if (_currentBackground == value)
                    return;

                _currentBackground = value;
                LoadBackgroundImage();
                Invalidate();
            }
        }

        /// <summary>
        /// Initializes a new instance of the CrosshairPreviewPanel.
        /// </summary>
        public CrosshairPreviewPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.DimGray;
            MinimumSize = new Size(200, 200);

            _renderer = new CrosshairRenderer();
            _isValid = false;
        }

        /// <summary>
        /// Attempts to set and validate a crosshair from a share code.
        /// </summary>
        /// <param name="shareCode">The crosshair share code to parse.</param>
        /// <returns>True if the crosshair is valid, false otherwise.</returns>
        public bool TrySetCrosshair(string shareCode)
        {
            try
            {
                ResetValidationState();

                if (string.IsNullOrWhiteSpace(shareCode))
                {
                    SetInvalidState("Please enter a crosshair share code.");
                    return false;
                }

                var decoder = new CsgoCrosshairDecoder();
                var crosshairInfo = decoder.DecodeShareCodeToCrosshairInfo(shareCode);

                if (!ValidateCrosshairProperties(crosshairInfo))
                {
                    NotifyValidationStatusChanged();
                    return false;
                }

                SetValidCrosshair(crosshairInfo);
                return true;
            }
            catch (ArgumentException ex)
            {
                SetInvalidState($"Invalid format: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                SetInvalidState($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a copy of the current crosshair information.
        /// </summary>
        /// <returns>A clone of the current crosshair information.</returns>
        public CrosshairInfo GetCrosshairInfo()
        {
            return _crosshairInfo?.Clone();
        }

        /// <summary>
        /// Sets the scale factor for the crosshair rendering.
        /// </summary>
        /// <param name="scale">The scale factor to apply.</param>
        public void SetScale(float scale)
        {
            if (_renderer != null)
            {
                _renderer.ScaleFactor = scale;
                Invalidate();
            }
        }

        /// <summary>
        /// Paints the control with background and crosshair.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            DrawBackground(e.Graphics);
            DrawCrosshair(e.Graphics);
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Private Methods

        private void ResetValidationState()
        {
            _isValid = false;
            ValidationMessage = string.Empty;
        }

        private void SetInvalidState(string message)
        {
            _isValid = false;
            ValidationMessage = message;
            NotifyValidationStatusChanged();
            Invalidate();
        }

        private void SetValidCrosshair(CrosshairInfo crosshairInfo)
        {
            _crosshairInfo = crosshairInfo;
            _renderer.CrosshairInfo = _crosshairInfo;
            _renderer.ScaleFactor = 1.0f;
            _isValid = true;
            ValidationMessage = "Valid crosshair code!";

            NotifyValidationStatusChanged();
            Invalidate();
        }

        private void NotifyValidationStatusChanged()
        {
            ValidationStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateCrosshairProperties(CrosshairInfo crosshairInfo)
        {
            if (crosshairInfo.Length > 1000)
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

            return true;
        }

        private void LoadBackgroundImage()
        {
            _backgroundImage?.Dispose();

            switch (_currentBackground)
            {
                case BackgroundType.Dust2:
                    //_backgroundImage = Properties.Resources.bg_dust2;
                    break;
                case BackgroundType.Mirage:
                    //_backgroundImage = Properties.Resources.bg_mirage;
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

        private void DrawBackground(Graphics graphics)
        {
            if (_backgroundImage != null)
            {
                graphics.DrawImage(_backgroundImage, 0, 0, Width, Height);
            }
            else
            {
                graphics.Clear(BackColor);
            }
        }

        private void DrawCrosshair(Graphics graphics)
        {
            if (_isValid && _crosshairInfo != null)
            {
                _renderer.Draw(graphics, Width / 2, Height / 2);
            }
            else
            {
                DrawPreviewUnavailableMessage(graphics);
            }
        }

        private void DrawPreviewUnavailableMessage(Graphics graphics)
        {
            using (var brush = new SolidBrush(Color.White))
            using (var font = new Font("Arial", 10))
            {
                string message = _isValid ? "" : "Preview not available";
                SizeF textSize = graphics.MeasureString(message, font);
                graphics.DrawString(message, font, brush,
                    (Width - textSize.Width) / 2,
                    (Height - textSize.Height) / 2);
            }
        }

        #endregion
    }
}