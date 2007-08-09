//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Windows.Forms;
//using System.Runtime.InteropServices;

//namespace VisualObject
//{
//    public static class MainLoop
//    {
//        private static bool enabled = true;

//        public static bool Enabled
//        {
//            get { return MainLoop.enabled; }
//            set { MainLoop.enabled = value; }
//        }

//        static MainLoop()
//        {
//            Application.Idle += new EventHandler(OnIdle);
//        }

//        public static event EventHandler Go;


//        private static void OnIdle(object sender, EventArgs e)
//        {


//            bool idle;
//            do
//            {
//                if (!enabled) return;
//                Go(null, null);
//                idle = AppStillIdle;
//            } while (enabled && idle);
//        }



//        private static bool AppStillIdle
//        {
//            get
//            {
//                MainLoop.PeekMsg msg;
//                return !MainLoop.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
//            }
//        }

//        [StructLayout(LayoutKind.Sequential)]
//        private struct PeekMsg
//        {
//            public IntPtr hWnd;
//            public Message msg;
//            public IntPtr wParam;
//            public IntPtr lParam;
//            public uint time;
//            public System.Drawing.Point p;
//        }

//        [System.Security.SuppressUnmanagedCodeSecurity]
//        [DllImport("User32.dll", CharSet = CharSet.Auto)]
//        private static extern bool PeekMessage(out PeekMsg msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);
//    }




//}
