// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using CaeGlobals;
using System.Drawing;
using System.Text.Json;
using vtkControl;
using Vtkmax.Interop;
namespace PrePoMax.Avalonia;

internal static class ViewportContractChecks
{
    private sealed class EglHost : IViewportHost,IDisposable
    {
        public Control Control {get;}=new Panel();
        public Viewport View {get;}=new(backend:3);
        public event Action? Ready {add{} remove{}}
        public event Action<PickResult>? Picked {add{} remove{}}
        public void Execute(Action<Viewport> action)=>action(View);
        public void Dispose()=>View.Dispose();
    }
    public static void Run(string output)
    {
        Directory.CreateDirectory(output);
        using var host=new EglHost();var adapter=new VtkMaxView(host);IViewport3D v=adapter;
        adapter.OperationFailed+=e=>throw new Exception("Queued viewport operation failed",e);
        void Require(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
        var d=new vtkMaxActorData{Name="cube",IsAPart=true,Pickable=true,Color=Color.Red};
        d.Geometry.Nodes=new(){Ids=[11,22,33,44,55,66,77,88],Coor=[[0,0,0],[1,0,0],[1,1,0],[0,1,0],[0,0,1],[1,0,1],[1,1,1],[0,1,1]]};
        d.Geometry.Cells=new(){Ids=[909],Types=[12],CellNodeIds=[[0,1,2,3,4,5,6,7]]};
        var actor=v.AddCells(d);d.Geometry.Nodes.Coor[0][0]=99;
        Require(v.GetVtkMaxActorData()[0].Geometry.Nodes.Coor[0][0]==0,"DTO copy isolation");
        v.SetFrontBackView(false,true);host.View.Render();
        var p=host.View.Project(.5,.5,1);var hit=host.View.Pick((int)p[0],(int)p[1]);
        Console.WriteLine(JsonSerializer.Serialize(new {p,hit},new JsonSerializerOptions{IncludeFields=true}));
        Require(hit.Hit==1&&hit.ElementId==909&&hit.FaceId==5&&hit.ActorId>0,"contract actor exact element/face IDs");
        var corner=host.View.Project(.001,.001,1);var node=host.View.Pick((int)Math.Round(corner[0]),(int)Math.Round(corner[1]));
        Require(node.NodeId==55,"contract exact node ID");
        int selections=0;adapter.Picked+=r=>{selections++;Require(r.ElementId==909,"selection ID callback");};
        v.Pick((int)p[0],(int)p[1],false,0,0);Require(selections==1,"selection callback once");
        v.HideActors(["cube"],true);Require(host.View.Pick((int)p[0],(int)p[1]).Hit==0,"hidden actor unpickable");v.ShowActors(["cube"],true);
        v.UpdateActor("cube","part",Color.FromArgb(128,0,0,255));Require(actor.Color.A==128&&v.ContainsActor("part"),"rename colour transparency");
        v.EdgesVisibility=vtkEdgesVisibility.NoEdges;v.SetTopBottomView(false,true);v.GetViewParameters(out var pos,out var focal,out var up,out var scale);
        Require(pos[1]>focal[1]&&up[2]<-.99,"Windows top convention");
        v.SetViewParameters(false,pos,focal,up,scale);v.SetLeftRightView(false,true);Require(v.GetViewPlaneNormal()[0]<-.99,"Windows left convention");
        v.SetFrontBackView(false,true);v.UpdateActorScalarField("part",[0,1,2,3,4,5,6,7],null!,null!,true);v.SetScalarBarText("U","All","mm","","");
        adapter.SetDeformation("part",Enumerable.Repeat(0d,24).ToArray(),2);v.SetScalarBarVisibility(false);v.SetScalarBarVisibility(true);
        v.RenderToPNG(Path.Combine(output,"contract.png"));v.RenderToPNG(Path.Combine(output,"contract-2x.png"),false,2);
        Require(File.Exists(Path.Combine(output,"contract-2x.png")),"PNG magnification export");
        bool unsupported=false;try{v.StartBoxZoom();}catch(NotSupportedException e){unsupported=e.Message.Contains("L2");}Require(unsupported,"unsupported contract fails clearly");
        adapter.RemoveActors(["part"]);Require(!v.ContainsActor("part"),"actor removal");
        var map=typeof(VtkMaxView).GetInterfaceMap(typeof(IViewport3D));
        File.WriteAllText(Path.Combine(output,"contract.json"),JsonSerializer.Serialize(new{hit,renderer=host.View.GlInfo,interfaceMethods=map.InterfaceMethods.Length,
            implementedAccessors=map.TargetMethods.Count(m=>!m.Name.StartsWith("vtkControl.IViewport3D."))},new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));
    }
}
