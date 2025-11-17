using CommunityToolkit.Mvvm.ComponentModel;
using KeyboardHook.Interfaces;

namespace KeyboardHook.AvaloniaExample.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _keyDownStr;

        [ObservableProperty]
        private string _keyUpStr;

        public MainWindowViewModel()
        {
            IKeyboardHook keyboardHook = KeyboardHookFactory.Create();

            keyboardHook.KeyDown += KeyboardHook_KeyDown;
            keyboardHook.KeyUp += KeyboardHook_KeyUp;
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
