using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace poji
{
    public sealed class GlobalKeyboardHook : IDisposable
    {
        // Win32 API constants and imports
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmKeyup = 0x0101;
        private const int WmSyskeydown = 0x0104;
        private const int WmSyskeyup = 0x0105;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _isStarted;

        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            if (!_isStarted)
            {
                _hookId = SetHook(_proc);
                _isStarted = true;
            }
        }

        public void Stop()
        {
            if (_isStarted && _hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _isStarted = false;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Check if it's a key we're interested in
                if ((wParam == (IntPtr)WmKeydown || wParam == (IntPtr)WmSyskeydown) && KeyDown != null)
                {
                    KeyEventArgs args = new KeyEventArgs(key);
                    KeyDown(this, args);

                    if (args.Handled)
                        return (IntPtr)1;
                }
                else if ((wParam == (IntPtr)WmKeyup || wParam == (IntPtr)WmSyskeyup) && KeyUp != null)
                {
                    KeyEventArgs args = new KeyEventArgs(key);
                    KeyUp(this, args);

                    if (args.Handled)
                        return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #region IDisposable Support
        private bool _disposedValue;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                Stop();

                _disposedValue = true;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}