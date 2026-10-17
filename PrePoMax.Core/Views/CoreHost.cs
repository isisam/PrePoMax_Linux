using System;
using System.Drawing;

namespace PrePoMax
{
    public static class CoreHost
    {
        public static Action<Exception> FileReadError { get; set; }

        public static double AvailableMemoryMB()
        {
#if NETFRAMEWORK
  using(var counter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes")) return counter.NextValue();
#else
            if (System.IO.File.Exists("/proc/meminfo"))
                foreach (var line in System.IO.File.ReadLines("/proc/meminfo"))
                {
                    if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                        return double.Parse(line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[1], System.Globalization.CultureInfo.InvariantCulture) / 1024;
                }

            return Math.Max(1, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes - GC.GetTotalMemory(false)) / 1048576.0;
#endif
        }

        public static Func<bool> IsMessageLoopThread { get; set; } = () => false;
    }

    [Serializable]
    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    public interface IWindowGeometry
    {
        WindowState WindowState { get; set; }

        Rectangle RestoreBounds { get; }

        Rectangle ScreenBounds { get; }

        System.Drawing.Size MinimumSize { get; }

        System.Drawing.Size Size { get; }

        Point Location { get; }

        void SetBounds(int x, int y, int width, int height);
    }
}
