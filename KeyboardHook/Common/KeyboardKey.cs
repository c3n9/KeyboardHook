using System;
using System.Reflection;
using KeyboardHook.Attributes;

namespace KeyboardHook.Enums
{
    public enum KeyboardKey
    {
        None,

        // Control keys
        [WindowsCode(0x08)]
        [LinuxCode(22)]
        Backspace,

        [WindowsCode(0x09)]
        [LinuxCode(23)]
        Tab,

        [WindowsCode(0x0D)]
        [LinuxCode(36)]
        Enter,

        [WindowsCode(0x13)]
        [LinuxCode(127)]
        Pause,

        [WindowsCode(0x14)]
        [LinuxCode(66)]
        CapsLock,

        [WindowsCode(0x1B)]
        [LinuxCode(9)]
        Escape,

        [WindowsCode(0x20)]
        [LinuxCode(65)]
        Space,

        // Navigation
        [WindowsCode(0x21)]
        [LinuxCode(112)]
        PageUp,

        [WindowsCode(0x22)]
        [LinuxCode(117)]
        PageDown,

        [WindowsCode(0x23)]
        [LinuxCode(115)]
        End,

        [WindowsCode(0x24)]
        [LinuxCode(110)]
        Home,

        [WindowsCode(0x25)]
        [LinuxCode(113)]
        Left,

        [WindowsCode(0x26)]
        [LinuxCode(111)]
        Up,

        [WindowsCode(0x27)]
        [LinuxCode(114)]
        Right,

        [WindowsCode(0x28)]
        [LinuxCode(116)]
        Down,

        [WindowsCode(0x2C)]
        [LinuxCode(107)]
        PrintScreen,

        [WindowsCode(0x2D)]
        [LinuxCode(118)]
        Insert,

        [WindowsCode(0x2E)]
        [LinuxCode(119)]
        Delete,

        // Digits
        [WindowsCode(0x30)]
        [LinuxCode(19)]
        D0,

        [WindowsCode(0x31)]
        [LinuxCode(10)]
        D1,

        [WindowsCode(0x32)]
        [LinuxCode(11)]
        D2,

        [WindowsCode(0x33)]
        [LinuxCode(12)]
        D3,

        [WindowsCode(0x34)]
        [LinuxCode(13)]
        D4,

        [WindowsCode(0x35)]
        [LinuxCode(14)]
        D5,

        [WindowsCode(0x36)]
        [LinuxCode(15)]
        D6,

        [WindowsCode(0x37)]
        [LinuxCode(16)]
        D7,

        [WindowsCode(0x38)]
        [LinuxCode(17)]
        D8,

        [WindowsCode(0x39)]
        [LinuxCode(18)]
        D9,

        // Letters
        [WindowsCode(0x41)]
        [LinuxCode(38)]
        A,

        [WindowsCode(0x42)]
        [LinuxCode(56)]
        B,

        [WindowsCode(0x43)]
        [LinuxCode(54)]
        C,

        [WindowsCode(0x44)]
        [LinuxCode(40)]
        D,

        [WindowsCode(0x45)]
        [LinuxCode(26)]
        E,

        [WindowsCode(0x46)]
        [LinuxCode(41)]
        F,

        [WindowsCode(0x47)]
        [LinuxCode(42)]
        G,

        [WindowsCode(0x48)]
        [LinuxCode(43)]
        H,

        [WindowsCode(0x49)]
        [LinuxCode(31)]
        I,

        [WindowsCode(0x4A)]
        [LinuxCode(44)]
        J,

        [WindowsCode(0x4B)]
        [LinuxCode(45)]
        K,

        [WindowsCode(0x4C)]
        [LinuxCode(46)]
        L,

        [WindowsCode(0x4D)]
        [LinuxCode(58)]
        M,

        [WindowsCode(0x4E)]
        [LinuxCode(57)]
        N,

        [WindowsCode(0x4F)]
        [LinuxCode(32)]
        O,

        [WindowsCode(0x50)]
        [LinuxCode(33)]
        P,

        [WindowsCode(0x51)]
        [LinuxCode(24)]
        Q,

        [WindowsCode(0x52)]
        [LinuxCode(27)]
        R,

        [WindowsCode(0x53)]
        [LinuxCode(39)]
        S,

        [WindowsCode(0x54)]
        [LinuxCode(28)]
        T,

        [WindowsCode(0x55)]
        [LinuxCode(30)]
        U,

        [WindowsCode(0x56)]
        [LinuxCode(55)]
        V,

        [WindowsCode(0x57)]
        [LinuxCode(25)]
        W,

        [WindowsCode(0x58)]
        [LinuxCode(53)]
        X,

        [WindowsCode(0x59)]
        [LinuxCode(29)]
        Y,

        [WindowsCode(0x5A)]
        [LinuxCode(52)]
        Z,

        // Windows keys
        [WindowsCode(0x5B)]
        [LinuxCode(133)]
        LWin,

        [WindowsCode(0x5C)]
        [LinuxCode(134)]
        RWin,

        [WindowsCode(0x5D)]
        [LinuxCode(135)]
        Apps,

        // Numeric keypad
        [WindowsCode(0x60)]
        [LinuxCode(90)]
        NumPad0,

        [WindowsCode(0x61)]
        [LinuxCode(87)]
        NumPad1,

        [WindowsCode(0x62)]
        [LinuxCode(88)]
        NumPad2,

        [WindowsCode(0x63)]
        [LinuxCode(89)]
        NumPad3,

        [WindowsCode(0x64)]
        [LinuxCode(83)]
        NumPad4,

        [WindowsCode(0x65)]
        [LinuxCode(84)]
        NumPad5,

        [WindowsCode(0x66)]
        [LinuxCode(85)]
        NumPad6,

        [WindowsCode(0x67)]
        [LinuxCode(79)]
        NumPad7,

        [WindowsCode(0x68)]
        [LinuxCode(80)]
        NumPad8,

        [WindowsCode(0x69)]
        [LinuxCode(81)]
        NumPad9,

        [WindowsCode(0x6A)]
        [LinuxCode(63)]
        Multiply,

        [WindowsCode(0x6B)]
        [LinuxCode(86)]
        Add,

        [WindowsCode(0x6C)]
        [LinuxCode(0)] // Separator - нет прямого аналога в Linux
        Separator,

        [WindowsCode(0x6D)]
        [LinuxCode(82)]
        Subtract,

        [WindowsCode(0x6E)]
        [LinuxCode(129)]
        Decimal,

        [WindowsCode(0x6F)]
        [LinuxCode(106)]
        Divide,

        // Function keys
        [WindowsCode(0x70)]
        [LinuxCode(67)]
        F1,

        [WindowsCode(0x71)]
        [LinuxCode(68)]
        F2,

        [WindowsCode(0x72)]
        [LinuxCode(69)]
        F3,

        [WindowsCode(0x73)]
        [LinuxCode(70)]
        F4,

        [WindowsCode(0x74)]
        [LinuxCode(71)]
        F5,

        [WindowsCode(0x75)]
        [LinuxCode(72)]
        F6,

        [WindowsCode(0x76)]
        [LinuxCode(73)]
        F7,

        [WindowsCode(0x77)]
        [LinuxCode(74)]
        F8,

        [WindowsCode(0x78)]
        [LinuxCode(75)]
        F9,

        [WindowsCode(0x79)]
        [LinuxCode(76)]
        F10,

        [WindowsCode(0x7A)]
        [LinuxCode(95)]
        F11,

        [WindowsCode(0x7B)]
        [LinuxCode(96)]
        F12,

        [WindowsCode(0x7C)]
        [LinuxCode(0)] // F13
        F13,

        [WindowsCode(0x7D)]
        [LinuxCode(0)] // F14
        F14,

        [WindowsCode(0x7E)]
        [LinuxCode(0)] // F15
        F15,

        [WindowsCode(0x7F)]
        [LinuxCode(0)] // F16
        F16,

        [WindowsCode(0x80)]
        [LinuxCode(0)] // F17
        F17,

        [WindowsCode(0x81)]
        [LinuxCode(0)] // F18
        F18,

        [WindowsCode(0x82)]
        [LinuxCode(0)] // F19
        F19,

        [WindowsCode(0x83)]
        [LinuxCode(0)] // F20
        F20,

        [WindowsCode(0x84)]
        [LinuxCode(0)] // F21
        F21,

        [WindowsCode(0x85)]
        [LinuxCode(0)] // F22
        F22,

        [WindowsCode(0x86)]
        [LinuxCode(0)] // F23
        F23,

        [WindowsCode(0x87)]
        [LinuxCode(0)] // F24
        F24,

        // Locks
        [WindowsCode(0x90)]
        [LinuxCode(77)]
        NumLock,

        [WindowsCode(0x91)]
        [LinuxCode(78)]
        ScrollLock,

        // Shift/Ctrl/Alt
        [WindowsCode(0xA0)]
        [LinuxCode(50)]
        LShift,

        [WindowsCode(0xA1)]
        [LinuxCode(62)]
        RShift,

        [WindowsCode(0xA2)]
        [LinuxCode(37)]
        LControl,

        [WindowsCode(0xA3)]
        [LinuxCode(105)]
        RControl,

        [WindowsCode(0xA4)]
        [LinuxCode(64)]
        LAlt,

        [WindowsCode(0xA5)]
        [LinuxCode(108)]
        RAlt,

        // Browser keys
        [WindowsCode(0xA6)]
        [LinuxCode(0)] // BrowserBack
        BrowserBack,

        [WindowsCode(0xA7)]
        [LinuxCode(0)] // BrowserForward
        BrowserForward,

        [WindowsCode(0xA8)]
        [LinuxCode(0)] // BrowserRefresh
        BrowserRefresh,

        [WindowsCode(0xA9)]
        [LinuxCode(0)] // BrowserStop
        BrowserStop,

        [WindowsCode(0xAA)]
        [LinuxCode(0)] // BrowserSearch
        BrowserSearch,

        [WindowsCode(0xAB)]
        [LinuxCode(0)] // BrowserFavorites
        BrowserFavorites,

        [WindowsCode(0xAC)]
        [LinuxCode(0)] // BrowserHome
        BrowserHome,

        // Volume / media
        [WindowsCode(0xAD)]
        [LinuxCode(0)] // VolumeMute
        VolumeMute,

        [WindowsCode(0xAE)]
        [LinuxCode(0)] // VolumeDown
        VolumeDown,

        [WindowsCode(0xAF)]
        [LinuxCode(0)] // VolumeUp
        VolumeUp,

        [WindowsCode(0xB0)]
        [LinuxCode(0)] // MediaNextTrack
        MediaNextTrack,

        [WindowsCode(0xB1)]
        [LinuxCode(0)] // MediaPreviousTrack
        MediaPreviousTrack,

        [WindowsCode(0xB2)]
        [LinuxCode(0)] // MediaStop
        MediaStop,

        [WindowsCode(0xB3)]
        [LinuxCode(0)] // MediaPlayPause
        MediaPlayPause,

        // Launch keys
        [WindowsCode(0xB4)]
        [LinuxCode(0)] // LaunchMail
        LaunchMail,

        [WindowsCode(0xB5)]
        [LinuxCode(0)] // LaunchMediaSelect
        LaunchMediaSelect,

        [WindowsCode(0xB6)]
        [LinuxCode(0)] // LaunchApp1
        LaunchApp1,

        [WindowsCode(0xB7)]
        [LinuxCode(0)] // LaunchApp2
        LaunchApp2,

        // OEM keys
        [WindowsCode(0xBA)]
        [LinuxCode(47)]
        OEM1,

        [WindowsCode(0xBB)]
        [LinuxCode(21)]
        OEMPlus,

        [WindowsCode(0xBC)]
        [LinuxCode(59)]
        OEMComma,

        [WindowsCode(0xBD)]
        [LinuxCode(20)]
        OEMMinus,

        [WindowsCode(0xBE)]
        [LinuxCode(60)]
        OEMPeriod,

        [WindowsCode(0xBF)]
        [LinuxCode(61)]
        OEM2,

        [WindowsCode(0xC0)]
        [LinuxCode(49)]
        OEM3,

        [WindowsCode(0xDB)]
        [LinuxCode(34)]
        OEM4,

        [WindowsCode(0xDC)]
        [LinuxCode(51)]
        OEM5,

        [WindowsCode(0xDD)]
        [LinuxCode(35)]
        OEM6,

        [WindowsCode(0xDE)]
        [LinuxCode(48)]
        OEM7,

        [WindowsCode(0xDF)]
        [LinuxCode(0)] // OEM8
        OEM8,

        [WindowsCode(0xE2)]
        [LinuxCode(0)] // OEM102
        OEM102,

        // System keys
        [WindowsCode(0xE5)]
        [LinuxCode(0)] // ProcessKey
        ProcessKey,

        [WindowsCode(0xE7)]
        [LinuxCode(0)] // Packet
        Packet,

        // Power management
        [WindowsCode(0x5F)]
        [LinuxCode(0)] // Sleep
        Sleep,

        [WindowsCode(0xE3)]
        [LinuxCode(0)] // Wake
        Wake,

        [WindowsCode(0)]
        [LinuxCode(135)]
        Menu,
    }
}