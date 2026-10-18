// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using PrePoMax.Viewer;
using vtkControl;
namespace PrePoMax.Avalonia;

internal static class ViewportModelBridge
{
    public static void Load(VtkMaxView viewport, ViewerModel model)
        => LoadMesh(viewport,model.Model.Mesh);
    public static void LoadMesh(VtkMaxView viewport,CaeMesh.FeMesh mesh,bool fit=true)
    {
        viewport.Clear();
        // An invisible complete mesh backs cross-part highlights. Its highlight actor
        // is owned by vtkmax and is not pickable; visible parts remain separate actors.
        if(mesh.Elements.Count>0)
        {
            var allNodes=mesh.Nodes.Keys.Order().ToArray();var allElements=mesh.Elements.Values.OrderBy(e=>e.Id).ToArray();
            var index=allNodes.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>(long)p.i);
            var cells=allElements.Select(e=>e.GetVtkNodeIds()).ToArray();var offsets=new int[cells.Length+1];
            for(int i=0;i<cells.Length;i++)offsets[i+1]=offsets[i]+cells[i].Length;
            viewport.Execute(v=>{v.Load(allNodes.Select(id=>(long)id).ToArray(),allNodes.SelectMany(id=>mesh.Nodes[id].Coor).ToArray(),
                allElements.Select(e=>(long)e.Id).ToArray(),allElements.Select(e=>e.GetVtkCellType()).ToArray(),offsets,cells.SelectMany(c=>c.Select(id=>index[id])).ToArray());
                v.ActorStyle(0,false,0,0,0);});
        }
        foreach(var part in mesh.Parts.Values)
        {
            var elements=part.Labels.Select(id=>mesh.Elements[id]).ToArray();
            var ids=elements.SelectMany(e=>e.NodeIds).Distinct().Order().ToArray();
            var map=ids.Select((id,index)=>(id,index)).ToDictionary(p=>p.id,p=>p.index);
            var data=new vtkMaxActorData { Name=part.Name, IsAPart=true, Pickable=true, Color=part.Color };
            data.Geometry.Nodes=new(){Ids=ids,Coor=ids.Select(id=>{var n=mesh.Nodes[id];return new[]{n.X,n.Y,n.Z};}).ToArray()};
            data.Geometry.Cells=new(){Ids=elements.Select(e=>e.Id).ToArray(),Types=elements.Select(e=>e.GetVtkCellType()).ToArray(),
                CellNodeIds=elements.Select(e=>e.GetVtkNodeIds().Select(id=>map[id]).ToArray()).ToArray()};
            viewport.AddCells(data);
            if(!part.Visible)viewport.HideActors([part.Name],true);
        }
        if(fit)viewport.Standard(0);
    }
    public static void Contour(VtkMaxView viewport,ViewerModel model,string field,double scale)
    {
        var values=model.Contours[field];
        var spectrum=new vtkMaxColorSpectrum();spectrum.SetUserMinMax(values.Min(),values.Max()==values.Min()?values.Min()+1:values.Max());
        spectrum.MinMaxType=vtkColorSpectrumMinMaxType.Manual;spectrum.NumberOfColors=12;
        viewport.SetScalarBarColorSpectrum(spectrum);viewport.SetScalarBarText(field,"","","","");
        var map=model.NodeIds.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>p.i);
        foreach(var actor in viewport.GetVtkMaxActorData())
        {
            var indices=actor.Geometry.Nodes.Ids.Select(id=>map[id]).ToArray();
            viewport.UpdateActorScalarField(actor.Name,indices.Select(i=>(float)values[i]).ToArray(),null!,null!,true);
            viewport.SetDeformation(actor.Name,model.Displacement==null?null:indices.SelectMany(i=>model.Displacement.Skip(3*i).Take(3)).ToArray(),scale);
        }
        viewport.SetZoomToFit(false);
    }
    public static void Deform(VtkMaxView viewport,ViewerModel model,double scale)
    {
        var map=model.NodeIds.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>p.i);
        foreach(var actor in viewport.GetVtkMaxActorData())
            viewport.SetDeformation(actor.Name,model.Displacement==null?null:actor.Geometry.Nodes.Ids.SelectMany(id=>model.Displacement.Skip(3*map[id]).Take(3)).ToArray(),scale);
        viewport.SetZoomToFit(false);
    }
}
