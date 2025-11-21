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
        private Thread _eventThread;
        private bool _running;
        private readonly HashSet<MouseButton> _pressed = new HashSet<MouseButton>();

        public LinuxMouseHook()
        {
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
                throw new Exception("Cannot open X11 display");

            InitXInput2();

            _running = true;
            _eventThread = new Thread(EventLoop) { IsBackground = true };
            _eventThread.Start();
        }

        private void InitXInput2()
        {
            int opcode, eventBase, errorBase;

            if (XQueryExtension(_display, "XInputExtension", out opcode, out eventBase, out errorBase) == 0)
                throw new Exception("XInput2 not available");

            XIEventMask mask = new XIEventMask();
            mask.deviceid = XIAllMasterDevices;
            mask.mask_len = 1;
            mask.mask = Marshal.AllocHGlobal(1);

            byte[] maskBytes = new byte[1];
            maskBytes[0] = (byte)((1 << XI_ButtonPress) | (1 << XI_ButtonRelease));

            Marshal.Copy(maskBytes, 0, mask.mask, 1);

            int rootWinInt = XDefaultRootWindow(_display);
            IntPtr rootWin = new IntPtr(rootWinInt);

            XISelectEvents(_display, rootWin, ref mask, 1);
            XFlush(_display);

            Marshal.FreeHGlobal(mask.mask);
        }

        private void EventLoop()
        {
            XEvent ev = new XEvent();

            while (_running)
            {
                XNextEvent(_display, ref ev);

                if (ev.type == GenericEvent)
                {
                    XGenericEventCookie cookie = ev.XGenericEventCookie;

                    if (XGetEventData(_display, ref cookie) != 0)
                    {
                        try
                        {
                            if (cookie.evtype == XI_ButtonPress)
                                HandleButton(true, cookie);
                            else if (cookie.evtype == XI_ButtonRelease)
                                HandleButton(false, cookie);
                        }
                        finally
                        {
                            XFreeEventData(_display, ref cookie);
                        }
                    }
                }
            }
        }

        private void HandleButton(bool isPress, XGenericEventCookie cookie)
        {
            int btn = GetButton(cookie);
            MouseButton? b = ConvertButton(btn);
            if (!b.HasValue) return;

            if (isPress)
            {
                _pressed.Add(b.Value);
                if (ButtonDown != null) ButtonDown(b.Value);
            }
            else
            {
                _pressed.Remove(b.Value);
                if (ButtonUp != null) ButtonUp(b.Value);
            }
        }

        private int GetButton(XGenericEventCookie cookie)
        {
            unsafe
            {
                XIDeviceEvent* d = (XIDeviceEvent*)cookie.data.ToPointer();
                return d->detail;
            }
        }

        private MouseButton? ConvertButton(int btn)
        {
            if (btn == 1) return MouseButton.Left;
            if (btn == 2) return MouseButton.Middle;
            if (btn == 3) return MouseButton.Right;
            if (btn == 8) return MouseButton.X1;
            if (btn == 9) return MouseButton.X2;
            return null;
        }

        public MouseButton[] GetPressedButtons()
        {
            MouseButton[] arr = new MouseButton[_pressed.Count];
            _pressed.CopyTo(arr);
            return arr;
        }

        public void SendButton(MouseButton button)
        {
            Send(button, true);
            Send(button, false);
        }

        public void SendButtonCombo(params MouseButton[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
                Send(buttons[i], true);

            for (int i = buttons.Length - 1; i >= 0; i--)
                Send(buttons[i], false);
        }

        private void Send(MouseButton btn, bool down)
        {
            int code = 0;

            if (btn == MouseButton.Left) code = 1;
            else if (btn == MouseButton.Middle) code = 2;
            else if (btn == MouseButton.Right) code = 3;
            else if (btn == MouseButton.X1) code = 8;
            else if (btn == MouseButton.X2) code = 9;
            else if (btn == MouseButton.WheelUp) code = 4;
            else if (btn == MouseButton.WheelDown) code = 5;

            if (code == 0) return;

            XTestFakeButtonEvent(_display, (uint)code, down ? 1 : 0, 0);
            XFlush(_display);
        }

        public void Dispose()
        {
            _running = false;
            Thread.Sleep(30);
            if (_display != IntPtr.Zero)
                XCloseDisplay(_display);
        }

        private const int GenericEvent = 35;
        private const int XI_ButtonPress = 3;
        private const int XI_ButtonRelease = 4;
        private const int XIAllMasterDevices = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct XIEventMask
        {
            public int deviceid;
            public int mask_len;
            public IntPtr mask;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XEvent
        {
            public int type;
            private long p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17, p18, p19, p20, p21, p22, p23;
            public XGenericEventCookie XGenericEventCookie;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XGenericEventCookie
        {
            public int type;
            public uint serial;
            public int send_event;
            public IntPtr display;
            public int extension;
            public int evtype;
            public int cookie;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct XIDeviceEvent
        {
            public int type;
            public IntPtr serial;
            public int send_event;
            public IntPtr display;
            public int extension;
            public int evtype;
            public int time;
            public int deviceid;
            public int sourceid;
            public int detail;
        }

        [DllImport("X11")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("X11")]
        private static extern void XCloseDisplay(IntPtr display);

        [DllImport("X11")]
        private static extern int XDefaultRootWindow(IntPtr display);

        [DllImport("X11")]
        private static extern void XNextEvent(IntPtr display, ref XEvent ev);

        [DllImport("X11")]
        private static extern void XFlush(IntPtr display);

        [DllImport("Xi")]
        private static extern int XISelectEvents(IntPtr display, IntPtr win, ref XIEventMask mask, int num_masks);

        [DllImport("Xi")]
        private static extern int XQueryExtension(IntPtr display, string name, out int opcode, out int event_base, out int error_base);

        [DllImport("X11")]
        private static extern int XGetEventData(IntPtr display, ref XGenericEventCookie cookie);

        [DllImport("X11")]
        private static extern void XFreeEventData(IntPtr display, ref XGenericEventCookie cookie);

        [DllImport("Xtst")]
        private static extern void XTestFakeButtonEvent(IntPtr display, uint button, int is_press, ulong delay);
    }
}
