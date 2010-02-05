using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Signum.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public Rect ToRect()
        {
            return new Rect(left, top, right - left, bottom - top);
        }

        public static RECT FromRect(Rect rect)
        {
            return new RECT
            {
                left = (int)rect.Left,
                top = (int)rect.Top,
                right = (int)rect.Right,
                bottom = (int)rect.Bottom
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;

        public static POINT FromPoint(Point point)
        {
            return new POINT { x = (int)point.X, y = (int)point.Y };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public uint size;
        public RECT monitor;
        public RECT work;
        public uint flags;
    }

    public class MonitorInfo
    {
        public IntPtr Handler { get; internal set; }
        public bool IsPrimary { get; internal set; }
        public Rect DisplayArea { get; internal set; }
        public Rect WorkingArea { get; internal set; }

        public override string ToString()
        {
            return string.Format("Monitor = {0}{1} Display = {2} Working Area = {3}", Handler, IsPrimary ? "(Primary)" : "", DisplayArea, WorkingArea);
        }
    }


    public static class Monitors
    {
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hmon, ref MONITORINFO mi);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT point, NotFoundOptions notFoundOptions);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromRect(RECT point, NotFoundOptions notFoundOptions);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hWind, NotFoundOptions notFoundOptions);


        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        public static List<MonitorInfo> GetMonitors()
        {
            List<MonitorInfo> result = new List<MonitorInfo>();

            MonitorEnumDelegate med = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var minfo = GetMonitorInfo(hMonitor);
                result.Add(minfo);
                return true;
            };

            Monitors.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, med, IntPtr.Zero);

            return result;
        }

        public static MonitorInfo GetMonitorFromPoint(Point point, NotFoundOptions notFoundOptions)
        {
            IntPtr hMonitor = MonitorFromPoint(POINT.FromPoint(point), notFoundOptions);
            return GetMonitorInfo(hMonitor);
        }

        public static MonitorInfo GetMonitorFromRect(Rect rect, NotFoundOptions notFoundOptions)
        {
            IntPtr hMonitor = MonitorFromRect(RECT.FromRect(rect), notFoundOptions);
            return GetMonitorInfo(hMonitor);
        }

        public static MonitorInfo GetMonitorFromWindow(Window windows, NotFoundOptions notFoundOptions)
        {
            IntPtr hMonitor = MonitorFromWindow(new WindowInteropHelper(windows).Handle, notFoundOptions);
            return GetMonitorInfo(hMonitor);
        }

        static MonitorInfo GetMonitorInfo(IntPtr hMonitor)
        {
            MONITORINFO mi = new MONITORINFO();
            mi.size = (uint)Marshal.SizeOf(mi);
            if (!Monitors.GetMonitorInfo(hMonitor, ref mi))
                return null;

            return new MonitorInfo
            {
                Handler = hMonitor,
                IsPrimary = mi.flags != 0,
                DisplayArea = mi.monitor.ToRect(),
                WorkingArea = mi.work.ToRect(),
            };
        }
    }

    public enum NotFoundOptions
    {
        DefaultToNull = 0,
        DefaultToPrimary = 1,
        DefaultToNearest = 2
    }
}
