using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Vtkmax.Interop;

namespace PrePoMax.Avalonia;
public sealed class VtkMaxView : NativeControlHost, IViewportHost
{
    public Control Control => this;
    public void Execute(Action<Viewport> action) { if (View is { } v) action(v); }
    public Viewport? View { get; private set; }
    public event Action? Ready;
    public event Action<PickResult>? Picked;
    private X11EventWatch? watch;
    public long PumpCount { get; private set; }
    private int width, height;
    public VtkMaxView() { SizeChanged += (_, _) => UpdateSize(); Focusable = true; }
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (parent.HandleDescriptor != "XID" && parent.HandleDescriptor != "X11")
            throw new PlatformNotSupportedException($"X11 embedding required, got {parent.HandleDescriptor}.");
        View = new Viewport(false);
        try
        {
            var child = View.AttachX11(parent.Handle);
            Console.WriteLine($"EMBED parent={parent.Handle} child={child} descriptor={parent.HandleDescriptor}");
            View.OnSelection(r => Picked?.Invoke(r));
            Dispatcher.UIThread.Post(() => { UpdateSize(); Ready?.Invoke(); Pump(); watch = new X11EventWatch(View!.X11Connection, Pump); });
            return new PlatformHandle(child, "XID");
        }
        catch { View.Dispose(); View = null; throw; }
    }
    private void UpdateSize()
    {
        if (View is null) return;
        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        int w = Math.Max(1, (int)Math.Round(Bounds.Width * scale)), h = Math.Max(1, (int)Math.Round(Bounds.Height * scale));
        if (w != width || h != height) { width = w; height = h; View.Resize(w, h); View.Render(); }
    }
    private void Pump()
    {
        if (View is null) return;
        PumpCount++; View.ProcessEvents();
    }
    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        watch?.Dispose(); watch = null; View?.Dispose(); View = null; width = height = 0;
        // vtkmax owns/destroys its X11 child; do not let the base destroy it twice.
    }
}
