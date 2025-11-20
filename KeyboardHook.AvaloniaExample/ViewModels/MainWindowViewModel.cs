using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyboardHook.Enums;
using KeyboardHook.Interfaces;

namespace KeyboardHook.AvaloniaExample.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IKeyboardHook _keyboardHook;

        [ObservableProperty]
        private string _keyDownStr;

        [ObservableProperty]
        private string _keyUpStr;

        [RelayCommand]
        private void SystemButtonSend()
        {
            _keyboardHook.SendKey(KeyboardKey.LWin);
        }

        public MainWindowViewModel()
        {
            _keyboardHook = KeyboardHookFactory.Create();

            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            _keyboardHook.KeyUp += KeyboardHook_KeyUp;
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
