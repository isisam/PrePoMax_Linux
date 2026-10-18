// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Threading;
using CaeGlobals;
using System.Drawing;
using vtkControl;
using Vtkmax.Interop;

namespace PrePoMax.Avalonia;

/// <summary>L1b contract adapter. All GL work runs in the host's current-context render queue.</summary>
public sealed partial class VtkMaxView : UserControl, IViewport3D, IViewportHost
{
    private readonly IViewportHost host;
    private readonly Dictionary<string, Actor> actors = new(StringComparer.Ordinal);
    private long nextId;
    private vtkEdgesVisibility edges = vtkEdgesVisibility.ElementEdges;
    private bool rendering = true;
    private string scalarTitle = "";
    private vtkMaxColorSpectrum spectrum = new();
    private DispatcherTimer? animation;
    public Control Control => this;
    public Viewport? View => host.View;
    public long PumpCount => host is X11VtkView x ? x.PumpCount : 0;
    public bool HasViewportFocus => host.Control.IsFocused;
    public event Action? Ready;
    public event Action<PickResult>? Picked;
    public event Action<Exception>? OperationFailed;
    public event Action<double[], double[], double[][], bool, vtkSelectOperation, string[]>? OnMouseLeftButtonUpSelection;
    public event Action<double[]>? RotationPointChanged;
    public VtkMaxView() : this(Environment.GetEnvironmentVariable("VTKMAX_EMBED") == "x11" ? new X11VtkView() : new GenericVtkView()) { }
    internal VtkMaxView(IViewportHost host)
    {
        this.host = host; Content = host.Control;
        host.Ready += () => Ready?.Invoke();
        host.Picked += ReceivePick;
        DetachedFromVisualTree += (_,_) => animation?.Stop();
    }
    public void Execute(Action<Viewport> action) => host.Execute(action);
    internal Task<double> BenchmarkFramesAsync()
    {
        if(host is not GenericVtkView generic)throw Unsupported("Embedded benchmark on X11 backend");
        var completion=new TaskCompletionSource<double>();var watch=new System.Diagnostics.Stopwatch();int frames=0;
        void Frame()
        {
            frames++;if(frames==5)watch.Start();
            if(frames==35){generic.FramePresented-=Frame;completion.SetResult(30/watch.Elapsed.TotalSeconds);return;}
            Execute(v=>v.Rotate(1,.25));
        }
        generic.FramePresented+=Frame;Execute(v=>v.Rotate(1,.25));return completion.Task;
    }
    private void Change(Action<Viewport> action) => Execute(v =>
    {
        try { action(v); if (rendering) v.Render(); }
        catch(Exception e) { Console.Error.WriteLine(e); OperationFailed?.Invoke(e); }
    });
    // Getters only read camera/data and do not issue GL calls.
    private Viewport Native => View ?? throw new InvalidOperationException("Viewport is not ready.");
    private sealed class Actor(VtkMaxView owner, long id, vtkMaxActorData data) : IViewportActor
    {
        internal readonly VtkMaxView Owner = owner;
        internal readonly long Id = id;
        internal vtkMaxActorData Data = data;
        internal bool Visible = true, Contours;
        public uint MemorySize => checked((uint)(Data.Geometry.Nodes.Coor.Length * 24L + Data.Geometry.Cells.CellNodeIds.Sum(c=>c.Length)*4L));
        public int GetNumberOfElements() => Data.Geometry.Cells.CellNodeIds.Length;
        public Color Color { get => Data.Color; set { Data.Color=value; Owner.Style(this); } }
        public bool UseSecondaryHighlightColor { get=>Data.UseSecondaryHighlightColor; set=>Data.UseSecondaryHighlightColor=value; }
    }
    private void Style(Actor a) => Change(v=>v.ActorStyle(a.Id,a.Visible,a.Color.R/255d,a.Color.G/255d,a.Color.B/255d,a.Color.A/255d,edges!=vtkEdgesVisibility.NoEdges));
    public IViewportActor AddCells(vtkMaxActorData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if(string.IsNullOrEmpty(data.Name))throw new ArgumentException("Actor name is required.");
        if(actors.ContainsKey(data.Name))throw new ArgumentException("Duplicate actor: "+data.Name);
        var copy = Copy(data); var g=copy.Geometry;
        var coords=g.Nodes.Coor ?? throw new ArgumentException("Actor needs coordinates.");
        var cells=g.Cells.CellNodeIds ?? throw new ArgumentException("Actor needs cell connectivity.");
        // PartExchangeData cell connectivity is LOCAL zero-based, not FE node IDs (vtkControl convention).
        if(coords.Length==0 || cells.Any(c=>c.Any(i=>i<0||i>=coords.Length)))throw new ArgumentException("Invalid local actor connectivity.");
        var nodes=g.Nodes.Ids?.Select(i=>(long)i).ToArray() ?? Enumerable.Range(0,coords.Length).Select(i=>(long)i).ToArray();
        var ids=g.Cells.Ids?.Select(i=>(long)i).ToArray() ?? Enumerable.Range(0,cells.Length).Select(i=>(long)i).ToArray();
        var types=g.Cells.Types ?? throw new ArgumentException("Actor needs VTK cell types.");
        if(nodes.Length!=coords.Length||ids.Length!=cells.Length||types.Length!=cells.Length)throw new ArgumentException("Actor array lengths disagree.");
        var offsets=new int[cells.Length+1];for(int i=0;i<cells.Length;i++)offsets[i+1]=checked(offsets[i]+cells[i].Length);
        var actor=new Actor(this,++nextId,copy); actors.Add(copy.Name,actor);
        Change(v=>{v.AddActor(actor.Id,nodes,coords.SelectMany(p=>p).ToArray(),ids,types,offsets,cells.SelectMany(c=>c.Select(i=>(long)i)).ToArray());
            v.ActorStyle(actor.Id,true,actor.Color.R/255d,actor.Color.G/255d,actor.Color.B/255d,actor.Color.A/255d,edges!=vtkEdgesVisibility.NoEdges);
            if(copy.ColorContours && g.Nodes.Values is { } values) ApplyScalars(v,actor,values);});
        return actor;
    }
    private static vtkMaxActorData Copy(vtkMaxActorData d) => new()
    { Name=d.Name,ParentName=d.ParentName,IsAPart=d.IsAPart,Color=d.Color.IsEmpty?Color.LightGray:d.Color,Layer=d.Layer,
      Pickable=d.Pickable,ColorContours=d.ColorContours,UseSecondaryHighlightColor=d.UseSecondaryHighlightColor,
      Geometry=d.Geometry.DeepCopy(),ActorRepresentation=d.ActorRepresentation };
    public void AddCells(vtkMaxActorData[] data) {foreach(var d in data)AddCells(d);}
    public IViewportActor AddPoints(vtkMaxActorData data)
    {
        var copy=Copy(data);int n=copy.Geometry.Nodes.Coor.Length;
        copy.Geometry.Cells=new(){Ids=copy.Geometry.Nodes.Ids,Types=Enumerable.Repeat(1,n).ToArray(),CellNodeIds=Enumerable.Range(0,n).Select(i=>new[]{i}).ToArray()};
        return AddCells(copy);
    }
    public void AddActor(IViewportActor actor)
    {if(actor is not Actor a || a.Owner!=this)throw new ArgumentException("Actor belongs to another viewport."); if(!ContainsActor(a.Data.Name))AddCells(a.Data);else {a.Visible=true;Style(a);}}
    public bool ContainsActor(string actorName)=>actors.ContainsKey(actorName);
    public void RemoveActors(string[] names) {foreach(var name in names)if(actors.Remove(name,out var a))Change(v=>v.RemoveActor(a.Id));}
    public void HideActors(string[] actorNames,bool updateColorContours) {foreach(var n in actorNames){var a=actors[n];a.Visible=false;Style(a);}}
    public void ShowActors(string[] actorNames,bool updateColorContours) {foreach(var n in actorNames){var a=actors[n];a.Visible=true;Style(a);}}
    public void UpdateResultRepresentationVisibility(string[] partNames,bool visible,bool update) {if(visible)ShowActors(partNames,update);else HideActors(partNames,update);}
    public void UpdateActor(string oldName,string newName,Color newColor)
    {if(oldName!=newName&&actors.ContainsKey(newName))throw new ArgumentException("Duplicate actor name");var a=actors[oldName];actors.Remove(oldName);a.Data.Name=newName;actors.Add(newName,a);a.Color=newColor;}
    public void UpdateActorColor(IViewportActor actor,Color newColor)
    {if(actor is not Actor a || a.Owner!=this)throw new ArgumentException("Foreign actor");a.Color=newColor;}
    public vtkMaxActorData[] GetVtkMaxActorData()=>actors.Values.Select(a=>Copy(a.Data)).ToArray();
    public void Clear() {RemoveActors(actors.Keys.ToArray());ClearSelection();}
    public void ClearButKeepParts()=>RemoveActors(actors.Where(p=>!p.Value.Data.IsAPart).Select(p=>p.Key).ToArray());
    public void ClearOverlay()=>RemoveActors(actors.Where(p=>p.Value.Data.Layer!=vtkRendererLayer.Base).Select(p=>p.Key).ToArray());
    public void RemoveOverlayActors(string[] actorNames)=>RemoveActors(actorNames);
    public bool RenderingOn { get=>rendering;set {rendering=value;if(value)Change(_=>{});} }
    public bool UserPick {get;set;}=true;
    public bool QuerySelectionInProgress {get;set;}
    public bool ViewAnimationEnabled {get;set;}=true;
    public vtkSelectBy SelectBy {get;set;}
    public vtkSelectItem SelectItem {get;set;}=vtkSelectItem.Element;
    public void SetSelectBy(vtkSelectBy selectBy)=>SelectBy=selectBy;
    public vtkEdgesVisibility EdgesVisibility
    {get=>edges;set {if(value==vtkEdgesVisibility.ModelEdges||value==vtkEdgesVisibility.Wireframe)throw Unsupported("EdgesVisibility "+value);edges=value;foreach(var a in actors.Values)Style(a);}}
    public void Pick(int x1,int y1,bool boxSelection,int x2,int y2)
    {if(boxSelection)throw Unsupported("Box picking");Change(v=>ReceivePick(v.Pick(x1,y1)));}
    private void ReceivePick(PickResult r)
    {
        if(!UserPick)return;
        Picked?.Invoke(r);
        var actor=actors.Values.FirstOrDefault(a=>a.Id==r.ActorId);
        OnMouseLeftButtonUpSelection?.Invoke([r.X,r.Y,r.Z],GetViewPlaneNormal(),[],false,vtkSelectOperation.None,actor==null?[]:[actor.Data.Name]);
        long id=SelectItem==vtkSelectItem.Node?r.NodeId:r.ElementId;
        if(r.Hit!=0&&id>=0)Change(v=>v.ActorHighlight(r.ActorId,[id],SelectItem==vtkSelectItem.Node));
    }
    public void HighlightActor(string actorName) {var a=actors[actorName];Change(v=>v.ActorHighlight(a.Id,a.Data.Geometry.Cells.Ids.Select(i=>(long)i).ToArray()));}
    public void ClearSelection(bool clearAllMouseSelection=true)=>Change(v=>v.Highlight([]));
    public void ClearCurrentMouseSelection()=>ClearSelection();
    public void ClearCurrentMouseSelection(bool render)=>ClearSelection();
    public void GetViewParameters(out double[] position,out double[] focalPoint,out double[] viewUp,out double parallelScale)
    {var c=Native.Camera;position=c.Position;focalPoint=c.Focal;viewUp=c.Up;parallelScale=Native.ParallelScale;}
    public void SetViewParameters(bool animate,double[] position,double[] focalPoint,double[] viewUp,double parallelScale)
    {if(animate)throw Unsupported("Arbitrary camera animation");Change(v=>v.SetCamera(position,focalPoint,viewUp,parallelScale));}
    public void Standard(int standard,bool animate=false)
    {
        animation?.Stop();
        if(!animate||!ViewAnimationEnabled){Change(v=>v.StandardView(standard));return;}
        Change(v=>v.AnimateStandardBegin(standard));var clock=System.Diagnostics.Stopwatch.StartNew();
        animation=new(){Interval=TimeSpan.FromMilliseconds(16)};
        animation.Tick+=(_,_)=>{var t=Math.Min(1,clock.Elapsed.TotalMilliseconds/500);Change(v=>v.AnimateStandardStep(t));if(t==1)animation.Stop();};animation.Start();
    }
    public void SetFrontBackView(bool animate,bool front)=>Standard(front?1:2,animate);
    public void SetTopBottomView(bool animate,bool top)=>Standard(top?3:4,animate);
    public void SetLeftRightView(bool animate,bool left)=>Standard(left?6:5,animate);
    public void SetIsometricView(bool animate,bool positive,bool zoomToFit=true) {if(!positive)throw Unsupported("Negative isometric view");Standard(0,animate);}
    public void SetZoomToFit(bool animate) {if(animate)throw Unsupported("Animated fit");Change(v=>v.Fit());}
    public void SetZoomFactor(double factor)=>Change(v=>v.Zoom(factor));
    public void Rotate(double azimuthAngle,double elevationAngle,double rollAngle)
    {if(rollAngle!=0)throw Unsupported("Camera roll");Change(v=>v.Rotate(azimuthAngle,elevationAngle));}
    public double[] GetViewPlaneNormal() {var c=Native.Camera;var n=Subtract(c.Position,c.Focal);double length=Math.Sqrt(n.Sum(x=>x*x));return n.Select(x=>x/length).ToArray();}
    public double[] GetClosestUpView()=>Native.Camera.Up;
    public double[] Subtract(double[] v1,double[] v2)=>v1.Zip(v2,(a,b)=>a-b).ToArray();
    public double GetAngle(double[] v1,double[] v2)=>Math.Acos(Math.Clamp(v1.Zip(v2,(a,b)=>a*b).Sum()/Math.Sqrt(v1.Sum(x=>x*x)*v2.Sum(x=>x*x)),-1,1))*180/Math.PI;
    public void SetRotationPoint(double[] rotationPoint)
    {var c=Native.Camera;Change(v=>v.SetCamera(c.Position,rotationPoint,c.Up,v.ParallelScale));RotationPointChanged?.Invoke(rotationPoint);}
    public void UpdateScalarsAndRedraw()=>Change(_=>{});
    public void UpdateScalarsAndCameraAndRedraw()=>Change(v=>v.Fit());
    public void AddScalarFieldOnCells(vtkMaxActorData data,bool update) {if(ContainsActor(data.Name))RemoveActors([data.Name]);AddCells(data);}
    private void ApplyScalars(Viewport v,Actor a,float[] values)
    {
        a.Contours=true;var d=values.Select(x=>(double)x).ToArray();
        double lo=spectrum.MinMaxType==vtkColorSpectrumMinMaxType.Automatic?d.Min():spectrum.MinUserValue;
        double hi=spectrum.MinMaxType==vtkColorSpectrumMinMaxType.Automatic?d.Max():spectrum.MaxUserValue;
        v.ActorScalars(a.Id,d,lo,hi,scalarTitle);v.ScalarBar(true,scalarTitle);
    }
    public void UpdateActorScalarField(string actorName,float[] values,NodesExchangeData extremeNodes,ResultFieldExchangeData resultField,bool update)
    {var a=actors[actorName];if(values.Length!=a.Data.Geometry.Nodes.Coor.Length)throw new ArgumentException("Nodal scalar count differs");a.Data.Geometry.Nodes.Values=values.ToArray();Change(v=>ApplyScalars(v,a,values));}
    public void UpdateActorsColorContoursVisibility(string[] actorNames,bool colorContours)
    {foreach(var name in actorNames){var a=actors[name];if(colorContours&&a.Data.Geometry.Nodes.Values is {} values)Change(v=>ApplyScalars(v,a,values));else Change(v=>v.ActorScalars(a.Id,null,0,1,scalarTitle));}}
    public void SetScalarBarVisibility(bool visibility)=>Change(v=>v.ScalarBar(visibility));
    public void SetScalarBarText(string fieldName,string componentName,string unitAbbreviation,string complexComponent,string minMaxType)
    {scalarTitle=string.Join(" ",new[]{fieldName,componentName,unitAbbreviation,complexComponent,minMaxType}.Where(s=>!string.IsNullOrEmpty(s)));Change(v=>v.ScalarBar(true,scalarTitle));}
    public void SetScalarBarColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
    {if(colorSpectrum.ReverseColors||colorSpectrum.Type!=vtkColorSpectrumType.Rainbow)throw Unsupported("Custom/reversed colour palette");spectrum.DeepCopy(colorSpectrum);Change(v=>v.ContourBands(colorSpectrum.Mode==vtkColorSpectrumMode.Smooth?256:colorSpectrum.NumberOfColors));}
    public bool ScalarBarAddMinColor=>false;
    public bool ScalarBarAddMaxColor=>false;
    public void SetDeformation(string actorName,double[]? displacement,double scale)=>Change(v=>v.Deformation(displacement,scale,actors[actorName].Id));
    public void RenderToPNG(string fileName)=>RenderToPNG(fileName,false,1);
    public void RenderToPNG(string fileName,bool transparentBackground,int magnification)=>Change(v=>v.WritePng(fileName,transparentBackground,magnification));
    public double[] GetBoundingBox()
    {var p=actors.Values.Where(a=>a.Visible).SelectMany(a=>a.Data.Geometry.Nodes.Coor).ToArray();if(p.Length==0)return new double[6];return Enumerable.Range(0,3).SelectMany(i=>new[]{p.Min(x=>x[i]),p.Max(x=>x[i])}).ToArray();}
    public double[] GetBoundingBoxSize(){var b=GetBoundingBox();return[b[1]-b[0],b[3]-b[2],b[5]-b[4]];}
    public double GetBoundingBoxDiagonal()=>Math.Sqrt(GetBoundingBoxSize().Sum(x=>x*x));
    private static NotSupportedException Unsupported(string member)=>new($"L2 viewport does not support {member}; planned for L3/L4.");
}
