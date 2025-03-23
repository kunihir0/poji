using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using poji.Enums;
using poji.Models;
using poji.Utils;
using Color = System.Drawing.Color;

namespace poji.Rendering
{
    /// <summary>
    /// Renders crosshairs based on CrosshairInfo configurations.
    /// </summary>
    public class CrosshairRenderer
    {
        private const float DefaultDotSizeMultiplier = 1.5f;
        private readonly Color _defaultDotColor = Color.Red;
        private readonly int _defaultDotSize = 10;
        
        // Recoil properties
        private float _recoilOffsetY = 0;
        private DateTime _lastRecoilUpdate = DateTime.MinValue;
        private float _recoilPhase = 0;
        private const float RecoilSpeed = 0.01f;
        private const float RecoilAmplitude = 8.0f;

        public CrosshairInfo CrosshairInfo { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;
        public bool SimulateRecoil { get; set; } = false;

        /// <summary>
        /// Controls what parts of the crosshair are drawn
        /// </summary>
        public enum RenderMode
        {
            Full,
            MainOnly,
            DotOnly
        }

        public RenderMode Mode { get; set; } = RenderMode.Full;

        /// <summary>
        /// Draws the crosshair at specified coordinates
        /// </summary>
        public void Draw(Graphics g, int centerX, int centerY)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            if (CrosshairInfo != null)
                DrawCrosshair(g, centerX, centerY);
            else
                DrawFallbackDot(g, centerX, centerY);
        }

        private void DrawFallbackDot(Graphics g, int centerX, int centerY)
        {
            using (var brush = new SolidBrush(_defaultDotColor))
            {
                int size = (int)(_defaultDotSize * ScaleFactor);
                g.FillEllipse(brush, centerX - size / 2, centerY - size / 2, size, size);
            }
        }

        private void DrawCrosshair(Graphics g, int centerX, int centerY)
        {
            // Use renderer's scale factor without modifying CrosshairInfo
            float effectiveScale = ScaleFactor;
            
            var dimensions = GetDimensions(effectiveScale);
            UpdateRecoilOffset();
            
            Color mainColor = CrosshairInfo.GetColor();
            Color outlineColor = CrosshairInfo.GetOutlineColor();
            
            using (var mainPen = new Pen(mainColor, dimensions.Thickness))
            using (var outlinePen = new Pen(outlineColor, dimensions.Thickness + (CrosshairInfo.HasOutline ? dimensions.OutlineThickness * 2 : 0)))
            using (var dotBrush = new SolidBrush(mainColor))
            using (var outlineDotBrush = new SolidBrush(outlineColor))
            {
                var coords = GetCoordinates(centerX, centerY, dimensions, effectiveScale);
                
                if (Mode == RenderMode.DotOnly || CrosshairInfo.DotOnly)
                {
                    DrawDotOnly(g, dotBrush, outlineDotBrush, centerX, centerY, dimensions);
                }
                else
                {
                    if (CrosshairInfo.HasOutline)
                        DrawOutlines(g, outlinePen, outlineDotBrush, coords, dimensions, centerX, centerY);
                    
                    bool drawMainCrosshair = !(CrosshairInfo.SplitDistance > 0 && Mode == RenderMode.Full);
                    
                    if (drawMainCrosshair)
                        DrawMainCrosshair(g, mainPen, dotBrush, coords, dimensions, centerX, centerY);
                    
                    if (CrosshairInfo.SplitDistance > 0 && Mode == RenderMode.Full)
                        DrawSplitCrosshair(g, coords, dimensions, centerX, centerY, effectiveScale);
                }
                
                if (CrosshairInfo.ShowDebugText)
                    DrawDebugInfo(g, centerX, centerY);
            }
        }

        private void UpdateRecoilOffset()
        {
            bool isDynamicStyle = CrosshairInfo.Style == CrosshairStyle.Default ||
                                  CrosshairInfo.Style == CrosshairStyle.ClassicDynamic ||
                                  CrosshairInfo.Style == CrosshairStyle.Classic;
            
            if (CrosshairInfo.FollowRecoil && SimulateRecoil && isDynamicStyle)
            {
                var now = DateTime.Now;
                var elapsed = (now - _lastRecoilUpdate).TotalSeconds;
                _lastRecoilUpdate = now;
                
                _recoilPhase += (float)(elapsed * RecoilSpeed * Math.PI * 2);
                while (_recoilPhase > Math.PI * 2)
                    _recoilPhase -= (float)(Math.PI * 2);
                
                _recoilOffsetY = (float)Math.Sin(_recoilPhase) * RecoilAmplitude * 
                                (float)Math.Max(0, 1 - (_recoilPhase / (Math.PI * 10)));
            }
            else
            {
                _recoilOffsetY = 0;
            }
        }

        private (float Length, float Thickness, float Gap, float OutlineThickness) GetDimensions(float scale)
        {
            // Apply scaling directly here without modifying CrosshairInfo
            float length = CrosshairInfo.GetRenderSize() * scale / 2;
            float thickness = CrosshairInfo.GetRenderThickness() * scale;
            
            float gap;
            if (CrosshairInfo.Style == CrosshairStyle.ClassicStatic && CrosshairInfo.DeployedWeaponGapEnabled)
            {
                gap = CrosshairInfo.FixedCrosshairGap * scale;
                gap = CrosshairUtils.RoundUpToOdd(2 * CrosshairUtils.MapGapValue(gap));
            }
            else
            {
                gap = CrosshairInfo.GetRenderGap() * scale;
            }
            
            float outlineThickness = CrosshairInfo.GetScaledOutline() * scale;
            
            return (length, thickness, gap, outlineThickness);
        }

        private (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
                float TopStart, float TopEnd, float BottomStart, float BottomEnd)
        GetCoordinates(int centerX, int centerY, (float Length, float Thickness, float Gap, float OutlineThickness) dimensions, float scale)
        {
            float halfGap = dimensions.Gap / 2;
            
            // Horizontal lines
            float leftStart = centerX - dimensions.Length - halfGap;
            float leftEnd = centerX - halfGap;
            float rightStart = centerX + halfGap;
            float rightEnd = centerX + dimensions.Length + halfGap;
            
            // Check if style is dynamic for recoil
            bool isDynamicStyle = CrosshairInfo.Style == CrosshairStyle.Default ||
                                  CrosshairInfo.Style == CrosshairStyle.ClassicDynamic ||
                                  CrosshairInfo.Style == CrosshairStyle.Classic;
            
            float recoilOffset = (CrosshairInfo.FollowRecoil && isDynamicStyle) ? _recoilOffsetY : 0;
            
            // Vertical lines
            float topStart = centerY - dimensions.Length - halfGap + recoilOffset;
            float topEnd = centerY - halfGap + recoilOffset;
            float bottomStart = centerY + halfGap + recoilOffset;
            float bottomEnd = centerY + dimensions.Length + halfGap + recoilOffset;
            
            return (leftStart, leftEnd, rightStart, rightEnd, topStart, topEnd, bottomStart, bottomEnd);
        }

        private void DrawDotOnly(Graphics g, SolidBrush dotBrush, SolidBrush outlineDotBrush, 
                               int centerX, int centerY, 
                               (float Length, float Thickness, float Gap, float OutlineThickness) dimensions)
        {
            if (CrosshairInfo.HasCenterDot || CrosshairInfo.Dot)
            {
                if (CrosshairInfo.HasOutline)
                {
                    float outlineDotSize = dimensions.Thickness * DefaultDotSizeMultiplier + (dimensions.OutlineThickness * 2);
                    g.FillEllipse(outlineDotBrush, centerX - outlineDotSize / 2, centerY - outlineDotSize / 2, outlineDotSize, outlineDotSize);
                }
                
                float dotSize = dimensions.Thickness * DefaultDotSizeMultiplier;
                g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
            }
        }

        private void DrawOutlines(Graphics g, Pen outlinePen, SolidBrush outlineDotBrush,
                                (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
                                 float TopStart, float TopEnd, float BottomStart, float BottomEnd) coords,
                                (float Length, float Thickness, float Gap, float OutlineThickness) dimensions,
                                int centerX, int centerY)
        {
            // Horizontal outlines
            g.DrawLine(outlinePen, coords.LeftStart, centerY, coords.LeftEnd, centerY);
            g.DrawLine(outlinePen, coords.RightStart, centerY, coords.RightEnd, centerY);
            
            // Vertical outlines (skip top line if T-style)
            if (!CrosshairInfo.IsTStyle && !CrosshairInfo.T)
                g.DrawLine(outlinePen, centerX, coords.TopStart, centerX, coords.TopEnd);
                
            g.DrawLine(outlinePen, centerX, coords.BottomStart, centerX, coords.BottomEnd);
            
            // Center dot outline
            if (CrosshairInfo.HasCenterDot || CrosshairInfo.Dot)
            {
                float dotSize = dimensions.Thickness * DefaultDotSizeMultiplier + (dimensions.OutlineThickness * 2);
                g.FillEllipse(outlineDotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
            }
        }

        private void DrawMainCrosshair(Graphics g, Pen mainPen, SolidBrush dotBrush,
                                     (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
                                      float TopStart, float TopEnd, float BottomStart, float BottomEnd) coords,
                                     (float Length, float Thickness, float Gap, float OutlineThickness) dimensions,
                                     int centerX, int centerY)
        {
            // Horizontal lines
            g.DrawLine(mainPen, coords.LeftStart, centerY, coords.LeftEnd, centerY);
            g.DrawLine(mainPen, coords.RightStart, centerY, coords.RightEnd, centerY);
            
            // Vertical lines (skip top line if T-style)
            if (!CrosshairInfo.IsTStyle && !CrosshairInfo.T)
                g.DrawLine(mainPen, centerX, coords.TopStart, centerX, coords.TopEnd);
                
            g.DrawLine(mainPen, centerX, coords.BottomStart, centerX, coords.BottomEnd);
            
            // Draw center dot
            if (CrosshairInfo.HasCenterDot || CrosshairInfo.Dot)
            {
                float dotSize = dimensions.Thickness * DefaultDotSizeMultiplier;
                g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
            }
        }

        private void DrawSplitCrosshair(Graphics g,
                                      (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
                                       float TopStart, float TopEnd, float BottomStart, float BottomEnd) coords,
                                      (float Length, float Thickness, float Gap, float OutlineThickness) dimensions,
                                      int centerX, int centerY, float scale)
        {
            Color mainColor = CrosshairInfo.GetColor();
            
            // Create colors with adjusted alpha values
            Color innerColor = System.Drawing.Color.FromArgb(
                (int)Math.Min(255, Math.Max(0, CrosshairInfo.InnerSplitAlpha * 255)),
                mainColor.R, mainColor.G, mainColor.B);
                
            Color outerColor = Color.FromArgb(
                (int)Math.Min(255, Math.Max(0, CrosshairInfo.OuterSplitAlpha * 255)),
                mainColor.R, mainColor.G, mainColor.B);
            
            using (var innerPen = new Pen(innerColor, dimensions.Thickness * CrosshairInfo.SplitSizeRatio))
            using (var outerPen = new Pen(outerColor, dimensions.Thickness * CrosshairInfo.SplitSizeRatio))
            {
                // Apply scaling to split distance
                float splitDistance = CrosshairInfo.SplitDistance * scale;
                
                // Inner crosshair (upper)
                float innerSplitY = centerY - splitDistance;
                g.DrawLine(innerPen, coords.LeftStart, innerSplitY, coords.LeftEnd, innerSplitY);
                g.DrawLine(innerPen, coords.RightStart, innerSplitY, coords.RightEnd, innerSplitY);
                
                if (!CrosshairInfo.IsTStyle && !CrosshairInfo.T)
                    g.DrawLine(innerPen, centerX, coords.TopStart - splitDistance, centerX, coords.TopEnd - splitDistance);
                    
                g.DrawLine(innerPen, centerX, coords.BottomStart - splitDistance, centerX, coords.BottomEnd - splitDistance);
                
                // Outer crosshair (lower)
                float outerSplitY = centerY + splitDistance;
                g.DrawLine(outerPen, coords.LeftStart, outerSplitY, coords.LeftEnd, outerSplitY);
                g.DrawLine(outerPen, coords.RightStart, outerSplitY, coords.RightEnd, outerSplitY);
                
                if (!CrosshairInfo.IsTStyle && !CrosshairInfo.T)
                    g.DrawLine(outerPen, centerX, coords.TopStart + splitDistance, centerX, coords.TopEnd + splitDistance);
                    
                g.DrawLine(outerPen, centerX, coords.BottomStart + splitDistance, centerX, coords.BottomEnd + splitDistance);
            }
        }

        private void DrawDebugInfo(Graphics g, int centerX, int centerY)
        {
            using (var font = new Font("Arial", 8))
            using (var brush = new SolidBrush(Color.White))
            using (var textFormat = new StringFormat { Alignment = StringAlignment.Center })
            {
                string styleDescription = CrosshairInfo.Style switch
                {
                    CrosshairStyle.Default => "Default (0, Dynamic)",
                    CrosshairStyle.ClassicStatic => "Classic Static (1)",
                    CrosshairStyle.ClassicDynamic => "Classic Dynamic (2,3)",
                    CrosshairStyle.Static => "Full Static (4)",
                    CrosshairStyle.Classic => "Semi-Static (5, 1.6 Style)",
                    _ => CrosshairInfo.Style.ToString()
                };
                
                string debugText = $"Style: {styleDescription}\n" +
                                   $"Size/Length: {CrosshairInfo.Size}/{CrosshairInfo.Length}\n" +
                                   $"Thickness: {CrosshairInfo.Thickness}\n" +
                                   $"Gap: {CrosshairInfo.Gap}\n" +
                                   $"Outline: {CrosshairInfo.Outline}\n" +
                                   $"Dot: {CrosshairInfo.HasCenterDot}/{CrosshairInfo.Dot}\n" +
                                   $"T Style: {CrosshairInfo.IsTStyle}/{CrosshairInfo.T}\n" +
                                   $"Split: {CrosshairInfo.SplitDistance}\n" +
                                   $"Color: {CrosshairInfo.ColorR},{CrosshairInfo.ColorG},{CrosshairInfo.ColorB}\n" +
                                   $"Alpha: {CrosshairInfo.Alpha}\n" +
                                   $"Renderer Scale: {ScaleFactor}\n" +
                                   $"Mode: {Mode}";
                
                var textSize = g.MeasureString(debugText, font);
                g.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)),
                    centerX - textSize.Width / 2, centerY + 50, textSize.Width, textSize.Height);
                g.DrawString(debugText, font, brush, centerX, centerY + 50, textFormat);
            }
        }
    }
}