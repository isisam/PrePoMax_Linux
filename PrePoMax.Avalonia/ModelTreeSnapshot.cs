// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeResults;
using System.Reflection;

namespace PrePoMax.Avalonia;

/// <summary>Stable entity identities, independent of translated captions and UI controls.</summary>
public sealed record TreeEntry(string Key, string Caption, string Kind, ViewGeometryModelResults View,
    NamedClass? Entity = null, string? Step = null, TreeEntry[]? Items = null)
{
    public TreeEntry[] Children => Items ?? [];
}

public static class ModelTreeSnapshot
{
    public static TreeEntry[] Build(Controller controller, ViewGeometryModelResults view, FeResults? results = null)
    {
        var m = controller.Model;
        TreeEntry Group(string title, string kind, IEnumerable<NamedClass>? values, string? step = null) =>
            new($"{view}/{step}/{kind}", title, kind, view, Step: step,
                Items: values?.Where(n => !n.Internal).Select(n => new TreeEntry($"{view}/{step}/{kind}/{n.Name}", n.Name, kind, view, n, step)).ToArray() ?? []);
        TreeEntry Branch(string title, params TreeEntry[] items) => new($"{view}/{title}", title, "Category", view, Items: items);
        TreeEntry MeshTree(FeMesh? mesh) => Branch("Mesh", Group("Parts", "Part", mesh?.Parts.Values),
            Group("Node Sets", "NodeSet", mesh?.NodeSets.Values), Group("Element Sets", "ElementSet", mesh?.ElementSets.Values),
            Group("Surfaces", "Surface", mesh?.Surfaces.Values));
        TreeEntry Features(FeMesh? mesh) => Branch("Features", Group("Reference Points", "ReferencePoint", mesh?.ReferencePoints.Values),
            Group("Coordinate Systems", "CoordinateSystem", mesh?.CoordinateSystems.Values));
        if (view == ViewGeometryModelResults.Geometry)
            return [Group("Parts", "Part", m.Geometry?.Parts.Values), Group("Mesh Setup Items", "MeshSetupItem", m.Geometry?.MeshSetupItems.Values)];
        if (view == ViewGeometryModelResults.Results)
        {
            results ??= controller.CurrentResult;
            var fields = results?.GetVisibleFieldsAsNamedItems() ?? [];
            var history = results?.GetHistory() == null ? [] : results.GetHistoryOutputsAsNamedItems();
            return [Branch("Model", MeshTree(results?.Mesh), Features(results?.Mesh)),
                Branch("Results", Group("Field Outputs", "ResultField", fields), Group("History Outputs", "ResultHistory", history))];
        }
        var steps = m.StepCollection.StepsList.Select(s => new TreeEntry($"Model/Step/{s.Name}", s.Name, "Step", view, s, s.Name,
            [Group("Field Outputs", "FieldOutput", s.FieldOutputs.Values, s.Name), Group("History Outputs", "HistoryOutput", s.HistoryOutputs.Values, s.Name),
             Group("BCs", "BoundaryCondition", s.BoundaryConditions.Values, s.Name), Group("Loads", "Load", s.Loads.Values, s.Name),
             Group("Defined Fields", "DefinedField", s.DefinedFields.Values, s.Name)])).ToArray();
        return [Branch("Model", MeshTree(m.Mesh), Features(m.Mesh), Group("Materials", "Material", m.Materials.Values),
            Group("Composite Layups", "CompositeLayup", m.CompositeLayups.Values), Group("Sections", "Section", m.Sections.Values),
            Group("Constraints", "Constraint", m.Constraints.Values), Branch("Contacts", Group("Surface Interactions", "SurfaceInteraction", m.SurfaceInteractions.Values),
                Group("Contact Pairs", "ContactPair", m.ContactPairs.Values)), Group("Distributions", "Distribution", m.Distributions.Values),
            Group("Amplitudes", "Amplitude", m.Amplitudes.Values), Group("Initial Conditions", "InitialCondition", m.InitialConditions.Values),
            new("Model/Steps", "Steps", "Step", view, Items: steps)), Group("Analyses", "Job", controller.Jobs.Values)];
    }

    public static IEnumerable<TreeEntry> Flatten(IEnumerable<TreeEntry> entries) => entries.SelectMany(e => new[] { e }.Concat(Flatten(e.Children)));

    // These names are the existing Controller command entry points, never raw dictionary mutations.
    private static readonly Dictionary<string, string> Plurals = new()
    {
        ["Material"]="Materials", ["Section"]="Sections", ["CompositeLayup"]="CompositeLayups", ["NodeSet"]="NodeSets",
        ["ElementSet"]="ElementSets", ["Surface"]="Surfaces", ["Step"]="Steps", ["BoundaryCondition"]="BoundaryConditions",
        ["Load"]="Loads", ["Constraint"]="Constraints", ["ContactPair"]="ContactPairs", ["SurfaceInteraction"]="SurfaceInteractions",
        ["Distribution"]="Distributions", ["Amplitude"]="Amplitudes", ["InitialCondition"]="InitialConditions", ["FieldOutput"]="FieldOutputs",
        ["HistoryOutput"]="HistoryOutputs", ["DefinedField"]="DefinedFieldsForStep", ["Job"]="Jobs", ["MeshSetupItem"]="MeshSetupItems"
    };
    private static string? CommandName(TreeEntry e, string action)
    {
        if (e.Entity == null) return null;
        if (e.Kind == "Part") return action is "Hide" or "Show" or "Delete" ?
            (action == "Delete" ? "Remove" : action) + (e.View == ViewGeometryModelResults.Geometry ? "Geometry" : e.View == ViewGeometryModelResults.Results ? "Result" : "Model") + "PartsCommand" : null;
        if (e.Kind is "ReferencePoint" or "CoordinateSystem") return (action == "Delete" ? "Remove" : action) +
            (e.View == ViewGeometryModelResults.Results ? "Result" : "Model") + e.Kind + "sCommand";
        if (e.View == ViewGeometryModelResults.Results) return null;
        if (e.Kind == "CompositeLayup" && action == "Delete") return "RemoveCompositeLayupCommand";
        if (e.Kind == "DefinedField" && action == "Delete") return "RemoveDefinedFieldsCommand";
        if (action is "Hide" or "Show")
        {
            if (e.Kind == "BoundaryCondition") return action + "BoundaryConditionCommand";
            if (e.Kind == "Load") return action + "LoadsCommand";
            if (e.Kind == "Constraint") return action + "ConstraintsCommand";
            if (e.Kind == "ContactPair") return action + "ContactPairsCommand";
            if (e.Kind == "InitialCondition") return action + "InitialConditionsCommand";
            if (e.Kind == "DefinedField") return action + "DefinedFieldsCommand";
            return null;
        }
        return Plurals.TryGetValue(e.Kind, out var plural) ? (action == "Delete" ? "Remove" : action) + plural + "Command" : null;
    }
    private static bool Activatable(TreeEntry e) => e.View != ViewGeometryModelResults.Results && e.Entity != null &&
        e.Kind is "Material" or "CompositeLayup" or "Part" or "MeshSetupItem" or "Constraint" or "ContactPair" or "InitialCondition" or "Step" or "FieldOutput" or "HistoryOutput" or "BoundaryCondition" or "Load" or "DefinedField" &&
        (e.Kind != "Part" || e.View == ViewGeometryModelResults.Model);

    public static bool CanExecute(TreeEntry e, string action) => action is "Activate" or "Deactivate" ? Activatable(e) :
        action is "Duplicate" or "Delete" or "Hide" or "Show" && CommandName(e, action) is { } name && typeof(Controller).GetMethods().Any(m => m.Name == name);

    public static void Execute(Controller controller, TreeEntry e, string action)
    {
        if (!CanExecute(e, action)) throw new InvalidOperationException($"{action} is unavailable for {e.Kind}");
        if (action is "Activate" or "Deactivate")
        { controller.ActivateDeactivateMultipleCommand([e.Entity!], action == "Activate", [e.Step!]); return; }
        var method = typeof(Controller).GetMethods().Single(m => m.Name == CommandName(e, action));
        object[] arguments = e.Kind == "CompositeLayup" && action == "Delete" ? [e.Entity!.Name] :
            e.Step != null && e.Kind != "Step" ? [e.Step, new[] { e.Entity!.Name }] : [new[] { e.Entity!.Name }];
        try { method.Invoke(controller, arguments); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
}
