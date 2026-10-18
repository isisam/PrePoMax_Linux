using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using PrePoMax.Viewer;
using Vtkmax.Interop;

static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
static void Reject(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
var output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);var receipts=new List<object>();
Require(Marshal.SizeOf<PickResult>()==64,"0.2 ABI layout");
var bounded=ViewerModel.Open(Path.Combine(args[1],"unstructured_65.inp"));
Require(bounded.Plies.Count<=ViewerModel.MaxColourGroups&&bounded.PlySource.StartsWith("Per-part"),"unstructured mesh must use bounded per-part palette");
Console.WriteLine("PASS bounded no-layup per-part colour fallback");
using(var view=new Viewport(backend:3))
{
    using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(args[1],"expected.json")));
    foreach(var spec in fixture.RootElement.EnumerateArray())
    {
        string name=spec.GetProperty("name").GetString()!;var model=ViewerModel.Open(Path.Combine(args[1],name+".inp"));
        long[] order=spec.GetProperty("order").EnumerateArray().Select(x=>x.GetInt64()).ToArray();
        Require(model.Types.SequenceEqual([spec.GetProperty("type").GetInt32()]),name+" VTK type");
        Require(model.Connectivity.Select(i=>model.NodeIds[i]).SequenceEqual(order),name+" independent node-order oracle");
        model.Show(view);view.StandardView(1);view.Render();
        double[] p=spec.GetProperty("point").EnumerateArray().Select(x=>x.GetDouble()).ToArray();var xy=view.Project(p[0],p[1],p[2]);
        var hit=view.Pick((int)Math.Round(xy[0]),(int)Math.Round(xy[1]));
        Require(hit.Hit==1&&hit.ElementId==9001&&hit.FaceId==spec.GetProperty("face").GetInt32(),$"{name} exact pick: {hit.ElementId}/{hit.FaceId}");
        view.WritePng(Path.Combine(output,name+".png"));receipts.Add(new{name,hit});Console.WriteLine($"PASS INP {name}: order, element=9001 face={hit.FaceId}");
    }
    // Wheel uses the actual interactor event in the embedding harness. Here assert
    // projection before/after camera zoom, including perspective.
    var xyz=new double[]{0,0,0,1,0,0,1,1,0,0,1,0,0,0,1,1,0,1,1,1,1,0,1,1};long[] nodes=[11,22,33,44,55,66,77,88];
    view.Load(nodes,xyz,[101],[12],[0,8],[0,1,2,3,4,5,6,7]);view.StandardView(1);view.Render();
    var before=view.Project(1,1,1);view.Zoom(1.2);view.Render();var after=view.Project(1,1,1);
    Require(Math.Abs(after[0]-500)>Math.Abs(before[0]-500)*1.19,"zoom projection before/after");
    view.AddActor(7,nodes,xyz.Select((x,i)=>i%3==2?x+2:x).ToArray(),[202],[12],[0,8],[0,1,2,3,4,5,6,7]);
    Reject(()=>view.AddActor(7,nodes,xyz,[999],[12],[0,8],[0,1,2,3,4,5,6,7]));
    view.StandardView(1);view.Render();int callbacks=0;view.OnSelection(r=>{Require(r.ActorId==7,"callback actor");callbacks++;});
    var center=view.Project(.5,.5,3);var occluded=view.Pick((int)center[0],(int)center[1]);
    Require(occluded.ActorId==7&&occluded.ElementId==202&&occluded.FaceId==5&&callbacks==1,"occlusion/callback");view.OnSelection(null);
    view.ActorStyle(7,false,.4,.5,.6);view.Render();var revealed=view.Pick((int)center[0],(int)center[1]);
    Require(revealed.ActorId==0&&revealed.ElementId==101&&revealed.FaceId==5,"hidden front actor reveals rear");
    view.ActorStyle(7,true,.2,.8,.5,.5,false);view.ActorScalars(7,Enumerable.Range(0,8).Select(x=>(double)x).ToArray(),0,7,"Actor node value");view.ContourBands(8);
    view.Deformation(Enumerable.Repeat(.1,24).ToArray(),2,7);view.Deformation(Enumerable.Repeat(.1,24).ToArray(),2,7);view.Render();
    var moved=view.Project(.7,.7,3.2);Require(Math.Abs(view.Pick((int)moved[0],(int)moved[1]).Z-3.2)<1e-8,"deformation is absolute, not cumulative");
    view.Deformation(null,0,7);view.ActorHighlight(7,[202]);view.WritePng(Path.Combine(output,"actors.png"));view.Highlight([]);
    view.Perspective(true);view.Render();center=view.Project(.5,.5,3);var perspective=view.Pick((int)center[0],(int)center[1]);
    Require(perspective.ActorId==7&&perspective.ElementId==202&&perspective.FaceId==5,"perspective occlusion pick");
    using(var second=new Viewport(backend:3))
    {
        second.Load(nodes,xyz,[303],[12],[0,8],[0,1,2,3,4,5,6,7]);second.StandardView(2);second.Render();var point=second.Project(.5,.5,0);
        var pick=second.Pick((int)point[0],(int)point[1]);Require(pick.ElementId==303&&pick.FaceId==4,"second view back face");
        Require(view.Pick((int)center[0],(int)center[1]).ElementId==202,"two-view state isolation");
    }
    view.AnimateStandardBegin(3);view.AnimateStandardStep(.5);view.AnimateStandardStep(1);view.Render();view.RemoveActor(7);Reject(()=>view.RemoveActor(7));
    view.StandardView(1);view.AnimateStandardBegin(2);view.AnimateStandardStep(.5);var middle=view.Camera;
    double distance=Math.Sqrt(middle.Position.Zip(middle.Focal,(a,b)=>(a-b)*(a-b)).Sum());Require(distance>1&&middle.Up.All(double.IsFinite),"180-degree camera animation must not collapse through focal point");
    view.AnimateStandardStep(1);var end=view.Camera;Require(end.Position[2]<end.Focal[2]&&end.Up[1]>.99,"animation exact Windows back endpoint");
    receipts.Add(new{check="zoom_occlusion_perspective_two_views_callbacks_animation",before,after,occluded,revealed,perspective,callbacks});
    Console.WriteLine("PASS zoom, multi-actor styles, deformation, occlusion, perspective, two views, callbacks, animation");
    Console.WriteLine(view.GlInfo);
    foreach(string path in args.Skip(2))
    {
        var model=ViewerModel.Open(path);model.Show(view);view.StandardView(1);view.Render();
        double z=model.Model.Mesh.Nodes.Values.Max(n=>n.Z);
        var element=model.Model.Mesh.Elements.Values.OrderBy(e=>e.Id).First(e=>e.NodeIds.Count(id=>Math.Abs(model.Model.Mesh.Nodes[id].Z-z)<1e-8)>=4);
        var top=element.NodeIds.Select(id=>model.Model.Mesh.Nodes[id]).Where(n=>Math.Abs(n.Z-z)<1e-8).ToArray();
        var point=view.Project(top.Average(n=>n.X),top.Average(n=>n.Y),z);var hit=view.Pick((int)Math.Round(point[0]),(int)Math.Round(point[1]));
        Require(hit.Hit==1&&hit.ElementId==element.Id&&hit.FaceId==5,Path.GetFileName(path)+" exact face 5");
        receipts.Add(new{model=Path.GetFileName(path),nodes=model.NodeIds.Length,elements=model.ElementIds.Length,hit});Console.WriteLine($"PASS {Path.GetFileName(path)} element={hit.ElementId} exact face=5");
    }
}
foreach(int nz in new[]{10,50})
{
    using var view=new Viewport(backend:3);const int nx=100,ny=100;int np=(nx+1)*(ny+1)*(nz+1),nc=nx*ny*nz;
    long[] ids=new long[np],cells=new long[nc],conn=new long[nc*8];double[] xyz=new double[np*3];int[] offsets=new int[nc+1],types=Enumerable.Repeat(12,nc).ToArray();
    int Node(int x,int y,int z)=>(z*(ny+1)+y)*(nx+1)+x;
    for(int z=0;z<=nz;z++)for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){int n=Node(x,y,z);ids[n]=n+10;xyz[3*n]=x;xyz[3*n+1]=y;xyz[3*n+2]=z;}
    int e=0;for(int z=0;z<nz;z++)for(int y=0;y<ny;y++)for(int x=0;x<nx;x++,e++)
    {cells[e]=e+100;offsets[e]=e*8;long[] c=[Node(x,y,z),Node(x+1,y,z),Node(x+1,y+1,z),Node(x,y+1,z),Node(x,y,z+1),Node(x+1,y,z+1),Node(x+1,y+1,z+1),Node(x,y+1,z+1)];Array.Copy(c,0,conn,e*8,8);}offsets[nc]=conn.Length;
    var load=Stopwatch.StartNew();view.Load(ids,xyz,cells,types,offsets,conn);load.Stop();view.StandardView(1);view.Render();
    var xy=view.Project(50.5,50.5,nz);var times=new List<double>();long expected=100+(nz-1)*nx*ny+50*nx+50;
    for(int i=0;i<11;i++){var t=Stopwatch.StartNew();var hit=view.Pick((int)xy[0],(int)xy[1]);t.Stop();Require(hit.ElementId==expected&&hit.FaceId==5,$"{nc} mesh exact face pick {hit.ElementId}/{hit.FaceId}");if(i>0)times.Add(t.Elapsed.TotalMilliseconds);}
    var frames=Stopwatch.StartNew();const int nframes=12;for(int i=0;i<nframes;i++){view.Rotate(2,1);view.Render();}frames.Stop();times.Sort();
    var receipt=new{elements=nc,nodes=np,loadMs=load.Elapsed.TotalMilliseconds,pickMedianMs=times[times.Count/2],pickMaxMs=times.Max(),fps=nframes/frames.Elapsed.TotalSeconds,renderer=view.GlInfo};
    receipts.Add(receipt);Console.WriteLine(JsonSerializer.Serialize(receipt));view.WritePng(Path.Combine(output,$"mesh-{nc}.png"));
}
File.WriteAllText(Path.Combine(output,"receipt.json"),JsonSerializer.Serialize(receipts,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
Console.WriteLine("ALL L2b VIEWER CHECKS PASS");
