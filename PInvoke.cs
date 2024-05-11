using System.Diagnostics;
using System.Runtime.InteropServices;
using static Dtwo.Plugins.InputEvents.PInvoke;

namespace Dtwo.Plugins.MultiAccount
{
	public class PInvoke
	{
        public static void ActivateWindow(IntPtr mainWindowHandle)
        {
            //check if already has focus
            if (mainWindowHandle == GetForegroundWindow()) return;

            //check if window is minimized
            if (IsIconic(mainWindowHandle))
            {
                ShowWindow(mainWindowHandle, Restore);
            }

            // Simulate a key press
            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | 0, 0);

            //SetForegroundWindow(mainWindowHandle);

            // Simulate a key release
            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | KEYUP, 0);

            SetForegroundWindow(mainWindowHandle);
        }

        public const int ALT = 0xA4;
        public const int EXTENDEDKEY = 0x1;
        public const int KEYUP = 0x2;
        public const uint Restore = 9;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ShowWindow(IntPtr hWnd, uint Msg);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs(UnmanagedType.U4)] int cPoints);

        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref POINT pt, [MarshalAs(UnmanagedType.U4)] int cPoints);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, string lParam);

        public static void FocusProcess(Process process)
        {
            Task.Factory.StartNew(() =>
            {
                IntPtr hWnd;

                hWnd = process.MainWindowHandle;
                SetForegroundWindow(hWnd); //set to topmost
                ActivateWindow(hWnd);
                ShowWindow(hWnd, 3); //set fullscreen
            });
        }
    }
}
