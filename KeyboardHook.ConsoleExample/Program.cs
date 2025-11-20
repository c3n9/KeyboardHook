using System;
using System.Threading;
using KeyboardHook;
using KeyboardHook.Enums;

// Example program demonstrating how to use the KeyboardHook library.
// Comments are written in English.

// This example:
// - Creates a keyboard hook via the factory
// - Subscribes to global KeyDown and KeyUp events
// - Demonstrates sending a single key and a key combination programmatically
// - Reads console input to allow the user to exit (press Escape)
// - Prints the currently pressed keys (as tracked by the hook)

class Program
{
    static void Main()
    {
        // Create the hook using the factory. The concrete implementation is chosen by the runtime OS.
        var hook = KeyboardHookFactory.Create();

        // Subscribe to global key events.
        hook.KeyDown += OnKeyDown;
        hook.KeyUp += OnKeyUp;

        Console.WriteLine("Global keyboard hook started. Press Escape in this console to exit.");

        // Demonstrate sending a single key programmatically.
        Console.WriteLine("Sending the 'A' key in 2 seconds...");
        Thread.Sleep(2000);
        hook.SendKey(KeyboardKey.A);

        // Demonstrate sending a key combination (Ctrl+L).
        Console.WriteLine("Sending Ctrl+L combination in 1 second...");
        Thread.Sleep(1000);
        hook.SendKeyCombo(KeyboardKey.LControl, KeyboardKey.L);

        // Unsubscribe and dispose the hook if the underlying implementation supports disposal.
        hook.KeyDown -= OnKeyDown;
        hook.KeyUp -= OnKeyUp;

        if (hook is IDisposable d)
            d.Dispose();

        Console.WriteLine("Example finished. Hook disposed.");
    }

    // Event handler invoked when any global key is pressed.
    private static void OnKeyDown(KeyboardKey key)
    {
        Console.WriteLine($"KeyDown: {key} (VK={(int)key})");
    }

    // Event handler invoked when any global key is released.
    private static void OnKeyUp(KeyboardKey key)
    {
        Console.WriteLine($"KeyUp:   {key} (VK={(int)key})");
    }
}
