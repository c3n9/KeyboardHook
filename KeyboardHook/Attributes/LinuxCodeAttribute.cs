using System;

namespace KeyboardHook.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LinuxCodeAttribute : System.Attribute
    {
        public int Code { get; }
        public LinuxCodeAttribute(int code) => Code = code;
    }
}