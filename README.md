# KeyboardHook
[![NuGet](https://img.shields.io/nuget/v/KeyboardHook.svg)](https://www.nuget.org/packages/KeyboardHook/)

KeyboardHook provides a cross-platform global keyboard and event simulation.

## Supported Platforms

|               | Windows | macOS | Linux (only X11) |
|---------------|:-------:|:-----:|:----------------:|
| **x86**       | Yes     | N/A   | N/A              |
| **x64**       | Yes     | N/A   | Yes              |
| **Arm32**     | N/A     | N/A   | N/A              |
| **Arm64**     | N/A     | N/A   | N/A              |

## Global Hooks

KeyboardHook provides the IKeyboardHook interface. Here's a basic usage example:

```

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
