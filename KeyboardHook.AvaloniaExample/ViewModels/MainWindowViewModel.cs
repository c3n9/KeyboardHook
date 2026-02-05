using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyboardHook.Enums;
using KeyboardHook.Interfaces;

namespace KeyboardHook.AvaloniaExample.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IKeyboardHook _keyboardHook;
        //private readonly IMouseHook _mouseHook;

        [ObservableProperty]
        private string _keyDownStr;

        [ObservableProperty]
        private string _keyUpStr;

        [RelayCommand]
        private void SystemButtonSend()
        {
            _keyboardHook.SendKey(KeyboardKey.B);
        }

        [RelayCommand]
        private void LeftMouseButtonSend()
        {
            //_mouseHook.SendButton(MouseButton.Left);
        }

        public MainWindowViewModel()
        {
            _keyboardHook = KeyboardHookFactory.Create();
            //_mouseHook = MouseHookFactory.Create();

            //_mouseHook.ButtonUp += _mouseHook_ButtonUp;
            //_mouseHook.ButtonDown += _mouseHook_ButtonDown;

            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            _keyboardHook.KeyUp += KeyboardHook_KeyUp;
        }

        private void _mouseHook_ButtonDown(MouseButton btn)
        {
            KeyDownStr = btn.ToString();
        }

        private void _mouseHook_ButtonUp(Enums.MouseButton btn)
        {
            KeyUpStr = btn.ToString();
        }

        private void KeyboardHook_KeyUp(Enums.KeyboardKey key)
        {
            KeyUpStr = key.ToString();
        }

        private void KeyboardHook_KeyDown(Enums.KeyboardKey key)
        {
            KeyDownStr = key.ToString();
        }
    }
}
