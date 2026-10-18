// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using PrePoMax;
using vtkControl;

static partial class SelectionWindows
{
    static int Regions(string fixture,string output)
    {
        Directory.CreateDirectory(output);
        var host=new RegionHost();var c=new Controller(host);c.Open(fixture);
        c.CurrentView=ViewGeometryModelResults.Model;
        const string step="L3RegionsStep",point="L3RegionsRP";
        c.AddStepCommand(new StaticStep(step),false);
        var center=c.Model.Mesh.Nodes.Values.First().Coor;
        c.AddModelReferencePointCommand(new FeReferencePoint(point,center[0],center[1],center[2]));
        var nodes=c.Model.Mesh.NodeSets["L3SelectNodes"];
        var surface=c.Model.Mesh.Surfaces["L3SelectSurface"];
        c.AddBoundaryConditionCommand(step,new FixedBC("L3RegionFixedNodes",nodes.Name,RegionTypeEnum.NodeSetName,false));
        c.AddBoundaryConditionCommand(step,new FixedBC("L3RegionFixedSurface",surface.Name,RegionTypeEnum.SurfaceName,false));
        c.AddBoundaryConditionCommand(step,new FixedBC("L3RegionFixedPoint",point,RegionTypeEnum.ReferencePointName,false));
        c.AddLoadCommand(step,new CLoad("L3RegionForce",nodes.Name,RegionTypeEnum.NodeSetName,0,1,0,false,false,0));
        c.AddLoadCommand(step,new MomentLoad("L3RegionMoment",point,RegionTypeEnum.ReferencePointName,0,1,0,false,false,0));
        c.AddLoadCommand(step,new DLoad("L3RegionPressure",surface.Name,RegionTypeEnum.SurfaceName,1,false,false,0));
        var win=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"PrePoMax.exe"));
        var selection=Activator.CreateInstance(win.GetType("PrePoMax.FrmSelectItemSet"),c);
        var records=new List<object>();
        foreach(var name in new[]{"L3RegionFixedNodes","L3RegionFixedSurface","L3RegionFixedPoint","L3RegionForce","L3RegionMoment","L3RegionPressure"})
        {
            bool bc=name.Contains("Fixed");
            var form=Activator.CreateInstance(win.GetType("PrePoMax.Forms.Frm"+(bc?"BC":"Load")),c);
            win.GetType("PrePoMax.ItemSetDataEditor").GetMethod("SetForms",BindingFlags.Public|BindingFlags.Static).Invoke(null,new[]{selection,(object)false,form});
            Call(form,"PrepareForm",step,name);host.Nodes.Clear();host.Cells.Clear();c.ClearSelectionBuffer();Call(form,"Highlight");
            var entity=bc?(IMultiRegion)c.GetBoundaryCondition(step,name):c.GetLoad(step,name);
            var drawnNodes=host.Nodes.SelectMany(d=>d.Geometry.Nodes.Coor).ToArray();
            var drawnFaces=host.Cells.Where(d=>d.Geometry.Cells.Types!=null).SelectMany(d=>d.Geometry.Cells.Types.Select((t,i)=>new{t,i,d}))
                .Where(x=>new[]{5,9,22,23}.Contains(x.t)).Select(x=>x.d.Geometry.Cells.CellNodeIds[x.i].Select(i=>x.d.Geometry.Nodes.Coor[i]).ToArray()).ToArray();
            records.Add(new{name,kind=bc?"BoundaryCondition":"Load",regionType=entity.RegionType.ToString(),region=entity.RegionName,
                nodes=entity.RegionType==RegionTypeEnum.NodeSetName?nodes.Labels.OrderBy(i=>i).ToArray():new int[0],
                faces=entity.RegionType==RegionTypeEnum.SurfaceName?surface.FaceIds.OrderBy(i=>i).ToArray():new int[0],
                point=entity.RegionType==RegionTypeEnum.ReferencePointName?center:null,drawnNodes,drawnFaces});
            Call(form,"OnHideOrClose");((IDisposable)form).Dispose();
        }
        c.SaveToPmx(Path.Combine(output,"regions.pmx"));
        File.WriteAllText(Path.Combine(output,"windows-regions.json"),Json.Serialize(new{step,records,private_desktop=true,real_windows_forms=true}),new UTF8Encoding(false));
        ((IDisposable)selection).Dispose();Console.WriteLine("PASS Windows region fixture and real form highlight snapshots");return 0;
    }
}
sealed class RegionHost:NullMainView
{
    public readonly List<vtkMaxActorData> Nodes=new List<vtkMaxActorData>(),Cells=new List<vtkMaxActorData>();
    public override IViewportActor Add3DNodes(vtkMaxActorData data){Nodes.Add(data);return base.Add3DNodes(data);}
    public override IViewportActor Add3DCells(vtkMaxActorData data){Cells.Add(data);return base.Add3DCells(data);}
    public override void Add3DCells(vtkMaxActorData[] data){Cells.AddRange(data);base.Add3DCells(data);}
}
