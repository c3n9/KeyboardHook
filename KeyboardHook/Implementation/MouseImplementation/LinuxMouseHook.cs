using KeyboardHook.Enums;
using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyboardHook.Implementation.MouseImplementation
{
    internal class LinuxMouseHook : IMouseHook, IDisposable
    {
        public event Action<MouseButton> ButtonDown;
        public event Action<MouseButton> ButtonUp;

        private IntPtr _display;
        private Thread _pollThread;
        private bool _running;

        private int _prevMask = 0;

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern bool XQueryPointer(
            IntPtr display,
            IntPtr window,
            out IntPtr root_return,
            out IntPtr child_return,
            out int root_x_return,
            out int root_y_return,
            out int win_x_return,
            out int win_y_return,
            out uint mask_return);

        [DllImport("libX11.so.6")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libXtst.so.6")]
        private static extern void XTestFakeButtonEvent(IntPtr display, uint button, bool is_press, ulong delay);

        [DllImport("libX11.so.6")]
        private static extern void XFlush(IntPtr display);

        public LinuxMouseHook()
        {
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
                throw new Exception("Cannot open X11 display");

            _running = true;
            _pollThread = new Thread(PollLoop)
            {
                IsBackground = true,
                Name = "Mouse Poll Thread"
            };
            _pollThread.Start();
        }

        private void PollLoop()
        {
            IntPtr root = XDefaultRootWindow(_display);

            while (_running)
            {
                XQueryPointer(
                    _display,
                    root,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out uint mask);

                int curMask = (int)mask;

                ProcessMask(curMask);

                Thread.Sleep(5); 
            }
        }

        private void ProcessMask(int curMask)
        {
            Check(MouseButton.Left, curMask, _prevMask, 1 << 8);
            Check(MouseButton.Middle, curMask, _prevMask, 1 << 9);
            Check(MouseButton.Right, curMask, _prevMask, 1 << 10);
            

            _prevMask = curMask;
        }

        private void Check(MouseButton btn, int cur, int prev, int bit)
        {
            bool was = (prev & bit) != 0;
            bool now = (cur & bit) != 0;

            if (!was && now) ButtonDown?.Invoke(btn);
            if (was && !now) ButtonUp?.Invoke(btn);
        }

        public MouseButton[] GetPressedButtons()
        {
            List<MouseButton> list = new List<MouseButton>();

            if ((_prevMask & (1 << 8)) != 0) list.Add(MouseButton.Left);
            if ((_prevMask & (1 << 9)) != 0) list.Add(MouseButton.Middle);
            if ((_prevMask & (1 << 10)) != 0) list.Add(MouseButton.Right);

            return list.ToArray();
        }

        public void SendButton(MouseButton button)
        {
            int code = ButtonToX(button);
            if (code == 0) return;

            XTestFakeButtonEvent(_display, (uint)code, true, 0);
            XTestFakeButtonEvent(_display, (uint)code, false, 0);
            XFlush(_display);
        }

        public void SendButtonCombo(params MouseButton[] buttons)
        {
            foreach (var b in buttons)
                XTestFakeButtonEvent(_display, (uint)ButtonToX(b), true, 0);

            for (int i = buttons.Length - 1; i >= 0; i--)
                XTestFakeButtonEvent(_display, (uint)ButtonToX(buttons[i]), false, 0);

            XFlush(_display);
        }

        private int ButtonToX(MouseButton btn)
        {
            switch (btn)
            {
                case MouseButton.Left: return 1;
                case MouseButton.Middle: return 2;
                case MouseButton.Right: return 3;
                default: return 0;
            }
        }

        public void Dispose()
        {
            _running = false;
            _pollThread?.Join(1000);

            if (_display != IntPtr.Zero)
            {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }
    }
}
