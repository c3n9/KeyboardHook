using KeyboardHook.Enums;
using KeyboardHook.Extensions;
using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyboardHook.Implementation.KeyboardImplementation
{
    internal class LinuxKeyboardHook : IKeyboardHook, IDisposable
    {
        public event Action<KeyboardKey> KeyDown;
        public event Action<KeyboardKey> KeyUp;

        private IntPtr _sendDisplay;   
        private IntPtr _pollingDisplay; 
        private Thread _eventThread;
        private bool _running;
        private byte[] _previousKeys = new byte[32];

        #region X11 imports

        [DllImport("libX11.so.6")]
        private static extern int XInitThreads(); 

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern bool XQueryKeymap(IntPtr display, byte[] keys);

        [DllImport("libXtst.so.6")]
        private static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool press, uint delay);

        [DllImport("libX11.so.6")]
        private static extern int XFlush(IntPtr display);

        #endregion

        static LinuxKeyboardHook()
        {
            XInitThreads();
        }

        public LinuxKeyboardHook()
        {
            _sendDisplay = XOpenDisplay(IntPtr.Zero);
            _pollingDisplay = XOpenDisplay(IntPtr.Zero);

            if (_sendDisplay == IntPtr.Zero || _pollingDisplay == IntPtr.Zero)
                throw new Exception("Не удалось открыть X Display. Проверьте переменную DISPLAY.");

            _running = true;
            _eventThread = new Thread(KeymapPollingLoop)
            {
                IsBackground = true,
                Name = "Keyboard Polling Thread",
                Priority = ThreadPriority.AboveNormal 
            };
            _eventThread.Start();
        }

        private void KeymapPollingLoop()
        {
            while (_running)
            {
                try
                {
                    byte[] currentKeys = new byte[32];
                    if (XQueryKeymap(_pollingDisplay, currentKeys))
                    {
                        ProcessKeymapChanges(currentKeys);
                    }

                    Thread.Sleep(16);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KeyboardHook] Polling error: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        private void ProcessKeymapChanges(byte[] currentKeys)
        {
            for (int i = 0; i < 32; i++)
            {
                if (currentKeys[i] != _previousKeys[i])
                {
                    for (int bit = 0; bit < 8; bit++)
                    {
                        bool wasPressed = (_previousKeys[i] & (1 << bit)) != 0;
                        bool isPressed = (currentKeys[i] & (1 << bit)) != 0;

                        if (isPressed != wasPressed)
                        {
                            int keyCode = i * 8 + bit;
                            var key = KeyboardKeyExtensions.FromPlatformCode(keyCode);

                            if (isPressed)
                                KeyDown?.Invoke(key);
                            else
                                KeyUp?.Invoke(key);
                        }
                    }
                }
            }
            Array.Copy(currentKeys, _previousKeys, 32);
        }

        public void SendKey(KeyboardKey key)
        {
            uint code = (uint)KeyboardKeyExtensions.ToPlatformCode(key);
            XTestFakeKeyEvent(_sendDisplay, code, true, 0);
            XTestFakeKeyEvent(_sendDisplay, code, false, 0);
            XFlush(_sendDisplay);
        }

        public void SendKeyCombo(params KeyboardKey[] keyCodes)
        {
            foreach (var key in keyCodes)
                XTestFakeKeyEvent(_sendDisplay, (uint)KeyboardKeyExtensions.ToPlatformCode(key), true, 0);

            for (int i = keyCodes.Length - 1; i >= 0; i--)
                XTestFakeKeyEvent(_sendDisplay, (uint)KeyboardKeyExtensions.ToPlatformCode(keyCodes[i]), false, 0);

            XFlush(_sendDisplay);
        }

        public void Dispose()
        {
            _running = false;
            _eventThread?.Join(500);

            if (_sendDisplay != IntPtr.Zero)
            {
                XCloseDisplay(_sendDisplay);
                _sendDisplay = IntPtr.Zero;
            }

            if (_pollingDisplay != IntPtr.Zero)
            {
                XCloseDisplay(_pollingDisplay);
                _pollingDisplay = IntPtr.Zero;
            }
        }
    }
}