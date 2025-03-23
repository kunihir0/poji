using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using DWrite = SharpDX.DirectWrite;
using Color = System.Drawing.Color;
using poji.Models;
using D2D = SharpDX.Direct2D1;

namespace poji
{
    /// <summary>
    /// Handles DirectX rendering for the crosshair overlay
    /// </summary>
    public class DirectXRenderer : IDisposable
    {
        #region Win32 API Imports

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        /// <summary>
        /// Gets or sets the scale factor for the crosshair
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        #endregion

        #region Win32 Constants

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOPMOST = 0x00000008;

        #endregion

        #region Direct3D Resources

        private D3D11.Device _d3d11Device;
        private D3D11.DeviceContext _d3d11Context;
        private SwapChain _swapChain;
        private RenderTargetView _renderTargetView;

        #endregion

        #region Direct2D Resources

        private D2D.Factory1 _d2dFactory;
        private DWrite.Factory _dwriteFactory;
        private D2D.RenderTarget _d2dRenderTarget;
        private DWrite.TextFormat _textFormat;
        private D2D.SolidColorBrush _textBrush;
        private D2D.SolidColorBrush _crosshairBrush;
        private D2D.SolidColorBrush _outlineBrush;

        #endregion

        #region Shared Resources

        private SharpDX.DXGI.Resource _sharedResource;
        private DXGI.KeyedMutex _d3d11Mutex;
        private DXGI.KeyedMutex _d2dMutex;

        #endregion

        #region Form and State

        private Form _overlayForm;
        private bool _isInitialized;
        private bool _clickThrough = true;
        private CrosshairInfo _crosshairInfo;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the renderer has been successfully initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// The current crosshair configuration
        /// </summary>
        public CrosshairInfo CrosshairInfo
        {
            get => _crosshairInfo;
            set => _crosshairInfo = value;
        }

        /// <summary>
        /// Gets or sets whether input should pass through the overlay
        /// </summary>
        public bool ClickThrough
        {
            get => _clickThrough;
            set
            {
                _clickThrough = value;
                if (_isInitialized && _overlayForm != null)
                {
                    UpdateWindowStyles();
                }
            }
        }

        #endregion

        /// <summary>
        /// Initializes the DirectX renderer for the specified form
        /// </summary>
        /// <param name="overlayForm">The form to use as an overlay</param>
        /// <returns>True if initialization was successful</returns>
        public bool Initialize(Form overlayForm)
        {
            try
            {
                _overlayForm = overlayForm;

                ConfigureOverlayForm();
                InitializeDirect3D11();
                InitializeDirect2D();
                CreateSharedResources();

                _overlayForm.Resize += (sender, args) => Resize();
                _overlayForm.Activated += (sender, args) => SetTopMost();

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                LogAndDisplayError(ex);
                Dispose();
                return false;
            }
        }

        /// <summary>
        /// Configure the form for use as a transparent overlay
        /// </summary>
        private void ConfigureOverlayForm()
        {
            _overlayForm.FormBorderStyle = FormBorderStyle.None;
            _overlayForm.ShowInTaskbar = false;
            _overlayForm.TopMost = true;
            _overlayForm.BackColor = Color.Black;
            _overlayForm.TransparencyKey = Color.Black;

            UpdateWindowStyles();
        }

        /// <summary>
        /// Set window styles for transparency and click-through behavior
        /// </summary>
        private void UpdateWindowStyles()
        {
            IntPtr hwnd = _overlayForm.Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            if (_clickThrough)
            {
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT;
                exStyle |= WS_EX_LAYERED;
            }

            exStyle |= WS_EX_TOPMOST;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        /// <summary>
        /// Ensure the window remains topmost
        /// </summary>
        private void SetTopMost()
        {
            SetWindowPos(
                _overlayForm.Handle,
                HWND_TOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW
            );
        }

        /// <summary>
        /// Initialize Direct3D 11 resources
        /// </summary>
        private void InitializeDirect3D11()
        {
            var swapChainDescription = CreateSwapChainDescription();

            D3D11.Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 },
                swapChainDescription,
                out _d3d11Device,
                out _swapChain);

            _d3d11Context = _d3d11Device.ImmediateContext;
            CreateRenderTargetView();
            DisableAltEnterFullscreen();
        }

        /// <summary>
        /// Create swap chain description for the overlay
        /// </summary>
        private SwapChainDescription CreateSwapChainDescription()
        {
            return new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(
                    _overlayForm.ClientSize.Width,
                    _overlayForm.ClientSize.Height,
                    new Rational(60, 1),
                    Format.B8G8R8A8_UNorm),
                IsWindowed = true,
                OutputHandle = _overlayForm.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Sequential,
                Usage = Usage.RenderTargetOutput | Usage.Shared
            };
        }

        /// <summary>
        /// Create render target view from back buffer
        /// </summary>
        private void CreateRenderTargetView()
        {
            using (var backBuffer = D3D11.Resource.FromSwapChain<D3D11.Texture2D>(_swapChain, 0))
            {
                _renderTargetView = new RenderTargetView(_d3d11Device, backBuffer);
            }
        }

        /// <summary>
        /// Disable Alt+Enter fullscreen shortcut
        /// </summary>
        private void DisableAltEnterFullscreen()
        {
            using (var factory = _swapChain.GetParent<SharpDX.DXGI.Factory>())
            {
                factory.MakeWindowAssociation(_overlayForm.Handle, WindowAssociationFlags.IgnoreAltEnter);
            }
        }

        /// <summary>
        /// Initialize Direct2D and DirectWrite resources
        /// </summary>
        private void InitializeDirect2D()
        {
            _d2dFactory = new D2D.Factory1();
            _dwriteFactory = new DWrite.Factory();
            _textFormat = new DWrite.TextFormat(_dwriteFactory, "Consolas", 14.0f)
            {
                TextAlignment = DWrite.TextAlignment.Center,
                ParagraphAlignment = DWrite.ParagraphAlignment.Center
            };
        }

        /// <summary>
        /// Create shared resources between Direct3D and Direct2D
        /// </summary>
        private void CreateSharedResources()
        {
            try
            {
                using (var factory = new SharpDX.DXGI.Factory1())
                using (var adapter = factory.GetAdapter(0))
                using (var backBuffer = D3D11.Resource.FromSwapChain<D3D11.Texture2D>(_swapChain, 0))
                {
                    LogAdapterInfo(adapter);
                    SetupSharedResourceAndMutex(backBuffer);
                    SetupDirect2DRenderTarget(backBuffer);
                }
            }
            catch (Exception ex)
            {
                string errorDetails = $"CreateSharedResources failed: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetails += $" Inner exception: {ex.InnerException.Message}";
                }
                Console.WriteLine(ex);
                throw new Exception(errorDetails, ex);
            }
        }

        /// <summary>
        /// Log adapter information for debugging
        /// </summary>
        private void LogAdapterInfo(DXGI.Adapter adapter)
        {
            Console.WriteLine($"Adapter: {adapter.Description.Description}");
        }

        /// <summary>
        /// Setup shared resource and keyed mutex
        /// </summary>
        private void SetupSharedResourceAndMutex(D3D11.Texture2D backBuffer)
        {
            _sharedResource = backBuffer.QueryInterface<DXGI.Resource>();
            Console.WriteLine("Got shared resource");

            try
            {
                _d3d11Mutex = backBuffer.QueryInterface<DXGI.KeyedMutex>();
                Console.WriteLine("Got D3D11 mutex");
            }
            catch (SharpDXException ex)
            {
                _d3d11Mutex = null;
                Console.WriteLine("Not Supported D3D11 mutex\n", ex);
            }
        }

        /// <summary>
        /// Setup Direct2D render target and brushes
        /// </summary>
        private void SetupDirect2DRenderTarget(D3D11.Texture2D backBuffer)
        {
            using (var surface = backBuffer.QueryInterface<DXGI.Surface>())
            {
                var renderTargetProperties = new D2D.RenderTargetProperties(
                    new D2D.PixelFormat(Format.Unknown, D2D.AlphaMode.Premultiplied));

                _d2dRenderTarget = new D2D.RenderTarget(_d2dFactory, surface, renderTargetProperties);

                // Create brushes
                _textBrush = new D2D.SolidColorBrush(_d2dRenderTarget, new RawColor4(1, 1, 1, 1));
                _crosshairBrush = new D2D.SolidColorBrush(_d2dRenderTarget, new RawColor4(0, 1, 0, 1));
                _outlineBrush = new D2D.SolidColorBrush(_d2dRenderTarget, new RawColor4(0, 0, 0, 1));

                Console.WriteLine("Got DXGI surface");
            }
        }

        /// <summary>
        /// Resize the renderer when the form size changes
        /// </summary>
        public void Resize()
        {
            if (!_isInitialized) return;

            DisposeDirect2DResources();
            _renderTargetView?.Dispose();

            ResizeSwapChain();
            CreateRenderTargetView();
            CreateSharedResources();
            SetTopMost();
        }

        /// <summary>
        /// Resize the swap chain to match the form size
        /// </summary>
        private void ResizeSwapChain()
        {
            _swapChain.ResizeBuffers(
                1,
                _overlayForm.ClientSize.Width,
                _overlayForm.ClientSize.Height,
                Format.B8G8R8A8_UNorm,
                SwapChainFlags.None);
        }

        /// <summary>
        /// Render the crosshair overlay
        /// </summary>
        public void Render()
        {
            if (!_isInitialized || _crosshairInfo == null) return;

            try
            {
                SetTopMost();
                ClearRenderTarget();

                float centerX = _overlayForm.ClientSize.Width / 2.0f;
                float centerY = _overlayForm.ClientSize.Height / 2.0f;

                _d2dRenderTarget.BeginDraw();
                DrawCrosshair(centerX, centerY);
                DrawDebugTextIfEnabled(centerX, centerY);
                _d2dRenderTarget.EndDraw();

                PresentFrame();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Render error: {ex.Message}");
                // Continue operation - don't crash on a single frame error
            }
        }

        /// <summary>
        /// Clear the render target
        /// </summary>
        private void ClearRenderTarget()
        {
            _d3d11Context.ClearRenderTargetView(_renderTargetView, new RawColor4(0, 0, 0, 0));
        }

        /// <summary>
        /// Draw debug text if enabled in the crosshair settings
        /// </summary>
        private void DrawDebugTextIfEnabled(float centerX, float centerY)
        {
            if (_crosshairInfo.ShowDebugText)
            {
                DrawDebugText(centerX, centerY + 50);
            }
        }

        /// <summary>
        /// Present the frame to the screen
        /// </summary>
        private void PresentFrame()
        {
            _swapChain.Present(1, PresentFlags.None);
        }

        /// <summary>
        /// Draw the crosshair based on the current settings
        /// </summary>
        private void DrawCrosshair(float centerX, float centerY)
        {
            if (_crosshairInfo == null) return;

            UpdateCrosshairBrushColor();

            if (_crosshairInfo.Dot)
            {
                DrawCenterDot(centerX, centerY);
            }

            if (!_crosshairInfo.DotOnly)
            {
                DrawCrosshairLines(centerX, centerY);
            }
        }

        /// <summary>
        /// Update the crosshair brush color from settings
        /// </summary>
        private void UpdateCrosshairBrushColor()
        {
            _crosshairBrush.Color = new RawColor4(
                _crosshairInfo.ColorR / 255.0f,
                _crosshairInfo.ColorG / 255.0f,
                _crosshairInfo.ColorB / 255.0f,
                _crosshairInfo.Alpha / 255.0f
            );
        }

        /// <summary>
        /// Draw the center dot of the crosshair
        /// </summary>
        private void DrawCenterDot(float centerX, float centerY)
        {
            float thickness = _crosshairInfo.GetScaledThickness();
            float outline = _crosshairInfo.GetScaledOutline();
            float dotSize = thickness;

            if (_crosshairInfo.Outline > 0)
            {
                _d2dRenderTarget.FillEllipse(
                    new Ellipse(new RawVector2(centerX, centerY), dotSize + outline, dotSize + outline),
                    _outlineBrush
                );
            }

            _d2dRenderTarget.FillEllipse(
                new Ellipse(new RawVector2(centerX, centerY), dotSize, dotSize),
                _crosshairBrush
            );
        }

        /// <summary>
        /// Draw the crosshair lines
        /// </summary>
        private void DrawCrosshairLines(float centerX, float centerY)
        {
            float thickness = _crosshairInfo.GetScaledThickness();
            float size = _crosshairInfo.GetScaledLength();
            float gap = _crosshairInfo.GetScaledGap();
            float outline = _crosshairInfo.GetScaledOutline();
            float halfThickness = thickness / 2;
            float startGap = gap / 2;

            if (_crosshairInfo.T)
            {
                // Horizontal lines
                DrawCrosshairLine(
                    centerX - startGap - size, centerY - halfThickness,
                    centerX - startGap, centerY - halfThickness,
                    thickness, outline
                );

                DrawCrosshairLine(
                    centerX + startGap, centerY - halfThickness,
                    centerX + startGap + size, centerY - halfThickness,
                    thickness, outline
                );

                // Vertical lines
                DrawCrosshairLine(
                    centerX - halfThickness, centerY - startGap - size,
                    centerX - halfThickness, centerY - startGap,
                    thickness, outline
                );

                DrawCrosshairLine(
                    centerX - halfThickness, centerY + startGap,
                    centerX - halfThickness, centerY + startGap + size,
                    thickness, outline
                );
            }
        }

        /// <summary>
        /// Draw a single line of the crosshair
        /// </summary>
        private void DrawCrosshairLine(float x1, float y1, float x2, float y2, float thickness, float outline)
        {
            RawVector2 start = new RawVector2(x1, y1);
            RawVector2 end = new RawVector2(x2, y2);

            if (outline > 0)
            {
                _d2dRenderTarget.DrawLine(start, end, _outlineBrush, thickness + (outline * 2));
            }

            _d2dRenderTarget.DrawLine(start, end, _crosshairBrush, thickness);
        }

        /// <summary>
        /// Draw debug text showing crosshair settings
        /// </summary>
        private void DrawDebugText(float x, float y)
        {
            RawRectangleF textRect = new RawRectangleF(x - 150, y - 10, x + 150, y + 40);

            string debugText = string.Format(
                "Size: {0} | Gap: {1} | Thickness: {2}\nScale: {3:F2}",
                _crosshairInfo.Size,
                _crosshairInfo.Gap,
                _crosshairInfo.Thickness,
                _crosshairInfo.ScaleFactor
            );

            _d2dRenderTarget.DrawText(
                debugText,
                _textFormat,
                textRect,
                _textBrush
            );
        }

        /// <summary>
        /// Log and display error message
        /// </summary>
        private void LogAndDisplayError(Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                $"Failed to initialize DirectX rendering: {ex.Message}\n\nThe application will now exit.",
                "DirectX Error",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }

        /// <summary>
        /// Dispose Direct2D resources
        /// </summary>
        private void DisposeDirect2DResources()
        {
            SafeDispose(ref _textBrush);
            SafeDispose(ref _crosshairBrush);
            SafeDispose(ref _outlineBrush);
            SafeDispose(ref _d2dRenderTarget);
        }

        /// <summary>
        /// Safely dispose a resource
        /// </summary>
        private void SafeDispose<T>(ref T resource) where T : class, IDisposable
        {
            if (resource != null)
            {
                resource.Dispose();
                resource = null;
            }
        }

        /// <summary>
        /// Dispose all resources used by the renderer
        /// </summary>
        public void Dispose()
        {
            DisposeDirect2DResources();

            SafeDispose(ref _textFormat);
            SafeDispose(ref _dwriteFactory);
            SafeDispose(ref _d2dFactory);
            SafeDispose(ref _d3d11Mutex);
            SafeDispose(ref _sharedResource);
            SafeDispose(ref _renderTargetView);
            SafeDispose(ref _swapChain);
            SafeDispose(ref _d3d11Context);
            SafeDispose(ref _d3d11Device);

            _isInitialized = false;
        }
    }
}