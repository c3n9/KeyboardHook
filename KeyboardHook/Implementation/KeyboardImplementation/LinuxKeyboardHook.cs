using KeyboardHook.Enums;
using KeyboardHook.Extensions;
using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace KeyboardHook.Implementation.KeyboardImplementation
{
    internal class LinuxKeyboardHook : IKeyboardHook, IDisposable
    {
        public event Action<KeyboardKey> KeyDown;
        public event Action<KeyboardKey> KeyUp;

        private IntPtr _display;
        private Thread _eventThread;
        private bool _running;
        private byte[] _previousKeys = new byte[32];

        #region X11 imports

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

        public LinuxKeyboardHook()
        {
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
                throw new Exception("Couldn't open X Display");

            _running = true;
            _eventThread = new Thread(KeymapPollingLoop)
            {
                IsBackground = true,
                Name = "Keyboard Polling Thread"
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
                    if (XQueryKeymap(_display, currentKeys))
                    {
                        ProcessKeymapChanges(currentKeys);
                    }
                    Thread.Sleep(16); // ~60 FPS
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Polling error: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        private void ProcessKeymapChanges(byte[] currentKeys)
        {
            for (int i = 0; i < 32; i++)
            {
                byte current = currentKeys[i];
                byte previous = _previousKeys[i];

                if (current != previous)
                {
                    for (int bit = 0; bit < 8; bit++)
                    {
                        bool wasPressed = (previous & (1 << bit)) != 0;
                        bool isPressed = (current & (1 << bit)) != 0;
                        int keyCode = i * 8 + bit;
                        var key = KeyboardKeyExtensions.FromPlatformCode(keyCode);

                        if (isPressed && !wasPressed)
                        {
                            var handler = KeyDown;
                            if (handler != null) handler(key);
                        }
                        else if (!isPressed && wasPressed)
                        {
                            var handler = KeyUp;
                            if (handler != null) handler(key);
                        }
                    }
                }
            }

            Array.Copy(currentKeys, _previousKeys, 32);
        }

        public void SendKey(KeyboardKey key)
        {
            XTestFakeKeyEvent(_display, (uint)KeyboardKeyExtensions.ToPlatformCode(key), true, 0);
            XTestFakeKeyEvent(_display, (uint)KeyboardKeyExtensions.ToPlatformCode(key), false, 0);
            XFlush(_display);
        }

        public void SendKeyCombo(params KeyboardKey[] keyCodes)
        {
            foreach (var keyCode in keyCodes)
            {
                XTestFakeKeyEvent(_display, (uint)KeyboardKeyExtensions.ToPlatformCode(keyCode), true, 0);
            }

            for (int i = keyCodes.Length - 1; i >= 0; i--)
            {
                XTestFakeKeyEvent(_display, (uint)KeyboardKeyExtensions.ToPlatformCode(keyCodes[i]), false, 0);
            }

            XFlush(_display);
        }

        public KeyboardKey[] GetPressedKeys()
        {
            var list = new List<KeyboardKey>();
            for (int i = 0; i < _previousKeys.Length; i++)
            {
                byte b = _previousKeys[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((b & (1 << bit)) != 0)
                        list.Add((KeyboardKey)(i * 8 + bit));
                }
            }
            return list.ToArray();
        }

        public void Dispose()
        {
            _running = false;
            _eventThread?.Join(1000);

            if (_display != IntPtr.Zero)
            {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }
    }
}
