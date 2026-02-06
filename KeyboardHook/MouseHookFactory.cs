using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardHook
{
    public static class MouseHookFactory
    {
        public static IMouseHook Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Implementation.MouseImplementation.WindowsMouseHook();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new Implementation.MouseImplementation.LinuxMouseHook();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new Implementation.MouseImplementation.MacMouseHook();
            }

            throw new PlatformNotSupportedException("Unsupported platform");
        }
    }
}
