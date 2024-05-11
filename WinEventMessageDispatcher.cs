using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dtwo.API;

namespace Dtwo.Plugins.MultiAccount
{
    public class WinEventMessageDispatcher
    {
        [DllImport(@"user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool GetMessage(ref MSG message, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport(@"user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool TranslateMessage(ref MSG message);
        [DllImport(@"user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long DispatchMessage(ref MSG message);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            long x;
            long y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            IntPtr hwnd;
            public uint message;
            UIntPtr wParam;
            IntPtr lParam;
            uint time;
            POINT pt;
        }

        private bool m_isStarted;
        private bool m_needStop;

        public void Start(Action threadCallbacks, Action stopCallbacks)
        {
            if (m_isStarted)
            {
                LogManager.LogWarning(
                            $"{nameof(WinEventMessageDispatcher)}.{nameof(Start)}", 
                            "WinEventMessageDispatcher already started", 1);
                return;
            }

            m_isStarted = true;

            Update(threadCallbacks, stopCallbacks);
        }

        public void Stop()
        {
            m_needStop = true;
        }

        private void Update(Action threadCallbacks, Action stopCallbacks)
        {
            MSG msg = new MSG();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    threadCallbacks?.Invoke();

                    while (m_isStarted && GetMessage(ref msg, IntPtr.Zero, 0, 0))
                    {
                        if (m_needStop)
                        {
                            stopCallbacks.Invoke();
                            m_isStarted = false;
                            return;
                        }
                        //if (check_any_message_you_need == msg.message)
                        // you can for example check if Enter has been pressed to emulate Console.Readline();
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex.ToString(), 1);
                }
            }, TaskCreationOptions.LongRunning);

        }
    }
}
