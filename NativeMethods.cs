using System;
using System.Runtime.InteropServices;

namespace Metin2Macro;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int MOD_NOREPEAT = 0x4000;

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public const ushort VK_F3 = 0x72;
    public const ushort VK_F4 = 0x73;
    public const ushort VK_R  = 0x52;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void TapKey(ushort vk)
    {
        var down = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk
                }
            }
        };

        var up = down;
        up.U.ki.dwFlags = KEYEVENTF_KEYUP;

        var size = Marshal.SizeOf<INPUT>();
        SendInput(1, new[] { down }, size);
        SendInput(1, new[] { up }, size);
    }
}
