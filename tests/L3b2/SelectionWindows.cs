// SPDX-License-Identifier: GPL-3.0-or-later
// Runs only on the never-visible GUI3 private desktop, with a real ActiViz control.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using CaeGlobals;
using PrePoMax;
using vtkControl;

static partial class SelectionWindows
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, All).Invoke(obj,args);
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    [STAThread] static int Main(string[] args)
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            if(args[0]=="--regions")return Regions(args[1],args[2]);
            var host=new SelectionHost();var c = new Controller(host); c.Open(args[0]);
            using (var form = new Form { ClientSize = new System.Drawing.Size(1000,700), ShowInTaskbar = false })
            using (var view = new vtkControl.vtkControl { Dock = DockStyle.Fill })
            {
                form.Controls.Add(view); form.Show(); Application.DoEvents();
                host.View=view;
                view.Controller_GetVolumeGridData = c.GetVolumeGridData;
                view.Controller_GetCellFaceActorData = c.GetCellFaceActorData;
                var parts = new List<object>(); long actor = 0;
                foreach(var p in c.Model.Mesh.Parts.Values)
                {
                    var visual=c.GetModelPartActorData(c.Model.Mesh,p,vtkRendererLayer.Base);view.AddCells(visual);
                    var g = c.GetVolumeGridData(p.Name,-1);
                    parts.Add(new { actor = ++actor, name=p.Name, visible=p.Visible, nodes=g.Nodes.Ids, xyz=g.Nodes.Coor, elements=g.Cells.Ids, types=g.Cells.Types, cells=g.Cells.CellNodeIds,
                        visual=new {nodes=visual.Geometry.Nodes.Ids,xyz=visual.Geometry.Nodes.Coor,elements=visual.Geometry.Cells.Ids,types=visual.Geometry.Cells.Types,cells=visual.Geometry.Cells.CellNodeIds} });
                    if(!p.Visible)view.HideActors(new[]{p.Name},true);
                }
                var bounds=view.GetBoundingBox();
                var center=new[]{(bounds[0]+bounds[1])/2,(bounds[2]+bounds[3])/2,(bounds[4]+bounds[5])/2};
                double span=Math.Max(bounds[1]-bounds[0],Math.Max(bounds[3]-bounds[2],bounds[5]-bounds[4]));
                var position=new[]{center[0]+span,center[1]+span,center[2]+span};
                double scale=span*.65; var up=new[]{0d,1d,0d};
                view.SetViewParameters(false,position,center,up,scale); Application.DoEvents();
                var cases=new List<object>();
                var rectangles=new[]{new[]{0,0,999,699},new[]{200,200,600,450},new[]{400,300,450,350},new[]{1,1,3,3}};
                int index=0;
                foreach(var r in rectangles)
                {
                    object[] call={r[0],r[1],r[2],r[3],false,null}; Call(view,"PickByArea",call);
                    var picker=view.GetType().GetField("_areaPicker",All).GetValue(view);
                    var frustum=Call(picker,"GetFrustum");
                    var planes=(double[][])view.GetType().GetMethod("GetPlaneParameters",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{frustum});
                    view.GetPointAndCellIdsInsideFrustum(planes,out var nodes,out var elements);
                    cases.Add(new { name="box-"+(index++), rectangle=r, planes, actors=call[5], points=nodes.OrderBy(x=>x).ToArray(), cells=elements.OrderBy(x=>x).ToArray() });
                }
                var target=c.Model.Mesh.Nodes.Values.OrderBy(n=>n.Id).First();
                view.RenderingOn=false;
                view.GetGeometryPickProperties(target.Coor,out var element,out var edge,out var face);
                var result=new { passed=true, fixture=Path.GetFullPath(args[0]), parts, cases,
                    camera=new {position,focal=center,up,scale,width=view.ClientSize.Width,height=view.ClientSize.Height},
                    geometry=new {point=target.Coor,element,edge,face}, private_desktop=true, semantics="real Windows vtkControl, volume provider, six planes fed unchanged to Linux" };
                File.WriteAllText(args[1],Json.Serialize(result),new UTF8Encoding(false));
                if(args.Length>2)Replay(c,view,args[2]);
                Console.WriteLine("PASS Windows frustum snapshot: "+args[0]);
            }
            return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
    static void Replay(Controller c,vtkControl.vtkControl view,string folder)
    {
        var events=(object[])Json.DeserializeObject(File.ReadAllText(Path.Combine(folder,"events.json")));
        var results=new List<object>();string name=null;vtkSelectOperation operation=vtkSelectOperation.None;
        view.RenderingOn=true;view.UserPick=true;view.SetSelectionDistance(7);
        double[] pickedPoint=null;string[] pickedActors=null;
        view.OnMouseLeftButtonUpSelection+=(point,direction,planes,inside,ignored,names)=>{pickedPoint=point;pickedActors=names;c.SelectPointOrArea(point,direction,planes,inside,operation,names);};
        foreach(var item in events.Cast<Dictionary<string,object>>())
        {
            if(item.ContainsKey("command"))
            {
                var command=(Dictionary<string,object>)item["command"];
                if((string)command["op"]=="form-camera")
                {
                    double[] V(string key)=>((object[])command[key]).Select(Convert.ToDouble).ToArray();
                    view.SetViewParameters(false,V("position"),V("focal"),V("up"),Convert.ToDouble(command["scale"]));continue;
                }
                if((string)command["op"]=="form-selection"&&(string)command["button"]=="Undo last")
                {
                    c.Selection.RemoveLast(true);var fUndo=(Dictionary<string,object>)item["form"];
                    var expectedUndo=((object[])fUndo["selection"]).Select(Convert.ToInt32).OrderBy(x=>x).ToArray();var actualUndo=c.GetSelectionIds().OrderBy(x=>x).ToArray();
                    results.Add(new{name,by=c.SelectBy.ToString(),operation="Undo",ids=actualUndo,expected=expectedUndo,equal=actualUndo.SequenceEqual(expectedUndo)});continue;
                }
            }
            if(!item.ContainsKey("real_x11"))continue;
            var f=(Dictionary<string,object>)item["form"];var gesture=(Dictionary<string,object>)item["real_x11"];
            string current=(string)f["name"];
            if(current!=name){c.Selection=new Selection();c.SelectItem=(string)f["kind"]=="NodeSet"?vtkSelectItem.Node:(string)f["kind"]=="ElementSet"?vtkSelectItem.Element:vtkSelectItem.Surface;name=current;}
            var by=(vtkSelectBy)Enum.Parse(typeof(vtkSelectBy),(string)f["selectBy"]);
            if(by.IsGeometryBased()!=c.SelectBy.IsGeometryBased())c.ClearSelectionHistoryAndCallSelectionChanged();
            c.SelectBy=by;view.SelectBy=by;c.SelectAngle=Convert.ToDouble(f["selectAngle"]);
            var mode=(GeometrySelectModeEnum)Enum.Parse(typeof(GeometrySelectModeEnum),(string)f["geometryMode"]);
            if(mode!=c.GeometrySelectMode){c.GeometrySelectMode=mode;c.ClearSelectionHistoryAndCallSelectionChanged();}
            var modifiers=((object[])gesture["modifiers"]).Cast<string>().ToArray();
            bool shift=modifiers.Contains("Shift_L"),ctrl=modifiers.Contains("Control_L");operation=ctrl?(shift?vtkSelectOperation.Intersect:vtkSelectOperation.Subtract):shift?vtkSelectOperation.Add:vtkSelectOperation.None;
            bool box=gesture["rect"]!=null;var r=((object[])(box?gesture["rect"]:gesture["pixel"])).Select(Convert.ToInt32).ToArray();
            view.Pick(r[0],r[1],box,box?r[2]:r[0],box?r[3]:r[1]);
            var actual=c.GetSelectionIds().OrderBy(x=>x).ToArray();var expected=((object[])f["selection"]).Select(Convert.ToInt32).OrderBy(x=>x).ToArray();
            results.Add(new {name,by=by.ToString(),operation=operation.ToString(),pixel=r,pickedPoint,pickedActors,ids=actual,expected,equal=actual.SequenceEqual(expected)});
        }
        bool passed=results.Count>0&&results.All(r=>(bool)r.GetType().GetProperty("equal").GetValue(r));
        File.WriteAllText(Path.Combine(folder,"windows-e2e-comparison.json"),Json.Serialize(new {passed,results,real_activiz=true,same_camera=true}),new UTF8Encoding(false));
        if(!passed)throw new Exception("Windows/Linux end-to-end membership differs: "+folder);
    }
}
sealed class SelectionHost:NullMainView
{
    public vtkControl.vtkControl View;
    public override double GetSelectionPrecision()=>View.GetSelectionPrecision();
    public override void GetGeometryPickProperties(double[] p,out int element,out int[] edge,out int[] face,string[] names=null)
    {try{View.SetSelectableActorsFilter(names);View.GetGeometryPickProperties(p,out element,out edge,out face);}finally{View.SetSelectableActorsFilter(null);}}
    public override void GetPointAndCellIdsInsideFrustum(double[][] planes,string[] names,out int[] nodes,out int[] cells)
    {try{View.SetSelectableActorsFilter(names);View.GetPointAndCellIdsInsideFrustum(planes,out nodes,out cells);}finally{View.SetSelectableActorsFilter(null);}}
    public override void GetClosestCellsOnParts(double[] point,string[] names,out double[] distances,out double[][] normals)=>View.GetClosestCellsOnActors(point,names,out distances,out normals);
}
