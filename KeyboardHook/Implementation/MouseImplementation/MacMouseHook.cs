using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using KeyboardHook.Enums;
using KeyboardHook.Interfaces;

namespace KeyboardHook.Implementation.MouseImplementation
{
    public class MacMouseHook : IMouseHook, IDisposable
    {
        public event Action<MouseButton> ButtonDown;
        public event Action<MouseButton> ButtonUp;


        private IntPtr _tapPort;
        private IntPtr _runLoopSource;
        private IntPtr _runLoop;
        private Thread _hookThread;
        private CGEventTapCallBack _callbackDelegate;
        private bool _isDisposed;

        public MacMouseHook()
        {
            Start();
        }

        private void Start()
        {
            _hookThread = new Thread(() =>
            {
                try
                {
                    _callbackDelegate = HookCallback;

                    var eventsToListen = (1UL << (int)CGEventType.LeftMouseDown) |
                                         (1UL << (int)CGEventType.LeftMouseUp) |
                                         (1UL << (int)CGEventType.RightMouseDown) |
                                         (1UL << (int)CGEventType.RightMouseUp) |
                                         (1UL << (int)CGEventType.OtherMouseDown) |
                                         (1UL << (int)CGEventType.OtherMouseUp);
                    
                    _tapPort = CGEventTapCreate(
                        CGEventTapLocation.kCGSessionEventTap,
                        CGEventTapPlacement.kCGHeadInsertEventTap,
                        CGEventTapOptions.kCGEventTapOptionDefault,
                        eventsToListen,
                        _callbackDelegate,
                        IntPtr.Zero
                    );

                    if (_tapPort == IntPtr.Zero)
                    {
                        Console.WriteLine(
                            "Unable to create Event Tap. Check permissions.");
                        throw new UnauthorizedAccessException(
                            "Accessibility permissions are required to intercept keys.");
                    }

                    _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _tapPort, IntPtr.Zero);
                    _runLoop = CFRunLoopGetCurrent();
                    var defaultMode = CFStringCreateWithCString(IntPtr.Zero, "kCFRunLoopDefaultMode", 0);
                    CFRunLoopAddSource(_runLoop, _runLoopSource, defaultMode);
                    CGEventTapEnable(_tapPort, true);
                    CFRunLoopRun();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MacMouseHook Thread Error: {ex.Message}");
                }
            });

            _hookThread.IsBackground = true;
            _hookThread.Start();
        }
        private IntPtr HookCallback(IntPtr proxy, CGEventType type, IntPtr eventRef, IntPtr userInfo)
        {
            if (_isDisposed) return eventRef;
            
            if (type == CGEventType.TapDisabledByTimeout || type == CGEventType.TapDisabledByUserInput)
            {
                CGEventTapEnable(_tapPort, true);
                return eventRef;
            }

            bool isDown = false;
            MouseButton? btn = null;
            switch (type)
            {
                case CGEventType.LeftMouseDown:
                    btn = MouseButton.Left;
                    isDown = true;
                    break;
                case CGEventType.LeftMouseUp:
                    btn = MouseButton.Left;
                    isDown = false;
                    break;

                case CGEventType.RightMouseDown:
                    btn = MouseButton.Right;
                    isDown = true;
                    break;
                case CGEventType.RightMouseUp:
                    btn = MouseButton.Right;
                    isDown = false;
                    break;

                case CGEventType.OtherMouseDown:
                case CGEventType.OtherMouseUp:
                    long number = CGEventGetIntegerValueField(eventRef, CGEventField.kCGMouseEventButtonNumber);
                    if (number == 2)
                    {
                        btn = MouseButton.Middle;
                        isDown = (type == CGEventType.OtherMouseDown);
                    }

                    break;
            }

            if (btn.HasValue)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (isDown) ButtonDown?.Invoke(btn.Value);
                        else ButtonUp?.Invoke(btn.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                    }
                });
            }

            return eventRef;
        }

        public void SendButton(MouseButton button)
        {
            CGEventType downType, upType;
            CGMouseButton nativeButton;

            switch (button)
            {
                case MouseButton.Left:
                    nativeButton = CGMouseButton.kCGMouseButtonLeft;
                    downType = CGEventType.LeftMouseDown;
                    upType = CGEventType.LeftMouseUp;
                    break;
                case MouseButton.Right:
                    nativeButton = CGMouseButton.kCGMouseButtonRight;
                    downType = CGEventType.RightMouseDown;
                    upType = CGEventType.RightMouseUp;
                    break;
                case MouseButton.Middle:
                    nativeButton = CGMouseButton.kCGMouseButtonCenter;
                    downType = CGEventType.OtherMouseDown;
                    upType = CGEventType.OtherMouseUp;
                    break;
                default:
                    return;
            }

            // Можно передовать координаты курсора, пока (0,0)
            var point = new CGPoint() { X = 0, Y = 0 };

            var evtDown = CGEventCreateMouseEvent(IntPtr.Zero, downType, point, nativeButton);
            CGEventPost(CGEventTapLocation.kCGHIDEventTap, evtDown);
            CFRelease(evtDown);

            var evtUp = CGEventCreateMouseEvent(IntPtr.Zero, upType, point, nativeButton);
            CGEventPost(CGEventTapLocation.kCGHIDEventTap, evtUp);
            CFRelease(evtUp);
        }

        public void SendButtonCombo(params MouseButton[] buttons)
        {
            foreach (var btn in buttons) SendButton(btn);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_runLoop != IntPtr.Zero) CFRunLoopStop(_runLoop);
            if (_tapPort != IntPtr.Zero) CGEventTapEnable(_tapPort, false);
        }

        #region Native Interop (P/Invoke)

        private const string CoreGraphicsLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CGEventTapCallBack(IntPtr proxy, CGEventType type, IntPtr eventRef, IntPtr userInfo);

        private enum CGEventTapLocation
        {
            kCGHIDEventTap = 0,
            kCGSessionEventTap = 1
        }

        private enum CGEventTapPlacement
        {
            kCGHeadInsertEventTap = 0
        }

        private enum CGEventTapOptions
        {
            kCGEventTapOptionDefault = 0
        }

        private enum CGMouseButton : uint
        {
            kCGMouseButtonLeft = 0,
            kCGMouseButtonRight = 1,
            kCGMouseButtonCenter = 2
        }

        private enum CGEventType : uint
        {
            LeftMouseDown = 1,
            LeftMouseUp = 2,
            RightMouseDown = 3,
            RightMouseUp = 4,
            OtherMouseDown = 25,
            OtherMouseUp = 26,
            TapDisabledByTimeout = 0xFFFFFFFE,
            TapDisabledByUserInput = 0xFFFFFFFF
        }

        private enum CGEventField
        {
            kCGMouseEventButtonNumber = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CGPoint
        {
            public double X;
            public double Y;
        }

        [DllImport(CoreGraphicsLib)]
        private static extern IntPtr CGEventTapCreate(CGEventTapLocation tap, CGEventTapPlacement place,
            CGEventTapOptions options, ulong eventsOfInterest, CGEventTapCallBack callback, IntPtr userInfo);

        [DllImport(CoreGraphicsLib)]
        private static extern void CGEventTapEnable(IntPtr tap, bool enable);

        [DllImport(CoreGraphicsLib)]
        private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr tap, IntPtr order);

        [DllImport(CoreGraphicsLib)]
        private static extern long CGEventGetIntegerValueField(IntPtr evt, CGEventField field);

        [DllImport(CoreGraphicsLib)]
        private static extern IntPtr CGEventCreateMouseEvent(IntPtr source, CGEventType mouseType,
            CGPoint mouseCursorPosition, CGMouseButton mouseButton);

        [DllImport(CoreGraphicsLib)]
        private static extern void CGEventPost(CGEventTapLocation tap, IntPtr eventRef);

        [DllImport(CoreGraphicsLib)]
        private static extern void CFRelease(IntPtr cf);

        [DllImport(CoreFoundationLib)]
        private static extern IntPtr CFRunLoopGetCurrent();

        [DllImport(CoreFoundationLib)]
        private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

        [DllImport(CoreFoundationLib)]
        private static extern void CFRunLoopRun();

        [DllImport(CoreFoundationLib)]
        private static extern void CFRunLoopStop(IntPtr rl);

        [DllImport(CoreFoundationLib)]
        private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string cStr, int encoding);

        #endregion
    }
}