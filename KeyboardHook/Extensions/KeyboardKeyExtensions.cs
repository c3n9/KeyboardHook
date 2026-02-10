using KeyboardHook.Attributes;
using KeyboardHook.Enums;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace KeyboardHook.Extensions
{
    internal static class KeyboardKeyExtensions
    {
        internal static int ToPlatformCode(this KeyboardKey key)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsCode(key);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetLinuxCode(key);
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacOsCode(key);
            else
                return 0;
        }

        internal static KeyboardKey FromPlatformCode(int platformCode)
        {
            var fields = typeof(KeyboardKey).GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(KeyboardKey)) continue;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var attr = field.GetCustomAttribute<WindowsCodeAttribute>();
                    if (attr?.Code == platformCode)
                        return (KeyboardKey)field.GetValue(null);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var attr = field.GetCustomAttribute<LinuxCodeAttribute>();
                    if (attr?.Code == platformCode)
                        return (KeyboardKey)field.GetValue(null);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var attr = field.GetCustomAttribute<MacosCodeAttribute>();
                    if (attr?.Code == platformCode)
                        return (KeyboardKey)field.GetValue(null);
                }
            }
            return KeyboardKey.None;
        }

        private static int GetWindowsCode(KeyboardKey key)
        {
            var field = typeof(KeyboardKey).GetField(key.ToString());
            var attr = field?.GetCustomAttribute<WindowsCodeAttribute>();
            return attr?.Code ?? 0;
        }

        private static int GetLinuxCode(KeyboardKey key)
        {
            var field = typeof(KeyboardKey).GetField(key.ToString());
            var attr = field?.GetCustomAttribute<LinuxCodeAttribute>();
            return attr?.Code ?? 0;
        }
        private static int GetMacOsCode(KeyboardKey key)
        {
            var field = typeof(KeyboardKey).GetField(key.ToString());
            var attr = field?.GetCustomAttribute<MacosCodeAttribute>();
            return attr?.Code ?? 0;
        }
    }
}