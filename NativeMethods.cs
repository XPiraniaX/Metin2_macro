using System;
using System.Runtime.InteropServices;

namespace Metin2Macro;

internal static class NativeMethods
{
    public const int WH_KEYBOARD_LL = 13;

    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;

    public const ushort VK_R = 0x52;
    public const byte VK_F3 = 0x72;
    public const byte VK_F4 = 0x73;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    public delegate IntPtr LowLevelKeyboardProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(
        IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(
        string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern void keybd_event(
        byte bVk,
        byte bScan,
        uint dwFlags,
        UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(
        uint uCode,
        uint uMapType);

    public static void TapKey(byte virtualKey)
    {
        byte scanCode = (byte)MapVirtualKey(virtualKey, 0);

        // Key down
        keybd_event(
            virtualKey,
            scanCode,
            0,
            UIntPtr.Zero);

        // Key up
        keybd_event(
            virtualKey,
            scanCode,
            KEYEVENTF_KEYUP,
            UIntPtr.Zero);
    }
}