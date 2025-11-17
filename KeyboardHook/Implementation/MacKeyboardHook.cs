using KeyboardHook.Interfaces;
using KeyboardHook.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyboardHook.Implementation
{
    internal class MacKeyboardHook : IKeyboardHook, IDisposable
    {
        public event Action<KeyboardKey> KeyDown;
        public event Action<KeyboardKey> KeyUp;

        private Thread _runLoopThread;
        private IntPtr _eventTap; // CFMachPortRef
        private IntPtr _runLoopSource; // CFRunLoopSourceRef
        private IntPtr _runLoop; // CFRunLoopRef

        private const int kCGEventKeyDown = 10;
        private const int kCGEventKeyUp = 11;
        private const int kCGKeyboardEventKeycode = 0;

        public MacKeyboardHook()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacKeyboardHook can only be constructed on macOS");

            StartEventTap();
        }

        private void StartEventTap()
        {
            ulong mask = (1UL << kCGEventKeyDown) | (1UL << kCGEventKeyUp);
            _eventTap = CGEventTapCreate(0, 0, 0, mask, EventCallback, IntPtr.Zero);
            if (_eventTap == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create event tap. Is the application permitted to monitor input events?");

            _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
            _runLoop = CFRunLoopGetCurrent();

            CFRunLoopAddSource(_runLoop, _runLoopSource, CFRunLoopMode.kCFRunLoopCommonModes);
            CGEventTapEnable(_eventTap, true);

            _runLoopThread = new Thread(RunLoopThreadProc)
            {
                IsBackground = true,
                Name = "MacKeyboardHook RunLoop"
            };
            _runLoopThread.Start();
        }

        private void RunLoopThreadProc()
        {
            // Run loop for the thread that will process events
            CFRunLoopRun();
        }

        private IntPtr EventCallback(IntPtr proxy, int type, IntPtr ev, IntPtr userInfo)
        {
            try
            {
                if (type == kCGEventKeyDown || type == kCGEventKeyUp)
                {
                    long kc = CGEventGetIntegerValueField(ev, kCGKeyboardEventKeycode);
                    var key = (KeyboardKey)(int)kc;

                    if (type == kCGEventKeyDown)
                    {
                        var h = KeyDown;
                        if (h != null) h(key);
                    }
                    else
                    {
                        var h = KeyUp;
                        if (h != null) h(key);
                    }
                }
            }
            catch
            {
                // swallow
            }

            return ev;
        }

        public void SendKey(KeyboardKey key)
        {
            // Create keyboard down and up events
            IntPtr down = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)key, true);
            IntPtr up = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)key, false);
            CGEventPost(0, down);
            CGEventPost(0, up);
            CFRelease(down);
            CFRelease(up);
        }

        public void SendKeyCombo(params KeyboardKey[] keys)
        {
            var events = new List<IntPtr>();
            try
            {
                // key down in order
                foreach (var k in keys)
                {
                    var e = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)k, true);
                    events.Add(e);
                    CGEventPost(0, e);
                }

                // key up in reverse order
                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    var e = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)keys[i], false);
                    events.Add(e);
                    CGEventPost(0, e);
                }
            }
            finally
            {
                foreach (var e in events)
                    CFRelease(e);
            }
        }

        public KeyboardKey[] GetPressedKeys()
        {
            // macOS does not provide a simple global keymap like X11 here; returning empty array.
            return new KeyboardKey[0];
        }

        public void Dispose()
        {
            try
            {
                if (_eventTap != IntPtr.Zero)
                {
                    CGEventTapEnable(_eventTap, false);
                    CFMachPortInvalidate(_eventTap);
                    _eventTap = IntPtr.Zero;
                }

                if (_runLoop != IntPtr.Zero)
                {
                    CFRunLoopStop(_runLoop);
                    _runLoop = IntPtr.Zero;
                }

                if (_runLoopSource != IntPtr.Zero)
                {
                    CFRelease(_runLoopSource);
                    _runLoopSource = IntPtr.Zero;
                }

                if (_runLoopThread != null && _runLoopThread.IsAlive)
                {
                    _runLoopThread.Join(500);
                }
            }
            catch
            {
            }
        }

        #region PInvoke

        private delegate IntPtr CGEventTapCallBack(IntPtr proxy, int type, IntPtr ev, IntPtr userInfo);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventTapCreate(int tap, int place, int options, ulong eventsOfInterest, CGEventTapCallBack callback, IntPtr userInfo);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CGEventTapEnable(IntPtr tap, bool enable);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern long CGEventGetIntegerValueField(IntPtr ev, int field);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, bool keyDown);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CGEventPost(int tap, IntPtr ev);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CFMachPortInvalidate(IntPtr machPort);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr machPort, int order);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFRunLoopGetCurrent();

        private static class CFRunLoopMode
        {
            public static IntPtr kCFRunLoopCommonModes = (IntPtr)0; // not used as CF types in PInvoke; placeholder
        }

        private static class CFRunLoopModeString
        {
            // placeholder
        }

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopRun();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopStop(IntPtr rl);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

        #endregion
    }
}
