using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChooChooApp
{
    /// <summary>
    /// Installs a global low‐level keyboard hook so that key events can be intercepted
    /// even when an exclusive fullscreen game is active.
    /// </summary>
    public class GlobalHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private IntPtr hookId = IntPtr.Zero;
        private HookProc proc;

        public event EventHandler<GlobalHookEventArgs> KeyPressed;

        public GlobalHook()
        {
            proc = HookCallback;
            hookId = SetHook(proc);
        }

        private IntPtr SetHook(HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                ProcessModule curModule = null;
                try { curModule = curProcess.MainModule; } catch { }
                IntPtr hMod = (curModule != null && !string.IsNullOrEmpty(curModule.ModuleName)) 
                              ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, hMod, 0);
            }
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                const int WM_KEYDOWN = 0x0100;
                const int WM_SYSKEYDOWN = 0x0104;
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    Keys key = (Keys)hookStruct.vkCode;
                    OnKeyPressed(new GlobalHookEventArgs(key));
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        protected virtual void OnKeyPressed(GlobalHookEventArgs e)
        {
            KeyPressed?.Invoke(this, e);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(hookId);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class GlobalHookEventArgs : EventArgs
    {
        public Keys Key { get; private set; }
        public GlobalHookEventArgs(Keys key)
        {
            Key = key;
        }
    }
}
