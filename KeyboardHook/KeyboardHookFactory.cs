using KeyboardHook.Interfaces;
using KeyboardHook.Implementation;
using System;
using System.Runtime.InteropServices;

namespace KeyboardHook
{
    public static class KeyboardHookFactory
    {
        public static IKeyboardHook Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Implementation.WindowsKeyboardHook();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new Implementation.LinuxKeyboardHook();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new Implementation.MacKeyboardHook();
            }

            throw new PlatformNotSupportedException("Unsupported platform");
        }
    }
}
