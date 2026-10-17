using System.Runtime.InteropServices;

namespace Vtkmax.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PickResult
{
    public long NodeId, ElementId;
    public int FaceId, Hit;
    public double X, Y, Z;
    public ulong Sequence;
}

/// <summary>Owned, UI-thread-affine vtkmax view. Dispose on the creating thread.</summary>
public sealed class Viewport : IDisposable
{
    private IntPtr handle;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly bool generic;
    public event Action? RenderRequested;
    private IntPtr Handle
    {
        get
        {
            if (thread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Use the creating UI thread.");
            ObjectDisposedException.ThrowIf(handle == IntPtr.Zero, this);
            return handle;
        }
    }
    private static string Error => Marshal.PtrToStringUTF8(Native.vm_last_error()) ?? "vtkmax error";
    private static void Check(int status) { if (status < 0) throw new InvalidOperationException(Error); }
    public Viewport(bool offscreen = true, int width = 1000, int height = 700, int backend = -1)
    {
        if (Native.vm_abi_version() != 2) throw new NotSupportedException("vtkmax unstable 0.2 ABI mismatch");
        generic = backend == 2;
        handle = backend < 0 ? Native.vm_create(offscreen ? 1 : 0, width, height) : Native.vm_create_backend(backend, width, height);
        if (handle == IntPtr.Zero) throw new InvalidOperationException(Error);
    }
    public IntPtr AttachX11(IntPtr parent)
    {
        var child = Native.vm_attach_x11(Handle, (UIntPtr)parent);
        if (child == UIntPtr.Zero) throw new InvalidOperationException(Error);
        return (IntPtr)child;
    }
    public void Load(long[] nodeIds, double[] xyz, long[] elementIds, int[] types, int[] offsets, long[] connectivity)
    {
        if (xyz.Length != checked(nodeIds.Length * 3) || types.Length != elementIds.Length || offsets.Length != elementIds.Length + 1)
            throw new ArgumentException("Grid array lengths disagree.");
        Check(Native.vm_load_grid(Handle, nodeIds.Length, nodeIds, xyz, elementIds.Length, elementIds, types, offsets, connectivity.Length, connectivity));
    }
    public void Resize(int width, int height) => Check(Native.vm_resize(Handle, width, height));
    public void Display(bool surface, bool edges) => Check(Native.vm_display(Handle, surface ? 1 : 0, edges ? 1 : 0));
    public void Scalars(double[]? values, double min = 0, double max = 1, string title = "Ply", bool nodes = false) =>
        Check(Native.vm_scalars(Handle, nodes ? 1 : 0, values, values?.Length ?? 0, min, max, title));
    public void Highlight(long[] ids, bool nodes = false) => Check(Native.vm_highlight(Handle, nodes ? 1 : 0, ids, ids.Length));
    public void Fit() => Check(Native.vm_fit(Handle));
    public void StandardView(int view = 0) => Check(Native.vm_standard_view(Handle, view));
    public void Rotate(double azimuth, double elevation) => Check(Native.vm_rotate(Handle, azimuth, elevation));
    public void Zoom(double factor) => Check(Native.vm_zoom(Handle, factor));
    public void Render() { if (generic) RenderRequested?.Invoke(); else Check(Native.vm_render(Handle)); }
    public void RenderExternal(int framebuffer) => Check(Native.vm_render_external(Handle, framebuffer));
    public void Input(int kind, int x, int y, bool control = false, bool shift = false) => Check(Native.vm_input(Handle, kind, x, y, control ? 1 : 0, shift ? 1 : 0));
    public string GlInfo => Marshal.PtrToStringUTF8(Native.vm_gl_info(Handle)) ?? "";
    public PickResult Pick(int x, int y) { Check(Native.vm_pick(Handle, x, y, out var r)); return r; }
    public double[] Project(double x, double y, double z)
    {
        double[] result = new double[3]; Check(Native.vm_project(Handle, [x, y, z], result)); return result;
    }
    public void WritePng(string path) => Check(Native.vm_write_png(Handle, path));
    public void ProcessEvents() => Check(Native.vm_process_events(Handle));
    public PickResult LastPick { get { Check(Native.vm_last_pick(Handle, out var r)); return r; } }
    public void Dispose()
    {
        if (handle == IntPtr.Zero) return;
        var owned = Handle; handle = IntPtr.Zero; Native.vm_destroy(owned);
    }
}

internal static class Native
{
    private const string Lib = "vtkmax";
    // Every entry point is Cdecl; native strings use UTF-8, IDs are int64 on all platforms.
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_abi_version();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr vm_last_error();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr vm_create(int offscreen, int width, int height);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr vm_create_backend(int backend, int width, int height);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_render_external(IntPtr view, int framebuffer);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_input(IntPtr view, int kind, int x, int y, int control, int shift);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr vm_gl_info(IntPtr view);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void vm_destroy(IntPtr view);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern UIntPtr vm_attach_x11(IntPtr view, UIntPtr parent);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_resize(IntPtr view, int width, int height);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_load_grid(IntPtr view, int np, long[] ids, double[] xyz, int nc, long[] eids, int[] types, int[] offsets, int nconn, long[] conn);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_display(IntPtr view, int surface, int edges);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_scalars(IntPtr view, int association, double[]? values, int count, double min, double max, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_highlight(IntPtr view, int association, long[] ids, int count);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_fit(IntPtr view);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_standard_view(IntPtr view, int standard);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_rotate(IntPtr view, double azimuth, double elevation);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_zoom(IntPtr view, double factor);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_render(IntPtr view);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_pick(IntPtr view, int x, int y, out PickResult result);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_project(IntPtr view, double[] xyz, [Out] double[] display);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_write_png(IntPtr view, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_process_events(IntPtr view);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_last_pick(IntPtr view, out PickResult result);
}
