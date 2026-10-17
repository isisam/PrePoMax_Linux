using CaeModel;
using Vtkmax.Interop;

namespace PrePoMax.Viewer;

public sealed record PlyGroup(string Name, long[] ElementIds);
public sealed class ViewerModel
{
    public FeModel Model { get; }
    public long[] NodeIds { get; }
    public double[] Coordinates { get; }
    public long[] ElementIds { get; }
    public int[] Types { get; }
    public int[] Offsets { get; }
    public long[] Connectivity { get; }
    public List<PlyGroup> Plies { get; } = [];
    public double[] PlyScalars { get; }
    public string[] RawUserKeywords { get; private set; } = [];
    public string PlySource { get; private set; } = "Composite layup expansion";

    public static ViewerModel Open(string path)
    {
        var model = new FeModel(Path.GetFileNameWithoutExtension(path), null, true);
        model.Properties.ModelSpace = ModelSpaceEnum.ThreeD;
        var errors = model.ImportModelFromInpFileForViewing(Path.GetFullPath(path), Console.Error.WriteLine, out var rawKeywords);
        if (errors?.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        return new ViewerModel(model) { RawUserKeywords = rawKeywords };
    }
    public ViewerModel(FeModel model)
    {
        Model = model;
        NodeIds = model.Mesh.Nodes.Keys.Order().Select(i => (long)i).ToArray();
        var index = NodeIds.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => (long)p.i);
        Coordinates = NodeIds.SelectMany(id => { var n = model.Mesh.Nodes[(int)id]; return new[] { n.X, n.Y, n.Z }; }).ToArray();
        var elements = model.Mesh.Elements.Values.OrderBy(e => e.Id).ToArray();
        ElementIds = elements.Select(e => (long)e.Id).ToArray(); Types = elements.Select(e => e.GetVtkCellType()).ToArray();
        Offsets = new int[elements.Length + 1]; var conn = new List<long>();
        for (int i = 0; i < elements.Length; i++) { conn.AddRange(elements[i].GetVtkNodeIds().Select(id => index[id])); Offsets[i + 1] = conn.Count; }
        Connectivity = conn.ToArray();
        if (model.CompositeLayups.Count > 0)
        {
            foreach (var ply in CompositeLayupExpansion.Expand(model)) Plies.Add(new(ply.Section.Name, ply.ElementIds.Select(i => (long)i).ToArray()));
        }
        else
        {
            // INP has no typed CompositeLayup keyword. M2a export retains LU_* sets.
            foreach (var set in model.Mesh.ElementSets.Values.Where(s => s.Name.StartsWith("LU_", StringComparison.OrdinalIgnoreCase)))
                Plies.Add(new(set.Name, set.Labels.Select(i => (long)i).ToArray()));
            PlySource = "LU_* element sets from INP";
            if (Plies.Count == 0)
            {
                // For structured legacy decks, infer the axis with the fewest distinct
                // nonconstant centroid coordinates. DCB stacking is Y, not Z.
                // These are display groups only; no CompositeLayup data are invented.
                double Coord(int id, int a) { var n = model.Mesh.Nodes[id]; return a == 0 ? n.X : a == 1 ? n.Y : n.Z; }
                var axes = Enumerable.Range(0, 3).Select(a => new { Axis = a, Groups = elements.GroupBy(e => Math.Round(e.NodeIds.Average(id => Coord(id, a)), 8)).OrderBy(g => g.Key).ToArray() }).ToArray();
                var chosen = axes.Where(a => a.Groups.Length > 1).OrderBy(a => a.Groups.Length).FirstOrDefault() ?? axes[2];
                string axisName = new[] { "X", "Y", "Z" }[chosen.Axis];
                foreach (var group in chosen.Groups) Plies.Add(new($"{axisName} layer {group.Key:G6} (display)", group.Select(e => (long)e.Id).ToArray()));
                PlySource = $"Geometric {axisName} layers (display only; INP has no layup metadata)";
            }
        }
        var plyById = new Dictionary<long, int>();
        for (int i = 0; i < Plies.Count; i++) foreach (long id in Plies[i].ElementIds)
        {
            if (!plyById.TryAdd(id, i + 1)) throw new InvalidDataException($"Element {id} is in overlapping ply display sets.");
        }
        PlyScalars = ElementIds.Select(id => (double)plyById.GetValueOrDefault(id)).ToArray();
    }
    public void Show(Viewport view)
    {
        view.Load(NodeIds, Coordinates, ElementIds, Types, Offsets, Connectivity);
        view.Scalars(PlyScalars, 0, Math.Max(1, Plies.Count), "Ply / layer"); view.StandardView();
    }
    public IEnumerable<(string Name, IEnumerable<string> Items)> TreeGroups()
    {
        yield return ("Parts", Model.Mesh.Parts.Keys);
        yield return ("Materials", Model.Materials.Keys);
        yield return ("Sections", Model.Sections.Keys);
        yield return ("Composite layups", Model.CompositeLayups.Keys);
        yield return ("Ply / layer display", Plies.Select(p => p.Name));
    }
}
