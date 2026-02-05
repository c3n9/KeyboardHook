using KeyboardHook.Interfaces;
using KeyboardHook.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using KeyboardHook.Extensions;

namespace KeyboardHook.Implementation.KeyboardImplementation
{
    internal class MacKeyboardHook : IKeyboardHook, IDisposable
    {
        public event Action<KeyboardKey> KeyDown;
        public event Action<KeyboardKey> KeyUp;

        private Thread _runLoopThread;
        private IntPtr _eventTap;
        private IntPtr _runLoopSource;
        private IntPtr _runLoop;
        private bool _disposed = false;
        
        private CGEventTapCallBack _callbackKeepAlive;

        private const int kCGEventKeyDown = 10;
        private const int kCGEventKeyUp = 11;
        private const int kCGKeyboardEventKeycode = 9;
        
        private const int kCGSessionEventTap = 1;
        private const int kCGAnnotatedSessionEventTap = 2;

        public MacKeyboardHook()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacKeyboardHook can only be constructed on macOS");

            InitializeEventTap();
        }

        private void InitializeEventTap()
        {
            _callbackKeepAlive = EventCallback;

            if (!TryCreateEventTap())
            {
                Console.WriteLine(
                    "Unable to create Event Tap. Check permissions.");
                throw new UnauthorizedAccessException(
                    "Accessibility permissions are required to intercept keys.");
            }

            StartRunLoop();
        }

        private bool TryCreateEventTap()
        {
            var eventMask = (1UL << kCGEventKeyDown) | (1UL << kCGEventKeyUp);
            
            _eventTap = CGEventTapCreate(
                kCGSessionEventTap,
                0, // HeadInsert
                1, // Options: Default
                eventMask,
                _callbackKeepAlive,
                IntPtr.Zero
            );

            if (_eventTap == IntPtr.Zero)
                return false;

            _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
            if (_runLoopSource == IntPtr.Zero)
            {
                CFRelease(_eventTap);
                return false;
            }

            CGEventTapEnable(_eventTap, true);
            return true;
        }

        private void StartRunLoop()
        {
            _runLoopThread = new Thread(() =>
            {
                try
                {
                    _runLoop = CFRunLoopGetCurrent();
                    
                    IntPtr kCFRunLoopCommonModes = GetCFRunLoopCommonModes();
                    if (kCFRunLoopCommonModes != IntPtr.Zero)
                    {
                        CFRunLoopAddSource(_runLoop, _runLoopSource, kCFRunLoopCommonModes);
                        CFRunLoopRun();
                    }
                    else
                    {
                        Console.WriteLine("Error: Failed to get kCFRunLoopCommonModes");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error RunLoop: {ex}");
                }
            })
            {
                IsBackground = true,
                Name = "MacKeyboardHook Loop"
            };
            _runLoopThread.Start();
        }

        private IntPtr EventCallback(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo)
        {
            if (type == kCGEventKeyDown || type == kCGEventKeyUp)
            {
                long macKeyCode = CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
                var key = KeyboardKeyExtensions.FromPlatformCode((int)macKeyCode);;

                if (type == kCGEventKeyDown)
                    KeyDown?.Invoke(key);
                else
                    KeyUp?.Invoke(key);
            }
            return eventRef;
        }

        public void SendKey(KeyboardKey key)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MacKeyboardHook));

            IntPtr downEvent = IntPtr.Zero;
            IntPtr upEvent = IntPtr.Zero;

            try
            {
                downEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)key, true);
                upEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)key, false);

                if (downEvent != IntPtr.Zero)
                    CGEventPost(kCGAnnotatedSessionEventTap, downEvent);

                if (upEvent != IntPtr.Zero)
                    CGEventPost(kCGAnnotatedSessionEventTap, upEvent);
            }
            finally
            {
                if (downEvent != IntPtr.Zero)
                    CFRelease(downEvent);
                if (upEvent != IntPtr.Zero)
                    CFRelease(upEvent);
            }
        }

        public void SendKeyCombo(params KeyboardKey[] keys)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MacKeyboardHook));
            if (keys == null || keys.Length == 0) return;

            var events = new List<IntPtr>();

            try
            {
                // Key down in order
                foreach (var key in keys)
                {
                    var evt = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)key, true);
                    if (evt != IntPtr.Zero)
                    {
                        events.Add(evt);
                        CGEventPost(kCGAnnotatedSessionEventTap, evt);
                        Thread.Sleep(10);
                    }
                }
                
                for (int i = keys.Length - 1; i >= 0; i--)
                {
                    var evt = CGEventCreateKeyboardEvent(IntPtr.Zero, (ushort)keys[i], false);
                    if (evt != IntPtr.Zero)
                    {
                        events.Add(evt);
                        CGEventPost(kCGAnnotatedSessionEventTap, evt);
                        Thread.Sleep(10);
                    }
                }
            }
            finally
            {
                foreach (var evt in events)
                {
                    if (evt != IntPtr.Zero)
                        CFRelease(evt);
                }
            }
        }

        public KeyboardKey[] GetPressedKeys()
        {
            return Array.Empty<KeyboardKey>();
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_runLoop != IntPtr.Zero)
            {
                CFRunLoopStop(_runLoop);
            }

            if (_eventTap != IntPtr.Zero)
            {
                CGEventTapEnable(_eventTap, false);
                CFRelease(_eventTap);
            }

            if (_runLoopSource != IntPtr.Zero)
            {
                CFRelease(_runLoopSource);
            }
        }

        #region P/Invoke и получение констант
        
        private static IntPtr GetCFRunLoopCommonModes()
        {
            IntPtr handle = dlopen("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", 2); // RTLD_NOW
            if (handle == IntPtr.Zero) return IntPtr.Zero;

            try
            {
                IntPtr symbol = dlsym(handle, "kCFRunLoopCommonModes");
                if (symbol != IntPtr.Zero)
                {
                    return Marshal.ReadIntPtr(symbol);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return IntPtr.Zero;
        }

        [DllImport("libSystem.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("libSystem.dylib")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        private delegate IntPtr CGEventTapCallBack(IntPtr proxy, int type, IntPtr ev, IntPtr userInfo);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventTapCreate(int tap, int place, int options, ulong eventsOfInterest,
            CGEventTapCallBack callback, IntPtr userInfo);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CGEventTapEnable(IntPtr tap, bool enable);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern long CGEventGetIntegerValueField(IntPtr ev, int field);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr machPort, int order);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFRunLoopGetCurrent();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopRun();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopStop(IntPtr rl);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);
        
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, bool keyDown);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CGEventPost(int tap, IntPtr ev);

        #endregion
    }
}