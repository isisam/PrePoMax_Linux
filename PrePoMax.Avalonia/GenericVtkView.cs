using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using System.Diagnostics;
using Vtkmax.Interop;

namespace PrePoMax.Avalonia;
public sealed class GenericVtkView : OpenGlControlBase, IViewportHost
{
    public Control Control => this;
    public Viewport? View { get; private set; }
    public event Action? Ready;
    public event Action<PickResult>? Picked;
    public event Action? FramePresented;
    private readonly Queue<Action<Viewport>> actions = new();
    private ulong sequence;
    private int w, h;
    private long inputAt;
    public GenericVtkView() { Focusable = true; }
    public override void Render(DrawingContext context)
    { context.FillRectangle(Brushes.Transparent,new Rect(Bounds.Size));base.Render(context); }
    public void Execute(Action<Viewport> action) { actions.Enqueue(action); RequestNextFrameRendering(); }
    protected override void OnOpenGlInit(GlInterface gl)
    {
        if(GlVersion.Type!=GlProfileType.OpenGL)throw new PlatformNotSupportedException("VTK requires desktop OpenGL; select the X11 embedding backend on this platform.");
        View = new Viewport(backend: 2);
        View.RenderRequested += RequestNextFrameRendering;
        Console.WriteLine("EMBED generic current-context version="+GlVersion+" thread=" + Environment.CurrentManagedThreadId);
        Dispatcher.UIThread.Post(() => Ready?.Invoke());
    }
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (View is not { } view) return;
        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        int width = Math.Max(1, (int)Math.Round(Bounds.Width * scale));
        int height = Math.Max(1, (int)Math.Round(Bounds.Height * scale));
        if (width != w || height != h) { w = width; h = height; view.Resize(w, h); }
        while (actions.TryDequeue(out var action)) action(view);
        view.RenderExternal(fb);
        FramePresented?.Invoke();
        if (inputAt != 0) { Console.WriteLine($"INPUT_FRAME_MS={Stopwatch.GetElapsedTime(inputAt).TotalMilliseconds:F3}"); inputAt = 0; }
        var pick = view.LastPick;
        if (pick.Sequence != sequence) { sequence = pick.Sequence; Dispatcher.UIThread.Post(() => Picked?.Invoke(pick)); }
    }
    protected override void OnOpenGlDeinit(GlInterface gl) { View?.Dispose(); View = null; actions.Clear(); }
    private void Send(PointerEventArgs e, int kind)
    {
        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var p = e.GetPosition(this); int x = (int)Math.Round(p.X * scale), y = h - 1 - (int)Math.Round(p.Y * scale);
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control), shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        inputAt = Stopwatch.GetTimestamp(); Execute(v => v.Input(kind, x, y, ctrl, shift)); e.Handled = true;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus(); e.Pointer.Capture(this); var p = e.GetCurrentPoint(this).Properties;
        Send(e, p.IsMiddleButtonPressed ? 3 : p.IsRightButtonPressed ? 7 : 1);
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    { Send(e, e.InitialPressMouseButton == MouseButton.Middle ? 4 : e.InitialPressMouseButton == MouseButton.Right ? 8 : 2); e.Pointer.Capture(null); }
    protected override void OnPointerMoved(PointerEventArgs e)
    { if (e.GetCurrentPoint(this).Properties is { } p && (p.IsLeftButtonPressed || p.IsMiddleButtonPressed || p.IsRightButtonPressed)) Send(e, 0); }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e) => Send(e, e.Delta.Y > 0 ? 5 : 6);
    protected override void OnKeyDown(KeyEventArgs e)
    { if (e.Key is Key.W or Key.S or Key.P or Key.F or Key.E or Key.Q) e.Handled = true; base.OnKeyDown(e); }
}
