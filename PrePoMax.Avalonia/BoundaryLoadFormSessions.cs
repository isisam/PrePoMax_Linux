// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using CaeModel;
using CaeMesh;
using vtkControl;

namespace PrePoMax.Avalonia;

public sealed class BoundaryFormSession : EntityFormSession, IIdSelectionSession
{
    protected override bool ReplaceNeeded => PropertyItemChanged || !bc.Valid;
    private readonly BoundaryCondition bc;
    private readonly ViewBoundaryCondition view;
    private readonly string stepName;
    public override string Kind => "BoundaryCondition";
    public override NamedClass Draft => bc;
    public override IReadOnlyList<FormPage> Pages => [new(bc is FixedBC ? "Fixed" : "Displacement / Rotation", view)];
    public vtkSelectItem SelectItem => vtkSelectItem.Node;
    public int[] Ids => bc.CreationIds ?? [];
    public Selection? CreationData => bc.CreationData;
    public bool SelectionVisible => bc.RegionType == RegionTypeEnum.Selection;
    public BoundaryFormSession(Controller c, string step, BoundaryCondition? source = null, bool displacement = false) : base(c, source?.Name)
    {
        stepName = step;
        bool twoD = c.Model.Properties.ModelSpace.IsTwoD();
        string name = c.GetBoundaryConditionNamesForStep(step).GetNextNumberedKey(displacement ? "Displacement_Rotation" : "Fixed");
        string? region = c.GetUserNodeSetNames().FirstOrDefault();
        var type = region == null ? RegionTypeEnum.Selection : RegionTypeEnum.NodeSetName;
        bc = source?.DeepClone() ?? (displacement ? new DisplacementRotation(name, region!, type, twoD, false, 0) : new FixedBC(name, region!, type, twoD));
        if (source?.CreationData != null && (!c.Model.IsBoundaryConditionRegionValid(bc) || !c.Model.RegionValid(bc)))
        { bc.CreationData = null; bc.CreationIds = null; bc.RegionType = RegionTypeEnum.Selection; MarkChanged(); }
        if (source?.CreationData != null) bc.RegionType = RegionTypeEnum.Selection;
        if (bc is FixedBC fixedBc)
        {
            var v = new ViewFixedBC(fixedBc); v.PopulateDropDownLists(c.GetUserNodeSetNames(), c.GetUserSurfaceNames(), c.GetModelReferencePointNames()); view = v;
        }
        else if (bc is DisplacementRotation dr)
        {
            var v = new ViewDisplacementRotation(dr); v.PopulateDropDownLists(c.GetUserNodeSetNames(), c.GetUserSurfaceNames(), c.GetModelReferencePointNames(),
                c.GetDistributionNames(), c.GetAmplitudeNames(), c.GetModelCoordinateSystemNames()); view = v;
        }
        else throw new NotSupportedException("Unsupported boundary condition " + bc.GetType().Name);
    }
    public override void Changed(string property) { base.Changed(property); view.UpdateRegionVisibility(); view.UpdateVisibility(); }
    public void SelectionChanged(int[] ids, Selection selection) { bc.CreationIds = ids; bc.CreationData = selection.DeepCopy(true, false); }
    public string? Confirmation() => bc is DisplacementRotation dr && dr.RegionType == RegionTypeEnum.ReferencePointName &&
        dr.CoordinateSystemName != CoordinateSystem.DefaultCoordinateSystemName
        ? "A coordinate system will not be taken into account if applied on a reference point." : null;
    public override void Validate()
    {
        CheckName(Controller.GetBoundaryConditionNamesForStep(stepName), "boundary condition"); bc.CheckEquations();
        if (SelectionVisible && Ids.Length == 0) throw new CaeException("The boundary condition selection must contain at least one item.");
        if (bc is DisplacementRotation distributed && distributed.DistributionName != Distribution.DefaultDistributionName &&
            !distributed.IsDistributionTypeSupported(Controller.GetDistribution(distributed.DistributionName).DistributionType, out string distributionError))
            throw new CaeException(distributionError);
        if (bc is DisplacementRotation dr && !dr.IsProperlyDefined(out string error)) throw new CaeException(error);
        if (!Controller.GetStep(stepName).IsBoundaryConditionSupported(bc)) throw new CaeException("The step does not support this boundary condition.");
    }
    protected override void Publish()
    {
        if (OriginalName == null) Controller.AddBoundaryConditionCommand(stepName, bc.DeepClone());
        else Controller.ReplaceBoundaryConditionCommand(stepName, OriginalName, bc.DeepClone());
        bc.Valid = Controller.GetBoundaryCondition(stepName, bc.Name).Valid;
    }
}

public sealed class LoadFormSession : EntityFormSession, IIdSelectionSession
{
    private readonly Load load;
    private readonly ViewLoad view;
    private readonly string stepName;
    public override string Kind => "Load";
    public override NamedClass Draft => load;
    public override IReadOnlyList<FormPage> Pages => [new(load is CLoad ? "Concentrated force" : load is MomentLoad ? "Moment" : "Pressure", view)];
    public vtkSelectItem SelectItem => load is DLoad ? vtkSelectItem.Surface : vtkSelectItem.Node;
    public int[] Ids => load.CreationIds ?? [];
    public Selection? CreationData => load.CreationData;
    public bool SelectionVisible => load.RegionType == RegionTypeEnum.Selection;
    public LoadFormSession(Controller c, string step, Load? source = null, string type = "Concentrated force") : base(c, source?.Name)
    {
        stepName = step;
        bool twoD = c.Model.Properties.ModelSpace.IsTwoD();
        string name = c.GetLoadNamesForStep(step).GetNextNumberedKey(type.Replace(' ', '_'));
        bool pressure = type == "Pressure"; string? region = (pressure ? c.GetUserSurfaceNames(FeSurfaceType.Element) : c.GetUserNodeSetNames()).FirstOrDefault();
        var regionType = region == null ? RegionTypeEnum.Selection : pressure ? RegionTypeEnum.SurfaceName : RegionTypeEnum.NodeSetName;
        load = source?.DeepClone() ?? type switch
        {
            "Moment" => new MomentLoad(name, region!, regionType, 0, 0, 0, twoD, false, 0),
            "Pressure" => new DLoad(name, region!, regionType, 0, twoD, false, 0),
            _ => new CLoad(name, region!, regionType, 0, 0, 0, twoD, false, 0)
        };
        if (source?.CreationData != null && (!c.Model.IsLoadRegionValid(load) || !c.Model.RegionValid(load)))
        { load.CreationData = null; load.CreationIds = null; load.RegionType = RegionTypeEnum.Selection; MarkChanged(); }
        if (source?.CreationData != null) load.RegionType = RegionTypeEnum.Selection;
        if (load is CLoad cl)
        {
            var v = new ViewCLoad(cl); v.PopulateDropDownLists(c.GetUserNodeSetNames(), c.GetModelReferencePointNames(), c.GetAmplitudeNames(), c.GetModelCoordinateSystemNames()); view = v;
        }
        else if (load is MomentLoad ml)
        {
            var v = new ViewMomentLoad(ml); v.PopulateDropDownLists(c.GetUserNodeSetNames(), c.GetModelReferencePointNames(), c.GetAmplitudeNames()); view = v;
        }
        else if (load is DLoad dl)
        {
            var v = new ViewDLoad(dl); v.PopulateDropDownLists(c.GetUserSurfaceNames(FeSurfaceType.Element), c.GetDistributionNames(), c.GetAmplitudeNames()); view = v;
        }
        else throw new NotSupportedException("Unsupported load " + load.GetType().Name);
    }
    public override void Changed(string property) { base.Changed(property); view.UpdateRegionVisibility(); }
    public void SelectionChanged(int[] ids, Selection selection) { load.CreationIds = ids; load.CreationData = selection.DeepCopy(true, false); }
    public string? Confirmation()
    {
        if (load is not CLoad && load is not MomentLoad) return null;
        int count = SelectionVisible ? Ids.Length : load.RegionType == RegionTypeEnum.NodeSetName ? Controller.Model.Mesh.NodeSets[load.RegionName].Labels.Length : 0;
        return count > 5 ? "The " + (load is CLoad ? "concentrated force" : "moment") + " load will apply the entered load magnitude to all selected nodes. Continue?" : null;
    }
    public override void Validate()
    {
        CheckName(Controller.GetLoadNamesForStep(stepName), "load"); load.CheckEquations();
        if (SelectionVisible && Ids.Length == 0) throw new CaeException("The load selection must contain at least one item.");
        if (load is CLoad cl && cl.Magnitude.Value == 0) throw new CaeException("At least one force component must not be equal to 0.");
        if (load is MomentLoad ml && ml.Magnitude.Value == 0) throw new CaeException("At least one moment component must not be equal to 0.");
        if (load is DLoad dl && dl.Magnitude.Value == 0) throw new CaeException("The pressure load magnitude must not be equal to 0.");
        if (!Controller.GetStep(stepName).IsLoadTypeSupported(load.GetType())) throw new CaeException("The step does not support this load.");
    }
    protected override void Publish()
    {
        if (OriginalName == null) Controller.AddLoadCommand(stepName, load.DeepClone());
        else Controller.ReplaceLoadCommand(stepName, OriginalName, load.DeepClone());
    }
}
