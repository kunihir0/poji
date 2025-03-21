using System.Drawing;
using System.Drawing.Drawing2D;

namespace poji
{
    public class CrosshairRenderer
    {
        public CsgoCrosshairDecoder.CrosshairInfo CrosshairInfo { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;
        
        // Default dot properties
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
            
            // Use the getters for proper scaling
            float length = CrosshairInfo.GetScaledLength();
            float thickness = CrosshairInfo.GetScaledThickness();
            float gap = CrosshairInfo.GetScaledGap();
            float outlineThickness = CrosshairInfo.GetScaledOutline();
            
            // Apply calculations from CsgoCrosshairDecoder
            float renderSize = CsgoCrosshairDecoder.RoundUpToOdd(2 * length);
            float renderThickness = (float)System.Math.Floor(CsgoCrosshairDecoder.RoundUpToOdd(2 * thickness) / 2);
            float renderGap = CsgoCrosshairDecoder.RoundUpToOdd(2 * CsgoCrosshairDecoder.MapGapValue(gap));
            
            // Get colors
            Color mainColor = CrosshairInfo.GetColor();
            Color outlineColor = CrosshairInfo.GetOutlineColor();
            
            // Draw crosshair
            using (var mainPen = new Pen(mainColor, renderThickness))
            using (var outlinePen = new Pen(outlineColor, renderThickness + (CrosshairInfo.HasOutline ? outlineThickness * 2 : 0)))
            using (var dotBrush = new SolidBrush(mainColor))
            using (var outlineDotBrush = new SolidBrush(outlineColor))
            {
                float halfRenderGap = renderGap / 2;
                
                // Horizontal lines coordinates
                float leftLineStart = centerX - renderSize / 2 - halfRenderGap;
                float leftLineEnd = centerX - halfRenderGap;
                float rightLineStart = centerX + halfRenderGap;
                float rightLineEnd = centerX + renderSize / 2 + halfRenderGap;
                
                // Vertical lines coordinates
                float topLineStart = centerY - renderSize / 2 - halfRenderGap;
                float topLineEnd = centerY - halfRenderGap;
                float bottomLineStart = centerY + halfRenderGap;
                float bottomLineEnd = centerY + renderSize / 2 + halfRenderGap;

                // Draw outline first if enabled
                if (CrosshairInfo.HasOutline)
                {
                    // Horizontal lines
                    g.DrawLine(outlinePen, leftLineStart, centerY, leftLineEnd, centerY);
                    g.DrawLine(outlinePen, rightLineStart, centerY, rightLineEnd, centerY);

                    // Vertical lines
                    if (!CrosshairInfo.IsTStyle)
                    {
                        g.DrawLine(outlinePen, centerX, topLineStart, centerX, topLineEnd);
                    }
                    g.DrawLine(outlinePen, centerX, bottomLineStart, centerX, bottomLineEnd);
                    
                    // Center dot outline
                    if (CrosshairInfo.HasCenterDot)
                    {
                        float dotSize = renderThickness * 1.5f + (outlineThickness * 2);
                        g.FillEllipse(outlineDotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
                    }
                }

                // Draw main crosshair lines
                // Horizontal lines
                g.DrawLine(mainPen, leftLineStart, centerY, leftLineEnd, centerY);
                g.DrawLine(mainPen, rightLineStart, centerY, rightLineEnd, centerY);

                // Vertical lines
                if (!CrosshairInfo.IsTStyle)
                {
                    g.DrawLine(mainPen, centerX, topLineStart, centerX, topLineEnd);
                }
                g.DrawLine(mainPen, centerX, bottomLineStart, centerX, bottomLineEnd);

                // Draw center dot
                if (CrosshairInfo.HasCenterDot)
                {
                    float dotSize = renderThickness * 1.5f;
                    g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
                }
            }
            
            // Restore original scale factor
            CrosshairInfo.ScaleFactor = originalScaleFactor;
        }
    }
}