using KeyboardHook.Interfaces;
using KeyboardHook.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardHook.Implementation
{
    internal class WindowsKeyboardHook : IKeyboardHook, IDisposable
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelKeyboardProc _proc;
        private readonly IntPtr _hookId;

        private readonly HashSet<KeyboardKey> _pressedKeys = new HashSet<KeyboardKey>(); // для сочетаний

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        public event Action<KeyboardKey> KeyDown;
        public event Action<KeyboardKey> KeyUp;

        public WindowsKeyboardHook()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                var curModule = curProcess.MainModule;
                if (curModule == null)
                    throw new InvalidOperationException("Unable to get current process module.");

                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int vkCode = kb.vkCode;
                var key = (KeyboardKey)vkCode;

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    _pressedKeys.Add(key);
                    var handler = KeyDown;
                    if (handler != null) handler(key);
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    _pressedKeys.Remove(key);
                    var handler = KeyUp;
                    if (handler != null) handler(key);
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// Возвращает копию текущих нажатых клавиш (для сочетаний)
        /// </summary>
        public KeyboardKey[] GetPressedKeys()
        {
            var arr = new List<KeyboardKey>();
            foreach (var k in _pressedKeys)
                arr.Add(k);
            return arr.ToArray();
        }

        public void SendKey(KeyboardKey key)
        {
            var keyCode = (int)key;
            keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void SendKeyCombo(params KeyboardKey[] keyCodes)
        {
            foreach (var k in keyCodes)
                keybd_event((byte)(int)k, 0, 0, UIntPtr.Zero);

            for (int i = keyCodes.Length - 1; i >= 0; i--)
                keybd_event((byte)(int)keyCodes[i], 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;
    }
}
