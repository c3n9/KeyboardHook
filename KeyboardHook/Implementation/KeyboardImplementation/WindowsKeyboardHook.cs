using KeyboardHook.Interfaces;
using KeyboardHook.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using KeyboardHook.Extensions;

namespace KeyboardHook.Implementation.KeyboardImplementation
{
    // Uses Raw Input (WM_INPUT + RIDEV_INPUTSINK) instead of WH_KEYBOARD_LL.
    // A low-level hook sits in the system input chain: every keystroke in every app waits
    // until our callback returns, so a busy UI thread or a paused debugger lags the whole
    // keyboard. Raw Input is only a notification and never delays other applications.
    internal class WindowsKeyboardHook : IKeyboardHook, IDisposable
    {
        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly WndProc _wndProc;
        private readonly Thread _thread;
        private readonly HashSet<KeyboardKey> _pressedKeys = new HashSet<KeyboardKey>();
        private readonly object _lock = new object();
        private readonly string _className = "KeyboardHookRawInput_" + Guid.NewGuid().ToString("N");
        private uint _threadId;
        private IntPtr _hwnd;
        private Exception _startError;

        private const uint WM_INPUT = 0x00FF;
        private const uint WM_QUIT = 0x0012;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint WM_SYSKEYDOWN = 0x0104;
        private const uint WM_SYSKEYUP = 0x0105;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEKEYBOARD = 1;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const ushort RI_KEY_E0 = 0x02;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;
        private const uint MAPVK_VSC_TO_VK_EX = 3;
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        public event Action<KeyboardKey> KeyDown;
        public event Action<KeyboardKey> KeyUp;

        public WindowsKeyboardHook()
        {
            _wndProc = WindowProc;

            using (var ready = new ManualResetEventSlim())
            {
                _thread = new Thread(() => MessageLoop(ready))
                {
                    IsBackground = true,
                    Name = "Keyboard Raw Input Thread"
                };
                _thread.Start();
                ready.Wait();
            }

            if (_startError != null)
                throw _startError;
        }

        private void MessageLoop(ManualResetEventSlim ready)
        {
            try
            {
                _threadId = GetCurrentThreadId();

                var wc = new WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                    hInstance = GetModuleHandle(null),
                    lpszClassName = _className
                };
                if (RegisterClassEx(ref wc) == 0)
                    throw new InvalidOperationException("RegisterClassEx failed: " + Marshal.GetLastWin32Error());

                _hwnd = CreateWindowEx(0, _className, string.Empty, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wc.hInstance, IntPtr.Zero);
                if (_hwnd == IntPtr.Zero)
                    throw new InvalidOperationException("CreateWindowEx failed: " + Marshal.GetLastWin32Error());

                var device = new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01, // generic desktop
                    usUsage = 0x06,     // keyboard
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = _hwnd
                };
                if (!RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
                    throw new InvalidOperationException("RegisterRawInputDevices failed: " + Marshal.GetLastWin32Error());
            }
            catch (Exception ex)
            {
                _startError = ex;
                ready.Set();
                return;
            }

            ready.Set();

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }

            DestroyWindow(_hwnd);
            UnregisterClass(_className, GetModuleHandle(null));
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                try { HandleRawInput(lParam); }
                catch { /* never let a subscriber exception kill the message loop */ }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void HandleRawInput(IntPtr hRawInput)
        {
            uint headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
            uint size = 0;
            GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, headerSize);
            if (size == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, headerSize) != size)
                    return;

                var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
                if (header.dwType != RIM_TYPEKEYBOARD) return;

                var kb = Marshal.PtrToStructure<RAWKEYBOARD>(buffer + (int)headerSize);
                if (kb.VKey == 0xFF) return; // fake key generated for escaped sequences

                var key = KeyboardKeyExtensions.FromPlatformCode(ToSidedVirtualKey(kb));

                if (kb.Message == WM_KEYDOWN || kb.Message == WM_SYSKEYDOWN)
                {
                    lock (_lock) _pressedKeys.Add(key);
                    KeyDown?.Invoke(key);
                }
                else if (kb.Message == WM_KEYUP || kb.Message == WM_SYSKEYUP)
                {
                    lock (_lock) _pressedKeys.Remove(key);
                    KeyUp?.Invoke(key);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // Raw Input reports generic VK_SHIFT/VK_CONTROL/VK_MENU; WH_KEYBOARD_LL used to report the
        // left/right variants, so translate to keep KeyboardKey.LShift/RShift etc. working.
        private static int ToSidedVirtualKey(RAWKEYBOARD kb)
        {
            bool e0 = (kb.Flags & RI_KEY_E0) != 0;
            switch (kb.VKey)
            {
                case VK_SHIFT:
                    var vk = (int)MapVirtualKey(kb.MakeCode, MAPVK_VSC_TO_VK_EX);
                    return vk == VK_RSHIFT ? VK_RSHIFT : VK_LSHIFT;
                case VK_CONTROL:
                    return e0 ? VK_RCONTROL : VK_LCONTROL;
                case VK_MENU:
                    return e0 ? VK_RMENU : VK_LMENU;
                default:
                    return kb.VKey;
            }
        }

        public KeyboardKey[] GetPressedKeys()
        {
            lock (_lock)
            {
                var arr = new KeyboardKey[_pressedKeys.Count];
                _pressedKeys.CopyTo(arr);
                return arr;
            }
        }

        public void SendKey(KeyboardKey key)
        {
            var keyCode = KeyboardKeyExtensions.ToPlatformCode(key);
            keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void SendKeyCombo(params KeyboardKey[] keyCodes)
        {
            foreach (var k in keyCodes)
                keybd_event((byte)KeyboardKeyExtensions.ToPlatformCode(k), 0, 0, UIntPtr.Zero);

            for (int i = keyCodes.Length - 1; i >= 0; i--)
                keybd_event((byte)KeyboardKeyExtensions.ToPlatformCode(keyCodes[i]), 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void Dispose()
        {
            if (_threadId != 0)
                PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(1000);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint numDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetMessage(out MSG msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DispatchMessage(ref MSG msg);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;
    }
}
