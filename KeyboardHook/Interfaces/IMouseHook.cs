using KeyboardHook.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KeyboardHook.Interfaces
{
    public interface IMouseHook : IDisposable
    {
        event Action<MouseButton> ButtonDown;

        event Action<MouseButton> ButtonUp;

        void SendButton(MouseButton button);
        void SendButtonCombo(params MouseButton[] buttons);
    }
}
