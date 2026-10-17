using System;
using System.Diagnostics;

namespace CaeGlobals
{
    public interface IMessageSink
    {
        void Warning(string message);
        void Error(string message);
    }
    public interface IPlatformPaths { string ApplicationDirectory { get; } }
    public interface IProcessTreeService { void Kill(int processId); }

    // Hosts may replace these services. The net48 defaults retain the existing UI prompt policy.
    public static class PlatformServices
    {
        public static IMessageSink Messages { get; set; } = new DefaultMessageSink();
        public static IPlatformPaths Paths { get; set; } = new DefaultPaths();
        public static IProcessTreeService Processes { get; set; } = new DefaultProcessTreeService();
        private sealed class DefaultMessageSink : IMessageSink
        {
            public void Warning(string message)
            {
#if NETFRAMEWORK
                MessageBoxes.ShowWarning(message);
#else
                Console.Error.WriteLine("Warning: " + message);
#endif
            }
            public void Error(string message)
            {
#if NETFRAMEWORK
                MessageBoxes.ShowError(message);
#else
                Console.Error.WriteLine("Error: " + message);
#endif
            }
        }
        private sealed class DefaultPaths : IPlatformPaths
        {
            public string ApplicationDirectory =>
#if NETFRAMEWORK
                System.Windows.Forms.Application.StartupPath;
#else
                AppContext.BaseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar);
#endif
        }
        private sealed class DefaultProcessTreeService : IProcessTreeService
        {
            public void Kill(int processId)
            {
#if NETFRAMEWORK
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT ProcessId FROM Win32_Process WHERE ParentProcessId=" + processId))
                using (var children = searcher.Get())
                    foreach (System.Management.ManagementObject child in children)
                        Kill(Convert.ToInt32(child["ProcessId"]));
                using (var process = Process.GetProcessById(processId)) process.Kill();
#else
                using (var process = Process.GetProcessById(processId)) process.Kill(entireProcessTree: true);
#endif
            }
        }
    }
    public static class PlatformInfo
    {
#if !NETFRAMEWORK
        [System.Runtime.Versioning.SupportedOSPlatformGuard("windows")]
#endif
        public static bool IsWindows =>
#if NETFRAMEWORK
            true;
#else
            OperatingSystem.IsWindows();
#endif
    }
}
