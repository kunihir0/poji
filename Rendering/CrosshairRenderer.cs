using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using poji.Models;
using poji.Utils;
using Color = System.Drawing.Color;

namespace poji.Rendering
{
    /// <summary>
    /// Responsible for rendering crosshairs based on CrosshairInfo configurations.
    /// </summary>
    public class CrosshairRenderer
    {
        private const float DefaultDotSizeMultiplier = 1.5f;

        private readonly Color _defaultDotColor = Color.Red;
        private readonly int _defaultDotSize = 10;

        /// <summary>
        /// Gets or sets the crosshair information to render.
        /// </summary>
        public CrosshairInfo CrosshairInfo { get; set; }

        /// <summary>
        /// Gets or sets the scale factor for rendering.
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        /// <summary>
        /// Draws the crosshair at the specified center coordinates.
        /// </summary>
        /// <param name="g">The graphics object to draw on.</param>
        /// <param name="centerX">The center X coordinate.</param>
        /// <param name="centerY">The center Y coordinate.</param>
        public void Draw(Graphics g, int centerX, int centerY)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (CrosshairInfo != null)
            {
                DrawCsgoCrosshair(g, centerX, centerY);
            }
            else
            {
                DrawFallbackDot(g, centerX, centerY);
            }
        }

        #region Private Methods

        private void DrawFallbackDot(Graphics g, int centerX, int centerY)
        {
            using (var brush = new SolidBrush(_defaultDotColor))
            {
                int size = (int)(_defaultDotSize * ScaleFactor);
                g.FillEllipse(brush, centerX - size / 2, centerY - size / 2, size, size);
            }
        }

        private void DrawCsgoCrosshair(Graphics g, int centerX, int centerY)
        {
            float originalScaleFactor = CrosshairInfo.ScaleFactor;
            CrosshairInfo.ScaleFactor = ScaleFactor;

            try
            {
                // Calculate dimensions
                var dimensions = CalculateCrosshairDimensions();

                // Get colors
                Color mainColor = CrosshairInfo.GetColor();
                Color outlineColor = CrosshairInfo.GetOutlineColor();

                using (var mainPen = new Pen(mainColor, dimensions.Thickness))
                using (var outlinePen = new Pen(outlineColor, dimensions.Thickness + (CrosshairInfo.HasOutline ? dimensions.OutlineThickness * 2 : 0)))
                using (var dotBrush = new SolidBrush(mainColor))
                using (var outlineDotBrush = new SolidBrush(outlineColor))
                {
                    // Draw with calculated coordinates
                    var coords = CalculateCrosshairCoordinates(centerX, centerY, dimensions);

                    if (CrosshairInfo.HasOutline)
                    {
                        DrawOutline(g, outlinePen, outlineDotBrush, coords, dimensions);
                    }

                    DrawMainCrosshair(g, mainPen, dotBrush, coords, dimensions);
                }
            }
            finally
            {
                // Restore original scale factor
                CrosshairInfo.ScaleFactor = originalScaleFactor;
            }
        }

        private (float Length, float Thickness, float Gap, float OutlineThickness) CalculateCrosshairDimensions()
        {
            float length = CrosshairInfo.GetScaledLength();
            float thickness = CrosshairInfo.GetScaledThickness();
            float gap = CrosshairInfo.GetScaledGap();
            float outlineThickness = CrosshairInfo.GetScaledOutline();

            float renderSize = CrosshairUtils.RoundUpToOdd(2 * length);
            float renderThickness = (float)Math.Floor(CrosshairUtils.RoundUpToOdd(2 * thickness) / 2);
            float renderGap = CrosshairUtils.RoundUpToOdd(2 * CrosshairUtils.MapGapValue(gap));

            return (renderSize, renderThickness, renderGap, outlineThickness);
        }

        private (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
                 float TopStart, float TopEnd, float BottomStart, float BottomEnd)
        CalculateCrosshairCoordinates(int centerX, int centerY,
            (float Length, float Thickness, float Gap, float OutlineThickness) dimensions)
        {
            float halfRenderGap = dimensions.Gap / 2;

            // Horizontal lines coordinates
            float leftLineStart = centerX - dimensions.Length / 2 - halfRenderGap;
            float leftLineEnd = centerX - halfRenderGap;
            float rightLineStart = centerX + halfRenderGap;
            float rightLineEnd = centerX + dimensions.Length / 2 + halfRenderGap;

            // Vertical lines coordinates
            float topLineStart = centerY - dimensions.Length / 2 - halfRenderGap;
            float topLineEnd = centerY - halfRenderGap;
            float bottomLineStart = centerY + halfRenderGap;
            float bottomLineEnd = centerY + dimensions.Length / 2 + halfRenderGap;

            return (leftLineStart, leftLineEnd, rightLineStart, rightLineEnd,
                    topLineStart, topLineEnd, bottomLineStart, bottomLineEnd);
        }

        private void DrawOutline(Graphics g, Pen outlinePen, SolidBrush outlineDotBrush,
            (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
             float TopStart, float TopEnd, float BottomStart, float BottomEnd) coords,
            (float Length, float Thickness, float Gap, float OutlineThickness) dimensions)
        {
            int centerX = (int)((coords.LeftStart + coords.RightEnd) / 2);
            int centerY = (int)((coords.TopStart + coords.BottomEnd) / 2);

            // Horizontal outlines
            g.DrawLine(outlinePen, coords.LeftStart, centerY, coords.LeftEnd, centerY);
            g.DrawLine(outlinePen, coords.RightStart, centerY, coords.RightEnd, centerY);

            // Vertical outlines
            if (!CrosshairInfo.IsTStyle)
            {
                g.DrawLine(outlinePen, centerX, coords.TopStart, centerX, coords.TopEnd);
            }
            g.DrawLine(outlinePen, centerX, coords.BottomStart, centerX, coords.BottomEnd);

            // Center dot outline
            if (CrosshairInfo.HasCenterDot)
            {
                float dotSize = dimensions.Thickness * DefaultDotSizeMultiplier + (dimensions.OutlineThickness * 2);
                g.FillEllipse(outlineDotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
            }
        }

        private void DrawMainCrosshair(Graphics g, Pen mainPen, SolidBrush dotBrush,
            (float LeftStart, float LeftEnd, float RightStart, float RightEnd,
             float TopStart, float TopEnd, float BottomStart, float BottomEnd) coords,
            (float Length, float Thickness, float Gap, float OutlineThickness) dimensions)
        {
            int centerX = (int)((coords.LeftStart + coords.RightEnd) / 2);
            int centerY = (int)((coords.TopStart + coords.BottomEnd) / 2);

            // Horizontal lines
            g.DrawLine(mainPen, coords.LeftStart, centerY, coords.LeftEnd, centerY);
            g.DrawLine(mainPen, coords.RightStart, centerY, coords.RightEnd, centerY);

            // Vertical lines
            if (!CrosshairInfo.IsTStyle)
            {
                g.DrawLine(mainPen, centerX, coords.TopStart, centerX, coords.TopEnd);
            }
            g.DrawLine(mainPen, centerX, coords.BottomStart, centerX, coords.BottomEnd);

            // Draw center dot
            if (CrosshairInfo.HasCenterDot)
            {
                float dotSize = dimensions.Thickness * DefaultDotSizeMultiplier;
                g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
            }
        }

        #endregion
    }
}