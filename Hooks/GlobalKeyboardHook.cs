using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace poji.Hooks
{
    /// <summary>
    /// Provides a global keyboard hook that captures keyboard events system-wide.
    /// </summary>
    public sealed class GlobalKeyboardHook : IDisposable
    {
        #region Win32 API Constants and Imports
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookHandle = IntPtr.Zero;
        private bool _isHookActive;
        private bool _isDisposed;

        /// <summary>
        /// Occurs when a key is pressed.
        /// </summary>
        public event KeyEventHandler KeyDown;

        /// <summary>
        /// Occurs when a key is released.
        /// </summary>
        public event KeyEventHandler KeyUp;

        /// <summary>
        /// Initializes a new instance of the GlobalKeyboardHook class.
        /// </summary>
        public GlobalKeyboardHook()
        {
            _hookCallback = ProcessKeyEvent;
        }

        /// <summary>
        /// Starts the keyboard hook.
        /// </summary>
        public void Start()
        {
            if (!_isHookActive)
            {
                _hookHandle = InstallHook(_hookCallback);
                _isHookActive = true;
            }
        }

        /// <summary>
        /// Stops the keyboard hook.
        /// </summary>
        public void Stop()
        {
            if (_isHookActive && _hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
                _isHookActive = false;
            }
        }

        private IntPtr InstallHook(LowLevelKeyboardProc callback)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                return SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    callback,
                    GetModuleHandle(currentModule.ModuleName),
                    0);
            }
        }

        private IntPtr ProcessKeyEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if ((wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN) && KeyDown != null)
                {
                    KeyEventArgs args = new KeyEventArgs(key);
                    KeyDown(this, args);

                    if (args.Handled)
                        return (IntPtr)1;
                }
                else if ((wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP) && KeyUp != null)
                {
                    KeyEventArgs args = new KeyEventArgs(key);
                    KeyUp(this, args);

                    if (args.Handled)
                        return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        #region IDisposable Implementation
        /// <summary>
        /// Releases all resources used by the GlobalKeyboardHook.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Free unmanaged resources
                Stop();
                _isDisposed = true;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }
        #endregion
    }
}