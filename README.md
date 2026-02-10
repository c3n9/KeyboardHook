# KeyboardHook
[![NuGet](https://img.shields.io/nuget/v/KeyboardHook.svg)](https://www.nuget.org/packages/KeyboardHook/)

KeyboardHook provides a cross-platform global hook for keyboard, mouse and event simulation.

## Supported Platforms

|               | Windows | macOS | Linux (only X11) |
|---------------|:-------:|:-----:|:----------------:|
| **x86**       | Yes     | N/A   | N/A              |
| **x64**       | Yes     | N/A   | Yes              |
| **Arm32**     | N/A     | N/A   | N/A              |
| **Arm64**     | N/A     | Yes   | N/A              |

## Global Hooks

KeyboardHook provides the IKeyboardHook interface. Here's a basic usage example:

```csharp 

    IKeyboardHook keyboardHook = KeyboardHookFactory.Create();

    keyboardHook.KeyDown += KeyboardHook_KeyDown;
    keyboardHook.KeyUp += KeyboardHook_KeyUp;

    .........................................................

    private void KeyboardHook_KeyUp(Enums.KeyboardKey key)
    {
        KeyUpStr = key.ToString();
    }

    private void KeyboardHook_KeyDown(Enums.KeyboardKey key)
    {
        KeyDownStr = key.ToString();
    }
    
```

KeyboardHook provides the IMouseHook interface. Here's a basic usage example

```csharp 

    IMouseHook _mouseHook = MouseHookFactory.Create();

    _mouseHook.ButtonUp += _mouseHook_ButtonUp;
    _mouseHook.ButtonDown += _mouseHook_ButtonDown;

    .........................................................

    private void _mouseHook_ButtonDown(MouseButton btn)
    {
        MouseButtonDownStr = btn.ToString();
    }

    private void _mouseHook_ButtonUp(Enums.MouseButton btn)
    {
        MouseButtonUpStr = btn.ToString();
    }

```

## Emulate keystrokes and mouse buttons

You can emulate keystrokes using SendKey and SendKeyCombo. Here is a basic example of using keyboard emulation:

```csharp 
    
    IKeyboardHook hook = KeyboardHookFactory.Create();

    // Demonstrate sending a single key programmatically.
    hook.SendKey(KeyboardKey.A);

    // Demonstrate sending a key combination (Ctrl+C).
    hook.SendKeyCombo(KeyboardKey.LControl, KeyboardKey.L);
    
```

Here is a basic example of using mouse button emulation:

```csharp 
    
    IMouseHook hook = KeyboardHookFactory.Create();

    // Demonstrate sending a single mouse button programmatically.
    hook.SendButton(MouseButton.Left);

    // Demonstrate sending a double left click programmatically.
    hook.SendButton(MouseButton.Left, MouseButton.Left);
    
```

The library is experimental, if you suddenly have any questions or problems, write an issue or make a fork.
