using System.Drawing;
using System.Drawing.Drawing2D;

namespace poji
{
    public class CrosshairRenderer
    {
        public CsgoCrosshairDecoder.CrosshairInfo CrosshairInfo { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;
        
        // Default dot properties (used as fallback)
        private readonly Color _defaultDotColor = Color.Red;
        private readonly int _defaultDotSize = 10;

        public void Draw(Graphics g, int centerX, int centerY)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            if (CrosshairInfo != null)
            {
                DrawCsgoCrosshair(g, centerX, centerY);
            }
            else
            {
                // Draw fallback tiny dot
                using (var brush = new SolidBrush(_defaultDotColor))
                {
                    int size = (int)(_defaultDotSize * ScaleFactor);
                    g.FillEllipse(brush, centerX - size/2, centerY - size/2, size, size);
                }
            }
        }
        
        private void DrawCsgoCrosshair(Graphics g, int centerX, int centerY)
        {
            // Store original scale factor and apply our own
            float originalScaleFactor = CrosshairInfo.ScaleFactor;
            CrosshairInfo.ScaleFactor = ScaleFactor;
            
            // Get scaled dimensions
            float halfLength = CrosshairInfo.GetScaledLength() / 2;
            float halfThickness = CrosshairInfo.GetScaledThickness() / 2; // this is not used.
            float halfGap = CrosshairInfo.GetScaledGap() / 2;
            float outlineThickness = CrosshairInfo.GetScaledOutline();
            
            // Get colors
            Color mainColor = CrosshairInfo.GetColor();
            Color outlineColor = CrosshairInfo.GetOutlineColor();
            
            // Draw crosshair
            using (var mainPen = new Pen(mainColor, CrosshairInfo.GetScaledThickness()))
            using (var outlinePen = new Pen(outlineColor, CrosshairInfo.GetScaledThickness() + (CrosshairInfo.HasOutline ? outlineThickness * 2 : 0)))
            using (var dotBrush = new SolidBrush(mainColor))
            {
                // Draw outline first if enabled
                if (CrosshairInfo.HasOutline)
                {
                    // Horizontal lines
                    g.DrawLine(outlinePen, centerX - halfLength - halfGap, centerY, centerX - halfGap, centerY);
                    g.DrawLine(outlinePen, centerX + halfGap, centerY, centerX + halfLength + halfGap, centerY);

                    // Vertical lines
                    if (!CrosshairInfo.IsTStyle)
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
                if (!CrosshairInfo.IsTStyle)
                {
                    g.DrawLine(mainPen, centerX, centerY - halfLength - halfGap, centerX, centerY - halfGap);
                }
                g.DrawLine(mainPen, centerX, centerY + halfGap, centerX, centerY + halfLength + halfGap);

                // Draw center dot if enabled
                if (CrosshairInfo.HasCenterDot)
                {
                    float dotSize = CrosshairInfo.GetScaledThickness() * 1.5f;
                    g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
                }
            }
            
            // Restore original scale factor
            CrosshairInfo.ScaleFactor = originalScaleFactor;
        }
    }
}