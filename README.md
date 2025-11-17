# KeyboardHook
[![NuGet](https://img.shields.io/nuget/v/KeyboardHook.svg)](https://www.nuget.org/packages/KeyboardHook/)

KeyboardHook provides a cross-platform global keyboard and event simulation.

## Supported Platforms

<table>
  <tr>
    <th></th>
    <th>Windows</th>
    <th>macOS</th>
    <th>Linux</th>
  </tr>
  <tr>
    <th>x86</th>
    <td>Yes</td>
    <td>N/A</td>
    <td>No</td>
  </tr>
  <tr>
    <th>x64</th>
    <td>Yes</td>
    <td>N/A</td>
    <td>Yes</td>
  </tr>
  <tr>
    <th>Arm32</th>
    <td>N/A</td>
    <td>N/A</td>
    <td>Yes</td>
  </tr>
  <tr>
    <th>Arm64</th>
    <td>Yes</td>
    <td>N/A</td>
    <td>Yes</td>
  </tr>
</table>

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
