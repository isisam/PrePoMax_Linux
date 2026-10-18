using CaeModel;
using CaeResults;
using PrePoMax;
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
    public FeResults? Results { get; private set; }
    public double[]? Displacement { get; private set; }
    public Dictionary<string, double[]> Contours { get; } = [];
    public int ResultsStep { get; private set; }
    public int ResultsIncrement { get; private set; }
    public double ResultsTime { get; private set; }
    public const int MaxColourGroups = 64;
    private static readonly SemaphoreSlim importGate = new(1, 1);
    public static async Task<ViewerModel> OpenAsync(string path, IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        await importGate.WaitAsync(cancellation);
        try { return await Task.Run(() => Open(path, progress), cancellation); }
        finally { importGate.Release(); }
    }

    public static ViewerModel Open(string path, IProgress<string>? progress = null)
    {
        progress?.Report("Reading " + Path.GetFileName(path));
        if (Path.GetExtension(path).Equals(".pmx", StringComparison.OrdinalIgnoreCase))
        {
            SettingsContainer.IsSaveDisabled = true;
            Exception? error = null; var previous = CoreHost.FileReadError;
            CoreHost.FileReadError = e => error = e;
            try
            {
                var controller = new Controller(new ProgressView(progress));
                controller.RegenerationWorkDirectory=Path.GetDirectoryName(Path.GetFullPath(path));
                controller.Open(Path.GetFullPath(path));
                if (error != null) throw new InvalidDataException("PMX read failed", error);
                if (controller.Model?.Mesh == null) throw new InvalidDataException("PMX contains no mesh");
                progress?.Report("Adapting PMX mesh");
                return new ViewerModel(controller.Model);
            }
            finally { CoreHost.FileReadError = previous; }
        }
        if (!Path.GetExtension(path).Equals(".inp", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Expected INP or PMX");
        var model = new FeModel(Path.GetFileNameWithoutExtension(path), null, true);
        model.Properties.ModelSpace = ModelSpaceEnum.ThreeD;
        var errors = model.ImportModelFromInpFileForViewing(Path.GetFullPath(path), s => { progress?.Report(s); Console.Error.WriteLine(s); }, out var rawKeywords);
        if (errors?.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        progress?.Report("Adapting INP mesh");
        return new ViewerModel(model) { RawUserKeywords = rawKeywords };
    }
    private sealed class ProgressView(IProgress<string>? progress) : NullMainView
    {
        public override void WriteDataToOutput(string data) => progress?.Report(data);
        public override void WriteErrorToOutput(string data) => throw new InvalidDataException(data);
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
                var axes = Enumerable.Range(0, 3).Select(a => new { Axis = a, Count = elements.Select(e => Math.Round(e.NodeIds.Average(id => Coord(id, a)), 8)).Distinct().Take(MaxColourGroups + 1).Count() }).ToArray();
                var chosen = axes.Where(a => a.Count > 1 && a.Count <= MaxColourGroups).OrderBy(a => a.Count).FirstOrDefault();
                if (chosen != null)
                {
                    string axisName = new[] { "X", "Y", "Z" }[chosen.Axis];
                    foreach (var group in elements.GroupBy(e => Math.Round(e.NodeIds.Average(id => Coord(id, chosen.Axis)), 8)).OrderBy(g => g.Key))
                        Plies.Add(new($"{axisName} layer {group.Key:G6} (display)", group.Select(e => (long)e.Id).ToArray()));
                    PlySource = $"Geometric {axisName} layers (display only; INP has no layup metadata)";
                }
            }
            if (Plies.Count == 0 || Plies.Count > MaxColourGroups)
            {
                Plies.Clear();
                foreach (var group in model.Mesh.Parts.Values.GroupBy(p => Math.Abs(p.PartId % MaxColourGroups)))
                    Plies.Add(new(string.Join(", ", group.Take(3).Select(p => p.Name)), group.SelectMany(p => p.Labels).Select(i => (long)i).ToArray()));
                PlySource = "Per-part colours (64-colour palette; no inferred layup)";
            }
        }
        var plyById = new Dictionary<long, int>();
        for (int i = 0; i < Plies.Count; i++) foreach (long id in Plies[i].ElementIds)
        {
            if (!plyById.TryAdd(id, i + 1)) throw new InvalidDataException($"Element {id} is in overlapping ply display sets.");
        }
        PlyScalars = ElementIds.Select(id => (double)plyById.GetValueOrDefault(id)).ToArray();
    }
    public async Task LoadResultsAsync(string path, IProgress<string>? progress = null)
    {
        await importGate.WaitAsync();
        try { await Task.Run(() => LoadResults(path, progress)); }
        finally { importGate.Release(); }
    }
    public void LoadResults(string path, IProgress<string>? progress = null)
    {
        progress?.Report("Reading FRD " + Path.GetFileName(path));
        var r = FrdFileReader.Read(Path.GetFullPath(path)) ?? throw new InvalidDataException("FRD read failed");
        r.ComputeVisibleFieldInvariants();
        if (r.Mesh.Nodes.Count != NodeIds.Length || r.Mesh.Elements.Count != ElementIds.Length) throw new InvalidDataException("FRD mesh size differs");
        double coordinateTolerance=Math.Max(1,Coordinates.Select(Math.Abs).Max())*1e-5; // ASCII FRD has six significant digits.
        foreach (var id in NodeIds)
        {
            if (!r.Mesh.Nodes.TryGetValue((int)id, out var node)) throw new InvalidDataException($"FRD missing node {id}");
            var n = Model.Mesh.Nodes[(int)id];
            if (Math.Abs(n.X-node.X)+Math.Abs(n.Y-node.Y)+Math.Abs(n.Z-node.Z) > coordinateTolerance) throw new InvalidDataException($"FRD mesh differs at node {id}");
        }
        foreach (var id in ElementIds)
            if (!r.Mesh.Elements.TryGetValue((int)id,out var e) || !e.NodeIds.SequenceEqual(Model.Mesh.Elements[(int)id].NodeIds))
                throw new InvalidDataException($"FRD connectivity differs at element {id}");
        // A failed solve may append a DISP-only diagnostic increment. Select the
        // latest increment containing both S and U; never reuse a previous range.
        var common=r.GetAllFieldData().Where(f=>f.Name==FOFieldNames.Stress)
            .Where(f=>r.GetFieldData(FOFieldNames.Disp,FOComponentNames.U1,f.StepId,f.StepIncrementId,true).Valid)
            .OrderBy(f=>f.StepId).ThenBy(f=>f.StepIncrementId).LastOrDefault()
            ?? throw new InvalidDataException("FRD has no common S/U increment");
        int step=common.StepId,inc=common.StepIncrementId;
        var ids=NodeIds.Select(i=>(int)i).ToArray();
        double[] Values(string field, string component)
        {
            if(!r.GetAllFieldNames().Contains(field))throw new InvalidDataException($"FRD has no {field} field");
            var data=r.GetFieldData(field,component,step,inc) ?? throw new InvalidDataException($"FRD missing {field}/{component}");
            var values=r.GetValues(data,ids) ?? throw new InvalidDataException($"FRD has no {field}/{component} values");
            return values.Select(x=>(double)x).ToArray();
        }
        var u1=Values(FOFieldNames.Disp,FOComponentNames.U1); var u2=Values(FOFieldNames.Disp,FOComponentNames.U2); var u3=Values(FOFieldNames.Disp,FOComponentNames.U3);
        var displacement=new double[3*ids.Length];
        for(int i=0;i<ids.Length;i++){displacement[3*i]=u1[i];displacement[3*i+1]=u2[i];displacement[3*i+2]=u3[i];}
        var s=Values(FOFieldNames.Stress,FOComponentNames.Mises); var u=Values(FOFieldNames.Disp,FOComponentNames.All);
        Contours.Clear();Contours.Add("S (Mises)",s);Contours.Add("U (magnitude)",u);Displacement=displacement;Results=r;
        ResultsStep=step;ResultsIncrement=inc;ResultsTime=common.Time;
        progress?.Report($"FRD ready: step {step}, increment {inc}");
    }
    public void ShowContour(Viewport view,string field,double deformationScale=0)
    {
        var values=Contours[field];view.ContourBands(12);view.Scalars(values,values.Min(),values.Max(),field,nodes:true);
        view.Deformation(Displacement,deformationScale);view.Fit();view.Render();
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
