using KeyboardHook.Enums;
using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly object _syncRoot = new object();

        public LinuxMouseHook()
        {
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
                throw new Exception("Cannot open X11 display");

            InitXInput2();

            _running = true;
            _eventThread = new Thread(EventLoop) { IsBackground = true, Name = "LinuxMouseHook" };
            _eventThread.Start();
        }

        private void InitXInput2()
        {
            int opcode, eventBase, errorBase;

            if (XQueryExtension(_display, "XInputExtension", out opcode, out eventBase, out errorBase) == 0)
                throw new Exception("XInput2 not available");

            XIEventMask mask = new XIEventMask();
            mask.deviceid = XIAllMasterDevices;
            mask.mask_len = XIMaskLen(XI_ButtonPress);
            mask.mask = Marshal.AllocHGlobal(mask.mask_len);

            byte[] maskBytes = new byte[mask.mask_len];
            XISetMask(maskBytes, XI_ButtonPress);
            XISetMask(maskBytes, XI_ButtonRelease);

            Marshal.Copy(maskBytes, 0, mask.mask, mask.mask_len);

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
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in mouse hook event loop: {ex.Message}");
                }
            }
        }

        private void HandleButton(bool isPress, XGenericEventCookie cookie)
        {
            int btn = GetButton(cookie);
            MouseButton? b = ConvertButton(btn);
            if (!b.HasValue) return;

            lock (_syncRoot)
            {
                if (isPress)
                {
                    _pressed.Add(b.Value);
                }
                else
                {
                    _pressed.Remove(b.Value);
                }
            }

            Task.Run(() =>
            {
                try
                {
                    if (isPress)
                        ButtonDown?.Invoke(b.Value);
                    else
                        ButtonUp?.Invoke(b.Value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in mouse hook event handler: {ex.Message}");
                }
            });
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
            switch (btn)
            {
                case 1: return MouseButton.Left;
                case 2: return MouseButton.Middle;
                case 3: return MouseButton.Right;
                case 8: return MouseButton.X1;
                case 9: return MouseButton.X2;
                case 4: return MouseButton.WheelUp;
                case 5: return MouseButton.WheelDown;
                default: return null;
            }
        }

        public MouseButton[] GetPressedButtons()
        {
            lock (_syncRoot)
            {
                MouseButton[] arr = new MouseButton[_pressed.Count];
                _pressed.CopyTo(arr);
                return arr;
            }
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

            switch (btn)
            {
                case MouseButton.Left: code = 1; break;
                case MouseButton.Middle: code = 2; break;
                case MouseButton.Right: code = 3; break;
                case MouseButton.X1: code = 8; break;
                case MouseButton.X2: code = 9; break;
                case MouseButton.WheelUp: code = 4; break;
                case MouseButton.WheelDown: code = 5; break;
            }

            if (code == 0) return;

            XTestFakeButtonEvent(_display, (uint)code, down ? 1 : 0, 0);
            XFlush(_display);
        }

        public void Dispose()
        {
            _running = false;
            
            if (_eventThread != null && _eventThread.IsAlive)
            {
                if (!_eventThread.Join(1000))
                {
                    _eventThread.Abort();
                }
            }

            if (_display != IntPtr.Zero)
            {
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }

        private const int GenericEvent = 35;
        private const int XI_ButtonPress = 3;
        private const int XI_ButtonRelease = 4;
        private const int XIAllMasterDevices = 1;

        private static int XIMaskLen(int eventType)
        {
            return (eventType + 7) / 8;
        }

        private static void XISetMask(byte[] mask, int eventType)
        {
            mask[eventType / 8] |= (byte)(1 << (eventType % 8));
        }

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
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            private long[] padding;
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
            public int root;
            public int @event;
            public int child;
            public double root_x;
            public double root_y;
            public double event_x;
            public double event_y;
            public int flags;
            public int button_mask;
            public int valuator_mask;
            public int group;
            public int mods;
            public IntPtr valuators;
        }

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern void XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XDefaultRootWindow(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern void XNextEvent(IntPtr display, ref XEvent ev);

        [DllImport("libX11.so.6")]
        private static extern void XFlush(IntPtr display);

        [DllImport("libXi.so.6")]
        private static extern int XISelectEvents(IntPtr display, IntPtr win, ref XIEventMask mask, int num_masks);

        [DllImport("libXi.so.6")]
        private static extern int XQueryExtension(IntPtr display, string name, out int opcode, out int event_base, out int error_base);

        [DllImport("libX11.so.6")]
        private static extern int XGetEventData(IntPtr display, ref XGenericEventCookie cookie);

        [DllImport("libX11.so.6")]
        private static extern void XFreeEventData(IntPtr display, ref XGenericEventCookie cookie);

        [DllImport("libXtst.so.6")]
        private static extern void XTestFakeButtonEvent(IntPtr display, uint button, int is_press, ulong delay);
    }
}