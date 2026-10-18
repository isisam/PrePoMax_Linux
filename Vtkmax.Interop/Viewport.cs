using System.Runtime.InteropServices;

namespace Vtkmax.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PickResult
{
    public long NodeId, ElementId;
    public int FaceId, Hit;
    public double X, Y, Z;
    public ulong Sequence;
    public long ActorId;
}

/// <summary>Owned, UI-thread-affine vtkmax view. Dispose on the creating thread.</summary>
public sealed class Viewport : IDisposable
{
    private IntPtr handle;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly bool generic;
    public event Action? RenderRequested;
    private Native.SelectionCallback? selectionCallback;
    public Exception? SelectionCallbackError { get; private set; }
    public void OnSelection(Action<PickResult>? callback)
    {
        selectionCallback = callback is null ? null : (ref PickResult result, IntPtr _) =>
        { try { callback(result); } catch (Exception e) { SelectionCallbackError = e; } };
        Check(Native.vm_selection_handler(Handle, selectionCallback, IntPtr.Zero));
    }
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
        if (Native.vm_abi_version() != 3) throw new NotSupportedException("vtkmax unstable 0.3 ABI mismatch");
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
    public void AddActor(long id, long[] nodeIds, double[] xyz, long[] elementIds, int[] types, int[] offsets, long[] connectivity)
    {
        if (xyz.Length != checked(nodeIds.Length * 3) || types.Length != elementIds.Length || offsets.Length != elementIds.Length + 1)
            throw new ArgumentException("Grid array lengths disagree.");
        Check(Native.vm_add_actor(Handle, id, nodeIds.Length, nodeIds, xyz, elementIds.Length, elementIds, types, offsets, connectivity.Length, connectivity));
    }
    public void RemoveActor(long id) => Check(Native.vm_remove_actor(Handle, id));
    public void ActorSelectable(long id, bool selectable) => Check(Native.vm_actor_selectable(Handle, id, selectable ? 1 : 0));
    public void ActorPickGeometry(long actor,long[] nodes,double[] xyz,long[] elements,int[] types,int[] offsets,long[] conn)
    {
        if(xyz.Length!=nodes.Length*3||types.Length!=elements.Length||offsets.Length!=elements.Length+1)throw new ArgumentException("Pick geometry lengths disagree.");
        Check(Native.vm_actor_pick_geometry(Handle,actor,nodes.Length,nodes,xyz,elements.Length,elements,types,offsets,conn.Length,conn));
    }
    public void SelectableFilter(long[]? ids)
    {
        // A non-null sentinel distinguishes the empty filter from no filter at the C boundary.
        Check(Native.vm_selectable_filter(Handle, ids is { Length: 0 } ? [-1] : ids, ids?.Length ?? 0));
    }
    public long[] SelectedIds(int association)
    {
        Check(Native.vm_selected_ids(Handle, association, out var pointer, out var count));
        var ids = new long[count]; if (count > 0) Marshal.Copy(pointer, ids, 0, count); return ids;
    }
    public (double[][] Planes, long[] Actors) AreaPick(int x1, int y1, int x2, int y2)
    {
        var planes = new double[36]; Check(Native.vm_area_pick(Handle, x1, y1, x2, y2, planes));
        return (Enumerable.Range(0, 6).Select(i => planes.Skip(i * 6).Take(6).ToArray()).ToArray(), SelectedIds(2));
    }
    public (long[] Points, long[] Cells) FrustumSelect(double[][] planes)
    {
        if (planes.Length != 6 || planes.Any(p => p.Length != 6)) throw new ArgumentException("Six planes of six components required.");
        Check(Native.vm_frustum_select(Handle, planes.SelectMany(p => p).ToArray())); return (SelectedIds(1), SelectedIds(0));
    }
    public (long Element, long[] Edge, long[] Face) GeometryPick(double[] point, bool unlimited = false)
    {
        if (point.Length != 3) throw new ArgumentException("Three coordinates required.");
        Check(Native.vm_geometry_pick(Handle, point, out var element, unlimited ? 1 : 0)); return (element, SelectedIds(3), SelectedIds(4));
    }
    public double SelectionPrecision { get { Check(Native.vm_selection_precision(Handle, out double value)); return value; } }
    public void SelectionDistance(int pixels) => Check(Native.vm_selection_distance(Handle, pixels));
    public void SelectionMode(bool enabled) => Check(Native.vm_selection_mode(Handle, enabled ? 1 : 0));
    public bool BoxSelectionActive { get { int value = Native.vm_box_selection_active(Handle); Check(value); return value != 0; } }
    public int[] LastGesture { get { var values = new int[7]; Check(Native.vm_last_gesture(Handle, values)); return values; } }
    public void ActorStyle(long id, bool visible, double r, double g, double b, double opacity = 1, bool edges = true) =>
        Check(Native.vm_actor_style(Handle, id, visible ? 1 : 0, r, g, b, opacity, edges ? 1 : 0));
    public void ActorScalars(long id, double[]? values, double min, double max, string title, bool nodes = true) =>
        Check(Native.vm_actor_scalars(Handle, id, nodes ? 1 : 0, values, values?.Length ?? 0, min, max, title));
    public void ContourBands(int bands) => Check(Native.vm_contour_bands(Handle, bands));
    public void Deformation(double[]? displacement, double scale, long actorId = 0) =>
        Check(Native.vm_deformation(Handle, actorId, displacement, displacement?.Length ?? 0, scale));
    public void ActorHighlight(long actorId, long[] ids, bool nodes = false) => Check(Native.vm_actor_highlight(Handle, actorId, nodes ? 1 : 0, ids, ids.Length));
    public void Perspective(bool enabled) => Check(Native.vm_perspective(Handle, enabled ? 1 : 0));
    public void AnimateStandardBegin(int view) => Check(Native.vm_animate_standard_begin(Handle, view));
    public void AnimateStandardStep(double fraction) => Check(Native.vm_animate_standard_step(Handle, fraction));
    public (double[] Position,double[] Focal,double[] Up) Camera
    { get { double[] p=new double[3],f=new double[3],u=new double[3];Check(Native.vm_camera(Handle,p,f,u));return(p,f,u); } }
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
    public void WritePng(string path, bool transparent, int magnification) => Check(Native.vm_write_png_scaled(Handle,path,transparent?1:0,magnification));
    public double ParallelScale { get { Check(Native.vm_camera_scale(Handle,out var scale));return scale; } }
    public void SetCamera(double[] position,double[] focal,double[] up,double scale)
    { if(position.Length!=3||focal.Length!=3||up.Length!=3)throw new ArgumentException("Camera vectors must have 3 values");Check(Native.vm_set_camera(Handle,position,focal,up,scale)); }
    public void ScalarBar(bool visible,string? title=null) => Check(Native.vm_scalar_bar(Handle,visible?1:0,title));
    public void ProcessEvents() => Check(Native.vm_process_events(Handle));
    public int X11Connection { get { int fd = Native.vm_x11_connection(Handle); Check(fd); return fd; } }
    public PickResult LastPick { get { Check(Native.vm_last_pick(Handle, out var r)); return r; } }
    public void Dispose()
    {
        if (handle == IntPtr.Zero) return;
        var owned = Handle; Native.vm_selection_handler(owned, null, IntPtr.Zero); handle = IntPtr.Zero; Native.vm_destroy(owned); selectionCallback = null;
    }
}

internal static class Native
{
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_actor_pick_geometry(IntPtr view,long actor,int np,long[] nodes,double[] xyz,int nc,long[] elements,int[] types,int[] offsets,int nconn,long[] conn);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_actor_selectable(IntPtr view,long actor,int selectable);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_selectable_filter(IntPtr view,long[]? actors,int count);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_area_pick(IntPtr view,int x1,int y1,int x2,int y2,[Out]double[] planes);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_frustum_select(IntPtr view,double[] planes);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_selected_ids(IntPtr view,int association,out IntPtr ids,out int count);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_geometry_pick(IntPtr view,double[] point,out long element,int unlimited);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_selection_precision(IntPtr view,out double distance);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_selection_distance(IntPtr view,int pixels);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_selection_mode(IntPtr view,int enabled);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_box_selection_active(IntPtr view);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_last_gesture(IntPtr view,[Out]int[] values);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_camera_scale(IntPtr view,out double scale);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_set_camera(IntPtr view,double[] position,double[] focal,double[] up,double scale);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_scalar_bar(IntPtr view,int visible,[MarshalAs(UnmanagedType.LPUTF8Str)]string? title);
    [DllImport("vtkmax", CallingConvention=CallingConvention.Cdecl)] internal static extern int vm_write_png_scaled(IntPtr view,[MarshalAs(UnmanagedType.LPUTF8Str)]string path,int transparent,int magnification);
    private const string Lib = "vtkmax";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void SelectionCallback(ref PickResult result, IntPtr user);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_selection_handler(IntPtr view, SelectionCallback? callback, IntPtr user);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_add_actor(IntPtr view, long id, int np, long[] ids, double[] xyz, int nc, long[] eids, int[] types, int[] offsets, int nconn, long[] conn);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_remove_actor(IntPtr view, long id);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_actor_style(IntPtr view, long id, int visible, double r, double g, double b, double opacity, int edges);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_actor_scalars(IntPtr view, long id, int association, double[]? values, int count, double min, double max, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_actor_highlight(IntPtr view, long id, int association, long[] ids, int count);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_contour_bands(IntPtr view, int bands);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_deformation(IntPtr view, long id, double[]? displacement, int count, double scale);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_perspective(IntPtr view, int enabled);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_animate_standard_begin(IntPtr view, int standard);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_animate_standard_step(IntPtr view, double fraction);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_camera(IntPtr view, [Out] double[] position, [Out] double[] focal, [Out] double[] up);
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
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_x11_connection(IntPtr view);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int vm_last_pick(IntPtr view, out PickResult result);
}
