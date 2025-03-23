using System;
using System.IO;
using EasyHook;
using SharpDX;

namespace GameOverlayInjection.Core
{
    /// <summary>
    /// Handles the injection of the overlay into target processes
    /// </summary>
    public class Injector
    {
        private static string _channelName = Guid.NewGuid().ToString();
        private IOverlayInterface _interface;

        public Injector()
        {
            _channelName = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Injects the overlay library into the specified process
        /// </summary>
        /// <param name="processId">Target process ID</param>
        /// <returns>True if injection succeeded, false otherwise</returns>
        public bool Inject(int processId)
        {
            string injectionLibraryPath = GetInjectionLibraryPath();

            // Create IPC server with the concrete interface implementation
            RemoteHooking.IpcCreateServer<Interfaces.OverlayInterface>(
                ref _channelName,
                System.Runtime.Remoting.WellKnownObjectMode.Singleton);

            try
            {
                // Inject library into target process
                RemoteHooking.Inject(
                    processId,
                    injectionLibraryPath,
                    injectionLibraryPath, // library to inject
                    _channelName // parameter for the injected library
                );

                Console.WriteLine($"Successfully injected into process {processId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Injection failed: {ex.Message}");
                return false;
            }
        }

        private string GetInjectionLibraryPath()
        {
            return Path.Combine(
                Path.GetDirectoryName(typeof(Injector).Assembly.Location),
                "GameOverlayInjection.dll");
        }

        /// <summary>
        /// Updates the crosshair appearance
        /// </summary>
        public void SetCrosshairSettings(int size, int gap, int thickness, bool dot, Color4 color)
        {
            _interface?.SetCrosshairSettings(size, gap, thickness, dot, color);
        }

        /// <summary>
        /// Shows or hides the crosshair overlay
        /// </summary>
        public void SetVisible(bool visible)
        {
            _interface?.SetVisible(visible);
        }
    }
}