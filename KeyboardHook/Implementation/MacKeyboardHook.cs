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
        private IntPtr _eventTap;
        private IntPtr _runLoopSource;
        private IntPtr _runLoop;
        private bool _disposed = false;

        private const int kCGEventKeyDown = 10;
        private const int kCGEventKeyUp = 11;
        private const int kCGKeyboardEventKeycode = 9;

        // Event tap locations
        private const int kCGHIDEventTap = 0;
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
            // Try to create event tap first, it might work without explicit permissions
            if (!TryCreateEventTap())
            {
                // If failed, prompt user for permissions
                Console.WriteLine("Accessibility permissions are required for keyboard monitoring.");
                Console.WriteLine("Please grant access in: System Preferences > Security & Privacy > Privacy > Accessibility");
                Console.WriteLine("Then restart the application.");
                
                throw new InvalidOperationException(
                    "Accessibility permissions required. " +
                    "Please add this app to Accessibility in System Preferences and restart."
                );
            }

            StartRunLoop();
        }

        private bool TryCreateEventTap()
        {
            ulong eventMask = (1UL << kCGEventKeyDown) | (1UL << kCGEventKeyUp);
            
            // Try different tap locations
            int[] tapLocations = { kCGSessionEventTap, kCGHIDEventTap, kCGAnnotatedSessionEventTap };
            
            foreach (var tapLocation in tapLocations)
            {
                _eventTap = CGEventTapCreate(
                    tapLocation,
                    1,              // headInsert - listen to events
                    1,              // filter events
                    eventMask,
                    EventCallback,
                    IntPtr.Zero
                );

                if (_eventTap != IntPtr.Zero)
                {
                    Console.WriteLine($"Event tap created successfully at location: {tapLocation}");
                    break;
                }
            }

            if (_eventTap == IntPtr.Zero)
            {
                return false;
            }

            // Create run loop source
            _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
            if (_runLoopSource == IntPtr.Zero)
            {
                CFRelease(_eventTap);
                _eventTap = IntPtr.Zero;
                return false;
            }

            // Enable the event tap
            CGEventTapEnable(_eventTap, true);
            return true;
        }

        private void StartRunLoop()
        {
            _runLoopThread = new Thread(RunLoopThreadProc)
            {
                IsBackground = true,
                Name = "MacKeyboardHook RunLoop",
                Priority = ThreadPriority.AboveNormal
            };
            _runLoopThread.Start();
        }

        private void RunLoopThreadProc()
        {
            try
            {
                _runLoop = CFRunLoopGetCurrent();
                
                // Add source to run loop
                CFRunLoopAddSource(_runLoop, _runLoopSource, kCFRunLoopCommonModes);
                
                Console.WriteLine("Keyboard hook run loop started");
                CFRunLoopRun();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Run loop error: {ex.Message}");
            }
        }

        private IntPtr EventCallback(IntPtr proxy, int type, IntPtr eventRef, IntPtr userInfo)
        {
            try
            {
                if (type == kCGEventKeyDown || type == kCGEventKeyUp)
                {
                    var keyCode = CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
                    var key = (KeyboardKey)keyCode;

                    if (type == kCGEventKeyDown)
                    {
                        KeyDown?.Invoke(key);
                    }
                    else
                    {
                        KeyUp?.Invoke(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in event callback: {ex.Message}");
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

                // Key up in reverse order
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

            try
            {
                Console.WriteLine("Disposing keyboard hook...");

                // Stop the run loop
                if (_runLoop != IntPtr.Zero)
                {
                    CFRunLoopStop(_runLoop);
                }

                // Disable and invalidate event tap
                if (_eventTap != IntPtr.Zero)
                {
                    CGEventTapEnable(_eventTap, false);
                    CFMachPortInvalidate(_eventTap);
                    CFRelease(_eventTap);
                }

                // Clean up run loop source
                if (_runLoopSource != IntPtr.Zero)
                {
                    CFRelease(_runLoopSource);
                }

                // Wait for thread to finish
                if (_runLoopThread != null && _runLoopThread.IsAlive)
                {
                    if (!_runLoopThread.Join(1000))
                    {
                        Console.WriteLine("Run loop thread did not exit gracefully");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disposal: {ex.Message}");
            }
            finally
            {
                _eventTap = IntPtr.Zero;
                _runLoopSource = IntPtr.Zero;
                _runLoop = IntPtr.Zero;
                _runLoopThread = null;
                _disposed = true;
            }
        }

        #region PInvoke Declarations

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

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopRun();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRunLoopStop(IntPtr rl);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

        // Run loop modes - use the correct constant
        private static readonly IntPtr kCFRunLoopCommonModes = GetCFRunLoopCommonModes();

        private static IntPtr GetCFRunLoopCommonModes()
        {
            // Try to get the actual constant value
            try
            {
                return CFStringCreateWithCString(IntPtr.Zero, "kCFRunLoopCommonModes", 0x0600);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string str, uint encoding);

        #endregion
    }
}