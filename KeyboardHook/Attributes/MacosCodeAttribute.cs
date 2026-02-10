using System;

namespace KeyboardHook.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MacosCodeAttribute : System.Attribute
    {
        public int Code { get; }
        public MacosCodeAttribute(int code) => Code = code;
    }
}