#if !NETFRAMEWORK
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CaeGlobals
{
    // This runs in a child process, not in the UI or CLI parent. Calling prctl in the
    // parent would protect the wrong process. The guardian owns a separate process group.
    public static class LinuxProcessHost
    {
        [DllImport("libc", SetLastError = true)] private static extern int prctl(int option, ulong arg2, ulong arg3, ulong arg4, ulong arg5);
        [DllImport("libc")] private static extern int getppid();
        [DllImport("libc", SetLastError = true)] private static extern int kill(int pid, int signal);

        public static int Run(string[] args)
        {
            if (!OperatingSystem.IsLinux() || args.Length < 3) throw new ArgumentException("Invalid Linux process-host invocation.");
            int expectedParent = int.Parse(args[0]);
            Process child = null;
            var gate = new object();
            void Terminate()
            {
                lock (gate)
                {
                    if (child == null) return;
                    // Kill the tree first, while its parent still exists and can be enumerated.
                    try { child.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                    kill(-child.Id, 9); // also reap surviving members of the solver's session/group
                }
            }
            using var termination = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                Terminate();
                Environment.Exit(143);
            });
            if (prctl(1, 15, 0, 0, 0) != 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            if (getppid() != expectedParent) return 143; // parent died before prctl was armed
            var info = new ProcessStartInfo("/usr/bin/setsid") { UseShellExecute = false, CreateNoWindow = true };
            if (args.Length > 3)
            {
                info.ArgumentList.Add(args[1]);
                for (int i = 3; i < args.Length; i++) info.ArgumentList.Add(args[i]);
            }
            else info.Arguments = Quote(args[1]) + " " + args[2];
            lock (gate)
            {
                // Recheck while holding the same lock used by the signal handler.
                if (getppid() != expectedParent) return 143;
                child = Process.Start(info) ?? throw new InvalidOperationException("Could not start Linux solver.");
            }
            child.WaitForExit();
            int exitCode = child.ExitCode;
            Terminate();
            child.Dispose();
            return exitCode;
        }
        private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
#endif
