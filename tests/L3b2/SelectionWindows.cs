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

static class SelectionWindows
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, All).Invoke(obj,args);
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    [STAThread] static int Main(string[] args)
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var c = new Controller(new NullMainView()); c.Open(args[0]);
            using (var form = new Form { ClientSize = new System.Drawing.Size(1000,700), ShowInTaskbar = false })
            using (var view = new vtkControl.vtkControl { Dock = DockStyle.Fill })
            {
                form.Controls.Add(view); form.Show(); Application.DoEvents();
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
                Console.WriteLine("PASS Windows frustum snapshot: "+args[0]);
            }
            return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
