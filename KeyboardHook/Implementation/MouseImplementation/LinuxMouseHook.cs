using KeyboardHook.Enums;
using KeyboardHook.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KeyboardHook.Implementation.MouseImplementation
{
    internal class LinuxMouseHook : IMouseHook, IDisposable
    {
        public event Action<MouseButton> ButtonDown;
        public event Action<MouseButton> ButtonUp;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SendButton(MouseButton button)
        {
            throw new NotImplementedException();
        }

        public void SendButtonCombo(params MouseButton[] buttons)
        {
            throw new NotImplementedException();
        }
    }
}
