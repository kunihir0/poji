using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using Device = SharpDX.Direct3D11.Device;
using D2D1Factory1 = SharpDX.Direct2D1.Factory1;

namespace GameOverlayInjection.Overlay
{
    /// <summary>
    /// Handles rendering of the crosshair using Direct2D
    /// </summary>
    public class CrosshairRenderer : IDisposable
    {
        private readonly CrosshairSettings _settings;
        private D2D1Factory1 _d2dFactory;

        public CrosshairRenderer(CrosshairSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _d2dFactory = new D2D1Factory1();
        }

        /// <summary>
        /// Draws the crosshair on the provided swap chain's back buffer
        /// </summary>
        public void Draw(SwapChain swapChain)
        {
            if (swapChain == null)
                throw new ArgumentNullException(nameof(swapChain));

            try
            {
                using (var device = swapChain.GetDevice<Device>())
                using (var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0))
                {
                    // Get screen dimensions
                    var backBufferDesc = backBuffer.Description;
                    int screenWidth = backBufferDesc.Width;
                    int screenHeight = backBufferDesc.Height;

                    // Draw using Direct2D
                    DrawWithDirect2D(backBuffer, screenWidth, screenHeight);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error drawing crosshair: {ex.Message}");
            }
        }

        private void DrawWithDirect2D(Texture2D backBuffer, int screenWidth, int screenHeight)
        {
            // Create DXGI surface from the backbuffer
            using (var dxgiSurface = backBuffer.QueryInterface<Surface>())
            {
                // Create D2D render target
                var renderTargetProperties = new RenderTargetProperties(
                    new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));

                using (var d2dRenderTarget = new RenderTarget(
                    _d2dFactory,
                    dxgiSurface,
                    renderTargetProperties))
                {
                    // Start drawing
                    d2dRenderTarget.BeginDraw();

                    DrawCrosshair(d2dRenderTarget, screenWidth, screenHeight);

                    // End drawing
                    d2dRenderTarget.EndDraw();
                }
            }
        }

        private void DrawCrosshair(RenderTarget renderTarget, int screenWidth, int screenHeight)
        {
            // Create a brush with the crosshair color
            var crosshairColor = ConvertToRawColor4(_settings.Color);

            using (var brush = new SolidColorBrush(renderTarget, crosshairColor))
            {
                float centerX = screenWidth / 2.0f;
                float centerY = screenHeight / 2.0f;

                DrawHorizontalLines(renderTarget, brush, centerX, centerY);
                DrawVerticalLines(renderTarget, brush, centerX, centerY);

                // Draw center dot if enabled
                if (_settings.Dot)
                {
                    DrawCenterDot(renderTarget, brush, centerX, centerY);
                }
            }
        }

        private void DrawHorizontalLines(RenderTarget renderTarget, SolidColorBrush brush, float centerX, float centerY)
        {
            float halfThickness = _settings.Thickness / 2.0f;

            // Left segment
            renderTarget.FillRectangle(
                new SharpDX.Mathematics.Interop.RawRectangleF(
                    centerX - _settings.Gap - _settings.Size,
                    centerY - halfThickness,
                    centerX - _settings.Gap,
                    centerY + halfThickness),
                brush);

            // Right segment
            renderTarget.FillRectangle(
                new SharpDX.Mathematics.Interop.RawRectangleF(
                    centerX + _settings.Gap,
                    centerY - halfThickness,
                    centerX + _settings.Gap + _settings.Size,
                    centerY + halfThickness),
                brush);
        }

        private void DrawVerticalLines(RenderTarget renderTarget, SolidColorBrush brush, float centerX, float centerY)
        {
            float halfThickness = _settings.Thickness / 2.0f;

            // Top segment
            renderTarget.FillRectangle(
                new SharpDX.Mathematics.Interop.RawRectangleF(
                    centerX - halfThickness,
                    centerY - _settings.Gap - _settings.Size,
                    centerX + halfThickness,
                    centerY - _settings.Gap),
                brush);

            // Bottom segment
            renderTarget.FillRectangle(
                new SharpDX.Mathematics.Interop.RawRectangleF(
                    centerX - halfThickness,
                    centerY + _settings.Gap,
                    centerX + halfThickness,
                    centerY + _settings.Gap + _settings.Size),
                brush);
        }

        private void DrawCenterDot(RenderTarget renderTarget, SolidColorBrush brush, float centerX, float centerY)
        {
            float halfThickness = _settings.Thickness / 2.0f;

            renderTarget.FillRectangle(
                new SharpDX.Mathematics.Interop.RawRectangleF(
                    centerX - halfThickness,
                    centerY - halfThickness,
                    centerX + halfThickness,
                    centerY + halfThickness),
                brush);
        }

        private SharpDX.Mathematics.Interop.RawColor4 ConvertToRawColor4(Color4 color)
        {
            return new SharpDX.Mathematics.Interop.RawColor4(
                color.Red,
                color.Green,
                color.Blue,
                color.Alpha);
        }

        public void Dispose()
        {
            _d2dFactory?.Dispose();
            _d2dFactory = null;
        }
    }
}