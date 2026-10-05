using KeyboardHook.Attributes;
using KeyboardHook.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace KeyboardHook.Extensions
{
    internal static class KeyboardKeyExtensions
    {
        // Reflection over the enum is far too slow for a per-keystroke path,
        // so both directions are built once for the current platform.
        private static readonly Dictionary<KeyboardKey, int> _toPlatform = new Dictionary<KeyboardKey, int>();
        private static readonly Dictionary<int, KeyboardKey> _fromPlatform = new Dictionary<int, KeyboardKey>();

        static KeyboardKeyExtensions()
        {
            foreach (var field in typeof(KeyboardKey).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                int? code = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    code = field.GetCustomAttribute<WindowsCodeAttribute>()?.Code;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    code = field.GetCustomAttribute<LinuxCodeAttribute>()?.Code;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    code = field.GetCustomAttribute<MacosCodeAttribute>()?.Code;

                if (code == null) continue;

                var key = (KeyboardKey)field.GetValue(null);
                if (!_toPlatform.ContainsKey(key))
                    _toPlatform[key] = code.Value;
                // first declared key wins, same as the previous linear search
                if (!_fromPlatform.ContainsKey(code.Value))
                    _fromPlatform[code.Value] = key;
            }
        }

        internal static int ToPlatformCode(this KeyboardKey key)
        {
            return _toPlatform.TryGetValue(key, out var code) ? code : 0;
        }

        internal static KeyboardKey FromPlatformCode(int platformCode)
        {
            return _fromPlatform.TryGetValue(platformCode, out var key) ? key : KeyboardKey.None;
        }
    }
}
