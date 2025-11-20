using KeyboardHook.Enums;
using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeyboardHook.Implementation.MouseImplementation
{
    internal class WindowsMouseHook : IMouseHook, IDisposable
    {
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelMouseProc _proc;
        private readonly IntPtr _hookId;
        private readonly HashSet<MouseButton> _pressedButtons = new HashSet<MouseButton>();

        private const int WH_MOUSE_LL = 14;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int WM_MOUSEWHEEL = 0x020A;

        public event Action<MouseButton> ButtonDown;
        public event Action<MouseButton> ButtonUp;

        public WindowsMouseHook()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                MouseButton? btn = null;

                if (msg == WM_MOUSEWHEEL)
                {
                    MSLLHOOKSTRUCT info = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    int delta = (short)((info.mouseData >> 16) & 0xffff);
                    if (delta > 0)
                    {
                        btn = MouseButton.WheelUp;
                    }
                    else
                    {
                        btn = MouseButton.WheelDown;
                    }

                    if (btn.HasValue)
                    {
                        Action<MouseButton> handlerWheel = ButtonDown;
                        if (handlerWheel != null)
                        {
                            handlerWheel(btn.Value);
                        }
                    }
                }
                else
                {
                    switch (msg)
                    {
                        case WM_LBUTTONDOWN:
                            btn = MouseButton.Left;
                            break;
                        case WM_LBUTTONUP:
                            btn = MouseButton.Left;
                            break;
                        case WM_RBUTTONDOWN:
                            btn = MouseButton.Right;
                            break;
                        case WM_RBUTTONUP:
                            btn = MouseButton.Right;
                            break;
                        case WM_MBUTTONDOWN:
                            btn = MouseButton.Middle;
                            break;
                        case WM_MBUTTONUP:
                            btn = MouseButton.Middle;
                            break;
                        case WM_XBUTTONDOWN:
                        case WM_XBUTTONUP:
                            {
                                MSLLHOOKSTRUCT info = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                                int xbtn = (info.mouseData >> 16) & 0xffff;
                                if (xbtn == 1)
                                {
                                    btn = MouseButton.X1;
                                }
                                else if (xbtn == 2)
                                {
                                    btn = MouseButton.X2;
                                }
                            }
                            break;
                    }

                    if (btn.HasValue)
                    {
                        bool isDown = (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN ||
                                       msg == WM_MBUTTONDOWN || msg == WM_XBUTTONDOWN);

                        if (isDown)
                        {
                            _pressedButtons.Add(btn.Value);
                            Action<MouseButton> handler = ButtonDown;
                            if (handler != null)
                                handler(btn.Value);
                        }
                        else
                        {
                            _pressedButtons.Remove(btn.Value);
                            Action<MouseButton> handler = ButtonUp;
                            if (handler != null)
                                handler(btn.Value);
                        }
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public MouseButton[] GetPressedButtons()
        {
            MouseButton[] result = new MouseButton[_pressedButtons.Count];
            _pressedButtons.CopyTo(result);
            return result;
        }

        public void SendButton(MouseButton button)
        {
            uint downFlag;
            uint upFlag;
            GetMouseEventFlags(button, out downFlag, out upFlag);

            mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
            if (upFlag != 0)
            {
                mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
            }
        }

        public void SendButtonCombo(params MouseButton[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                uint downFlag, upFlag;
                GetMouseEventFlags(buttons[i], out downFlag, out upFlag);
                mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
            }

            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                uint downFlag, upFlag;
                GetMouseEventFlags(buttons[i], out downFlag, out upFlag);
                if (upFlag != 0)
                    mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
            }
        }

        private void GetMouseEventFlags(MouseButton btn, out uint down, out uint up)
        {
            down = 0;
            up = 0;

            if (btn == MouseButton.Left)
            {
                down = MOUSEEVENTF_LEFTDOWN;
                up = MOUSEEVENTF_LEFTUP;
            }
            else if (btn == MouseButton.Right)
            {
                down = MOUSEEVENTF_RIGHTDOWN;
                up = MOUSEEVENTF_RIGHTUP;
            }
            else if (btn == MouseButton.Middle)
            {
                down = MOUSEEVENTF_MIDDLEDOWN;
                up = MOUSEEVENTF_MIDDLEUP;
            }
            else if (btn == MouseButton.X1)
            {
                down = MOUSEEVENTF_XDOWN;
                up = MOUSEEVENTF_XUP;
            }
            else if (btn == MouseButton.X2)
            {
                down = MOUSEEVENTF_XDOWN;
                up = MOUSEEVENTF_XUP;
            }
            else if (btn == MouseButton.WheelUp)
            {
                down = MOUSEEVENTF_WHEEL;
                up = 0;
            }
            else if (btn == MouseButton.WheelDown)
            {
                down = MOUSEEVENTF_WHEEL;
                up = 0;
            }
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
    }
}
