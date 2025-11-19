using System;

namespace KeyboardHook.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class WindowsCodeAttribute : System.Attribute
{
    public int Code { get; }
    public WindowsCodeAttribute(int code) => Code = code;
}