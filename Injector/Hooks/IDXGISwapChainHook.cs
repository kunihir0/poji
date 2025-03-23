using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using EasyHook;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Factory1 = SharpDX.DXGI.Factory1;

namespace GameOverlayInjection.Hooks
{
    /// <summary>
    /// Hooks the IDXGISwapChain::Present method to render overlay content
    /// </summary>
    public class IDXGISwapChainHook : IDisposable
    {
        // Delegate for the Present function
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int PresentDelegate(IntPtr swapChainPtr, int syncInterval, int flags);

        private readonly LocalHook _presentHook;
        private readonly PresentDelegate _originalPresent;

        // Action to execute when Present is called
        public Action<IntPtr> OnPresent { get; set; }

        /// <summary>
        /// Creates and installs a hook for the IDXGISwapChain::Present method
        /// </summary>
        public IDXGISwapChainHook()
        {
            IntPtr presentAddress = GetPresentFunctionAddress();

            _originalPresent = Marshal.GetDelegateForFunctionPointer<PresentDelegate>(presentAddress);
            _presentHook = LocalHook.Create(
                presentAddress,
                new PresentDelegate(PresentHook),
                this);

            // Activate the hook for all threads
            _presentHook.ThreadACL.SetExclusiveACL(new int[0]);
        }

        private IntPtr GetPresentFunctionAddress()
        {
            // Create a dummy device and swapchain to get the vtable
            var factory = new Factory1();
            var adapter = factory.GetAdapter(0);

            var swapChainDesc = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(100, 100, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = Process.GetCurrentProcess().MainWindowHandle,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput
            };

            Device.CreateWithSwapChain(adapter, DeviceCreationFlags.None, swapChainDesc, out Device device, out SwapChain swapChain);

            try
            {
                // Get the native pointer
                IntPtr swapChainPtr = swapChain.NativePointer;

                // Read the vtable pointer (first entry in the object)
                IntPtr vtablePtr = Marshal.ReadIntPtr(swapChainPtr);

                // Present is typically the 8th function in the vtable (index 8)
                return Marshal.ReadIntPtr(vtablePtr, 8 * IntPtr.Size);
            }
            finally
            {
                // Clean up
                swapChain.Dispose();
                device.Dispose();
                adapter.Dispose();
                factory.Dispose();
            }
        }

        private int PresentHook(IntPtr swapChainPtr, int syncInterval, int flags)
        {
            try
            {
                // Call the registered callback
                OnPresent?.Invoke(swapChainPtr);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PresentHook: {ex.Message}");
            }

            // Call the original Present function
            return _originalPresent(swapChainPtr, syncInterval, flags);
        }

        public void Dispose()
        {
            _presentHook?.Dispose();
        }
    }
}