// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using CaeMesh;
using PrePoMax.Forms;
using vtkControl;

namespace PrePoMax.Avalonia;

public interface IIdSelectionSession
{
    vtkSelectItem SelectItem { get; }
    int[] Ids { get; }
    Selection? CreationData { get; }
    void SelectionChanged(int[] ids, Selection selection);
    bool SelectionVisible { get; }
}

public sealed class NodeSetFormSession : EntityFormSession, IIdSelectionSession
{
    private readonly FeNodeSet set;
    private readonly ViewNodeSet view;
    public override string Kind => "NodeSet";
    public override NamedClass Draft => set;
    public override IReadOnlyList<FormPage> Pages => [new("Node set", view)];
    public vtkSelectItem SelectItem => vtkSelectItem.Node;
    public int[] Ids => set.Labels ?? [];
    public Selection? CreationData => set.CreationData;
    public bool SelectionVisible => true;
    public NodeSetFormSession(Controller c, FeNodeSet? source = null) : base(c, source?.Name)
    {
        set = source?.DeepClone() ?? new FeNodeSet(c.GetAllMeshEntityNames().GetNextNumberedKey("Node_Set"), null);
        view = new ViewNodeSet(null!, set);
    }
    public void SelectionChanged(int[] ids, Selection selection) { set.Labels = ids; set.CreationData = selection.DeepCopy(true, false); Controller.GetNodesCenterOfGravity(set); }
    public override void Validate()
    {
        CheckName(Controller.GetAllMeshEntityNames(), "node set");
        if (Ids.Length == 0) throw new CaeException("The node set selection must contain at least one item.");
    }
    protected override void Publish()
    {
        set.Valid = true;
        if (OriginalName == null) Controller.AddNodeSetCommand(set.DeepClone()); else Controller.ReplaceNodeSetCommand(OriginalName, set.DeepClone());
    }
}

public sealed class ElementSetFormSession : EntityFormSession, IIdSelectionSession
{
    private readonly FeElementSet set;
    private readonly ViewElementSet view;
    public override string Kind => "ElementSet";
    public override NamedClass Draft => set;
    public override IReadOnlyList<FormPage> Pages => [new("Element set", view)];
    public vtkSelectItem SelectItem => vtkSelectItem.Element;
    public int[] Ids => set.Labels ?? [];
    public Selection? CreationData => set.CreationData;
    public bool SelectionVisible => true;
    public ElementSetFormSession(Controller c, FeElementSet? source = null) : base(c, source?.Name)
    {
        set = source?.DeepClone() ?? new FeElementSet(c.GetAllMeshEntityNames().GetNextNumberedKey("Element_Set"), null);
        view = new ViewElementSet(set);
    }
    public void SelectionChanged(int[] ids, Selection selection) { set.Labels = ids; set.CreationData = selection.DeepCopy(true, false); }
    public override void Validate()
    {
        CheckName(Controller.GetAllMeshEntityNames(), "element set");
        if (Ids.Length == 0) throw new CaeException("The element set selection must contain at least one item.");
    }
    protected override void Publish()
    {
        set.Valid = true;
        if (OriginalName == null) Controller.AddElementSetCommand(set.DeepClone()); else Controller.ReplaceElementSetCommand(OriginalName, set.DeepClone());
    }
}

public sealed class SurfaceFormSession : EntityFormSession, IIdSelectionSession
{
    private readonly FeSurface surface;
    private readonly ViewFeSurface view;
    public override string Kind => "Surface";
    public override NamedClass Draft => surface;
    public override IReadOnlyList<FormPage> Pages => [new("Surface", view)];
    public vtkSelectItem SelectItem => vtkSelectItem.Surface;
    public int[] Ids => surface.FaceIds ?? [];
    public Selection? CreationData => surface.CreationData;
    public bool SelectionVisible => surface.CreatedFrom == FeSurfaceCreatedFrom.Selection;
    public SurfaceFormSession(Controller c, FeSurface? source = null) : base(c, source?.Name)
    {
        surface = source?.DeepClone() ?? new FeSurface(c.GetAllMeshEntityNames().GetNextNumberedKey("Surface"));
        view = new ViewFeSurface(surface); view.PopulateDropDownList(c.GetUserNodeSetNames());
        if (!SelectionVisible && c.GetUserNodeSetNames().Contains(surface.CreatedFromNodeSetName)) c.UpdateSurfaceArea(surface);
    }
    public override void Changed(string property)
    {
        if (surface.CreatedFrom == FeSurfaceCreatedFrom.NodeSet && surface.CreatedFromNodeSetName != null) Controller.UpdateSurfaceArea(surface);
    }
    public void SelectionChanged(int[] ids, Selection selection)
    { surface.FaceIds = ids; surface.CreationData = selection.DeepCopy(true, false); if (ids.Length > 0) Controller.UpdateSurfaceArea(surface); else surface.Area = 0; }
    public override void Validate()
    {
        CheckName(Controller.GetAllMeshEntityNames(), "surface");
        if (SelectionVisible && Ids.Length == 0) throw new CaeException("The surface selection must contain at least one item.");
        if (!SelectionVisible && !Controller.GetUserNodeSetNames().Contains(surface.CreatedFromNodeSetName)) throw new CaeException("The surface node set does not exist.");
        Controller.UpdateSurfaceArea(surface);
    }
    protected override void Publish()
    {
        surface.Valid = true;
        if (OriginalName == null) Controller.AddSurfaceCommand(surface.DeepClone()); else Controller.ReplaceSurfaceCommand(OriginalName, surface.DeepClone());
    }
}
