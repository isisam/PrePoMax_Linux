// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using vtkControl;

namespace PrePoMax.Avalonia;

public sealed partial class MainWindow
{
    private EntityEditorWindow? activeEditor;
    private static bool CanEdit(TreeEntry entry) => entry.View == ViewGeometryModelResults.Model && (entry.Entity switch
    {
        Material m => m.Properties.All(p => p is Density or Elastic or ElasticEngineeringConstants or ElasticLamina or ElasticWithDensity or Plastic or ThermalExpansion or ThermalConductivity or SpecificHeat or SlipWear or UserMaterial),
        SolidSection or FeNodeSet or FeElementSet or FeSurface or FixedBC or DisplacementRotation or CLoad or MomentLoad or DLoad => true,
        StaticStep s => s.GetType() == typeof(StaticStep), _ => false
    });
    private static string[] NewFormTypes(string kind) => kind switch
    {
        "Material" => ["Material"], "Section" => ["Solid Section"], "NodeSet" => ["Node set"], "ElementSet" => ["Element set"], "Surface" => ["Surface"],
        "Step" => ["Static step"], "BoundaryCondition" => ["Fixed", "Displacement / Rotation"], "Load" => ["Concentrated force", "Moment", "Pressure"], _ => []
    };
    private EntityFormSession CreateSession(TreeEntry entry)
    {
        var c = mainView.Controller;
        return entry.Entity switch
        {
            Material x => new MaterialFormSession(c, x), SolidSection x => new SectionFormSession(c, x),
            FeNodeSet x => new NodeSetFormSession(c, x), FeElementSet x => new ElementSetFormSession(c, x), FeSurface x => new SurfaceFormSession(c, x),
            StaticStep x when x.GetType() == typeof(StaticStep) => new StepFormSession(c, x),
            BoundaryCondition x => new BoundaryFormSession(c, entry.Step!, x), Load x => new LoadFormSession(c, entry.Step!, x),
            _ => throw new NotSupportedException(entry.Kind)
        };
    }
    private void OpenNewEditor(TreeEntry category, string type)
    {
        try
        {
            var c = mainView.Controller;
            ShowEntityEditor(category.Kind switch
            {
                "Material" => new MaterialFormSession(c), "Section" => new SectionFormSession(c), "NodeSet" => new NodeSetFormSession(c),
                "ElementSet" => new ElementSetFormSession(c), "Surface" => new SurfaceFormSession(c), "Step" => new StepFormSession(c),
                "BoundaryCondition" => new BoundaryFormSession(c, category.Step!, displacement: type != "Fixed"),
                "Load" => new LoadFormSession(c, category.Step!, type: type), _ => throw new NotSupportedException(type)
            }, () => OpenNewEditor(category, type));
        }
        catch (Exception error) { Log(error.ToString()); SetStatus(error.Message); }
    }
    private void ShowEntityEditor(EntityFormSession session, Action? addNew = null)
    {
        if (activeEditor != null) { activeEditor.Activate(); return; }
        var dialog = activeEditor = new EntityEditorWindow(session, mainView.Controller);
        dialog.Applied += () => { RefreshModelTrees(); Log("FORM " + session.Kind + " " + session.Draft.Name + " commands=" + mainView.Controller.GetActiveCommandCount()); };
        dialog.DraftChanged += () => HighlightDraft(session);
        dialog.AddNewRequested += () => global::Avalonia.Threading.Dispatcher.UIThread.Post(() => addNew?.Invoke());
        dialog.Closed += (_, _) => { activeEditor = null; viewport.RemoveActors(["__form_reference_point","__selection_faces"]); viewport.ClearSelection(); };
        if(session is IIdSelectionSession)dialog.Show(this);else _ = dialog.ShowDialog(this);
        HighlightDraft(session);
    }
    private void ViewportSelection(double[] point,double[] direction,double[][] planes,bool completelyInside,vtkSelectOperation operation,string[] names)
    {
        if(activeEditor?.SelectionPanel is {IsVisible:true} panel){panel.SelectPointOrArea(point,direction,planes,completelyInside,operation,names);return;}
        if(viewport.SelectBy==vtkSelectBy.Default && planes!=null && DisplayedMesh is {} mesh)
        {
            var parts=names.Where(mesh.Parts.ContainsKey).Select(n=>mesh.Parts[n]).ToArray();
            if(parts.Length==0)return;
            SelectPickedPart(new Vtkmax.Interop.PickResult {Hit=1,ElementId=parts[0].Labels[0]});
            viewport.Execute(v=>{v.Highlight(parts.SelectMany(p=>p.Labels).Distinct().Select(i=>(long)i).ToArray());v.Render();});
        }
    }
    private void HighlightDraft(EntityFormSession session)
    {
        try
        {
            var c = mainView.Controller; NamedClass entity = session.Draft;
            viewport.RemoveActors(["__form_reference_point","__selection_faces"]);
            highlightedFaces=[];highlightedNodes=[];highlightedElements=[];highlightedPoint=null;
            if (entity is Section section && section.RegionType is RegionTypeEnum.PartName or RegionTypeEnum.ElementSetName)
                entity = section.RegionType == RegionTypeEnum.PartName ? c.Model.Mesh.Parts[section.RegionName] : c.Model.Mesh.ElementSets[section.RegionName];
            else if (entity is Section selectedSection && selectedSection.RegionType == RegionTypeEnum.Selection)
                entity = new FeElementSet("selection", CompositeLayupExpansion.ResolveRegion(c.Model, selectedSection));
            if (entity is BoundaryCondition bc && bc.RegionType == RegionTypeEnum.NodeSetName) entity = c.Model.Mesh.NodeSets[bc.RegionName];
            else if (entity is BoundaryCondition surfaceBc && surfaceBc.RegionType == RegionTypeEnum.SurfaceName) entity = c.Model.Mesh.Surfaces[surfaceBc.RegionName];
            else if (entity is BoundaryCondition pointBc && pointBc.RegionType == RegionTypeEnum.ReferencePointName) entity = c.GetModelReferencePoint(pointBc.RegionName);
            if (entity is Load load && load.RegionType == RegionTypeEnum.NodeSetName) entity = c.Model.Mesh.NodeSets[load.RegionName];
            else if (entity is Load surfaceLoad && surfaceLoad.RegionType == RegionTypeEnum.SurfaceName) entity = c.Model.Mesh.Surfaces[surfaceLoad.RegionName];
            else if (entity is Load pointLoad && pointLoad.RegionType == RegionTypeEnum.ReferencePointName) entity = c.GetModelReferencePoint(pointLoad.RegionName);
            else if (entity is Load selectedLoad && selectedLoad.RegionType == RegionTypeEnum.Selection)
                entity = session is IIdSelectionSession { SelectItem: CaeGlobals.vtkSelectItem.Surface } surfaceSelection
                    ? new FeSurface("selection") { FaceIds = surfaceSelection.Ids } : new FeNodeSet("selection", selectedLoad.CreationIds);
            if (entity is BoundaryCondition selectedBc && selectedBc.RegionType == RegionTypeEnum.Selection)
                entity = new FeNodeSet("selection", selectedBc.CreationIds);
            if (entity is FeReferencePoint point)
            {
                viewport.ClearSelection();
                var marker = new vtkMaxActorData { Name = "__form_reference_point", Pickable = false, Layer = vtkRendererLayer.Selection, Color = System.Drawing.Color.Yellow };
                marker.Geometry.Nodes = new() { Ids = [0], Coor = [[point.X.Value, point.Y.Value, point.Z.Value]] };
                viewport.AddPoints(marker);highlightedPoint=[point.X.Value,point.Y.Value,point.Z.Value]; SetStatus(point.Name + ": reference point"); return;
            }
            SelectTreeEntity(new TreeEntry("draft", entity.Name, session.Kind, ViewGeometryModelResults.Model, entity));
        }
        catch (Exception error) { Log("Form highlight: " + error.Message); }
    }
}
