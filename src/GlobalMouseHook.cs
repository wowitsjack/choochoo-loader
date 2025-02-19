using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace ChooChooApp
{
    /// <summary>
    /// Installs a global low‐level mouse hook so that mouse events can be intercepted
    /// even when an exclusive fullscreen game is active.
    /// </summary>
    public class GlobalMouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private IntPtr hookId = IntPtr.Zero;
        private HookProc proc;

        public event EventHandler<GlobalMouseHookEventArgs> MouseAction;

        public GlobalMouseHook()
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
                return SetWindowsHookEx(WH_MOUSE_LL, proc, hMod, 0);
            }
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // Use non-generic Marshal.PtrToStructure overload
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                MouseMessages message = (MouseMessages)wParam.ToInt32();
                OnMouseAction(new GlobalMouseHookEventArgs(message, new Point(hookStruct.pt.x, hookStruct.pt.y)));
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        protected virtual void OnMouseAction(GlobalMouseHookEventArgs e)
        {
            MouseAction?.Invoke(this, e);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(hookId);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public enum MouseMessages
        {
            WM_MOUSEMOVE = 0x200,
            WM_LBUTTONDOWN = 0x201,
            WM_LBUTTONUP = 0x202,
            WM_RBUTTONDOWN = 0x204,
            WM_RBUTTONUP = 0x205,
            WM_MOUSEWHEEL = 0x20A
        }

        [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class GlobalMouseHookEventArgs : EventArgs
    {
        public GlobalMouseHook.MouseMessages Message { get; private set; }
        public Point Location { get; private set; }
        public GlobalMouseHookEventArgs(GlobalMouseHook.MouseMessages message, Point location)
        {
            Message = message;
            Location = location;
        }
    }
}
