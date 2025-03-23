using System;
using System.Threading;
using EasyHook;
using SharpDX;
using SharpDX.DXGI;
using GameOverlayInjection.Core;
using GameOverlayInjection.Hooks;
using GameOverlayInjection.Interfaces;

namespace GameOverlayInjection.Overlay
{
    /// <summary>
    /// Main overlay class that gets injected into the target process
    /// </summary>
    public class InjectedOverlay : IEntryPoint, IDisposable
    {
        // Static instance for callback access
        public static InjectedOverlay Instance { get; private set; }

        private string _channelName;
        private IOverlayInterface _interface;
        private CrosshairSettings _settings;
        private CrosshairRenderer _renderer;
        private IDXGISwapChainHook _swapChainHook;
        private bool _visible = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Constructor called during injection
        /// </summary>
        public InjectedOverlay(RemoteHooking.IContext context, string channelName)
        {
            Instance = this;
            _channelName = channelName;
            _settings = CrosshairSettings.CreateDefault();
            _renderer = new CrosshairRenderer(_settings);
        }

        /// <summary>
        /// Entry point after injection
        /// </summary>
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            try
            {
                // Connect back to IPC server
                _interface = RemoteHooking.IpcConnectClient<OverlayInterface>(_channelName);

                // Let the host process know we're connected
                _interface.SetVisible(true);

                // Install hooks
                InstallHooks();

                // Keep the thread alive
                while (!_isDisposed)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in injected code: {ex.Message}");
            }
            finally
            {
                // Clean up
                Dispose();
            }
        }

        private void InstallHooks()
        {
            // Create and configure hook for Present method
            _swapChainHook = new IDXGISwapChainHook();
            _swapChainHook.OnPresent = OnPresentCalled;
        }

        private void OnPresentCalled(IntPtr swapChainPtr)
        {
            if (!_visible)
                return;

            try
            {
                // Create a temporary SwapChain instance from the pointer
                using (var swapChain = new SwapChain(swapChainPtr))
                {
                    // Draw the crosshair
                    _renderer.Draw(swapChain);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Present callback: {ex.Message}");
            }
        }

        /// <summary>
        /// Show or hide the crosshair overlay
        /// </summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
        }

        /// <summary>
        /// Updates the crosshair appearance settings
        /// </summary>
        public void UpdateCrosshairSettings(int size, int gap, int thickness, bool dot, Color4 color)
        {
            _settings.Size = size;
            _settings.Gap = gap;
            _settings.Thickness = thickness;
            _settings.Dot = dot;
            _settings.Color = color;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _swapChainHook?.Dispose();
            _renderer?.Dispose();
            Instance = null;
        }
    }
}