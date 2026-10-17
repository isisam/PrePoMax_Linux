using System.Runtime.InteropServices;
using Avalonia.Threading;
namespace PrePoMax.Avalonia;
// Block on the X socket and an eventfd cancellation signal. No idle timer.
internal sealed class X11EventWatch : IDisposable
{
    [StructLayout(LayoutKind.Sequential)] private struct PollFd { public int fd; public short events, revents; }
    [DllImport("libc", SetLastError=true)] private static extern int poll([In, Out] PollFd[] fds, nuint count, int timeout);
    [DllImport("libc", SetLastError=true)] private static extern int eventfd(uint initial, int flags);
    [DllImport("libc")] private static extern nint write(int fd, ref ulong data, nuint count);
    [DllImport("libc")] private static extern int close(int fd);
    private readonly int cancelFd = eventfd(0, 0x80000);
    private readonly CancellationTokenSource cancellation = new();
    private readonly SemaphoreSlim acknowledged = new(0);
    private readonly Task worker;
    public X11EventWatch(int xfd, Action pump)
    {
        if (cancelFd < 0) throw new InvalidOperationException("eventfd failed");
        worker = Task.Run(() =>
        {
            var fds = new[] { new PollFd { fd=xfd, events=1 }, new PollFd { fd=cancelFd, events=1 } };
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    int result = poll(fds, 2, -1);
                    if (result < 0) { if (Marshal.GetLastPInvokeError()==4) continue; break; }
                    if (fds[1].revents != 0 || cancellation.IsCancellationRequested) break;
                    if ((fds[0].revents & 1) == 0) break;
                    Dispatcher.UIThread.Post(() => { try { if (!cancellation.IsCancellationRequested) pump(); } finally { acknowledged.Release(); } });
                    acknowledged.Wait(cancellation.Token);
                }
            }
            catch (OperationCanceledException) { }
        });
    }
    public void Dispose()
    {
        cancellation.Cancel(); ulong signal=1; write(cancelFd, ref signal, 8);
        worker.GetAwaiter().GetResult(); close(cancelFd);
        // Queued UI callbacks may still release the acknowledgement semaphore.
    }
}
