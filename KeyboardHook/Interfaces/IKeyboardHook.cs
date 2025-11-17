using System;
using System.Collections.Generic;
using System.Text;
using KeyboardHook.Enums;

namespace KeyboardHook.Interfaces
{
    public interface IKeyboardHook
    {
        event Action<KeyboardKey> KeyDown;
        event Action<KeyboardKey> KeyUp;

        void SendKey(KeyboardKey key);
        void SendKeyCombo(params KeyboardKey[] keys);
    }
}
