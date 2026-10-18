// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
namespace PrePoMax.Avalonia;
public sealed partial class VtkMaxView
{
    internal void SetPickGeometry(string name,PartExchangeData geometry)
    {
        var cells=geometry.Cells.CellNodeIds;var offsets=new int[cells.Length+1];for(int i=0;i<cells.Length;++i)offsets[i+1]=offsets[i]+cells[i].Length;
        Change(v=>v.ActorPickGeometry(actors[name].Id,geometry.Nodes.Ids.Select(i=>(long)i).ToArray(),geometry.Nodes.Coor.SelectMany(p=>p).ToArray(),
            geometry.Cells.Ids.Select(i=>(long)i).ToArray(),geometry.Cells.Types,offsets,cells.SelectMany(c=>c.Select(i=>(long)i)).ToArray()));
    }
    private void SendArea(int x1,int y1,int x2,int y2,vtkSelectOperation operation)
    {
        var area=Native.AreaPick(x1,y1,x2,y2);
        var names=area.Actors.Select(id=>actors.Values.FirstOrDefault(a=>a.Id==id)?.Data.Name).Where(n=>n!=null).Cast<string>().ToArray();
        OnMouseLeftButtonUpSelection?.Invoke(null!,null!,names.Length==0?null!:area.Planes,x1>x2,operation,names);
    }
    public void SetSelectableActorsFilter(string[]? names)
        => Native.SelectableFilter(names?.Where(actors.ContainsKey).Select(n=>actors[n].Id).ToArray());
    public bool IsBoxSelectionActive=>View?.BoxSelectionActive??false;
    public void GetClosestCellsOnActors(double[] point,string[] actorNames,out double[] distances,out double[][] normals)
    {var result=Native.ClosestCells(point,actorNames.Select(n=>actors.TryGetValue(n,out var a)?a.Id:-1).ToArray());distances=result.Distances;normals=result.Normals;}
    private int selectionDistance=7;
    public double GetSelectionPrecision()=>View?.SelectionPrecision??0;
    public void SetSelectionDistance(int distanceInPixels){selectionDistance=Math.Max(1,distanceInPixels);Execute(v=>v.SelectionDistance(selectionDistance));}
    public void GetPointAndCellIdsInsideFrustum(double[][] planes,out int[] pointIds,out int[] cellIds)
    {
        var ids=Native.FrustumSelect(planes);pointIds=ids.Points.Select(id=>checked((int)id)).ToArray();cellIds=ids.Cells.Select(id=>checked((int)id)).ToArray();
    }
    public void GetGeometryPickProperties(double[] point,out int elementId,out int[] edgeNodeIds,out int[] cellFaceNodeIds)
    {
        var pick=Native.GeometryPick(point,!RenderingOn||!UserPick);elementId=checked((int)pick.Element);
        edgeNodeIds=pick.Edge.Length==0?null!:pick.Edge.Select(id=>checked((int)id)).ToArray();
        cellFaceNodeIds=pick.Face.Length==0?null!:pick.Face.Select(id=>checked((int)id)).ToArray();
    }
    public int[][] GetVisualizationCellEdges(int nodes)=>nodes switch
    {
        3=>[[0,1],[1,2],[2,0]],4=>[[0,1],[1,2],[2,3],[3,0]],
        6=>[[0,3,1],[1,4,2],[2,5,0]],8=>[[0,4,1],[1,5,2],[2,6,3],[3,7,0]],
        _=>throw new NotSupportedException("Face edges require 3, 4, 6 or 8 nodes.")
    };
}
