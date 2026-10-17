using System.Text.Json;
using System.Runtime.InteropServices;
using PrePoMax.Viewer;
using Vtkmax.Interop;

static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
static void Reject(Action action)
{
    try { action(); } catch (InvalidOperationException) { return; }
    throw new Exception("Expected native argument rejection");
}
Require(Marshal.SizeOf<PickResult>() == 56, "C ABI struct size");
// Copy isolation matters to import and later edits; mutate nested arrays
// independently while preserving values and serialized model identities.
var originalPart = new CaeMesh.MeshPart("copy", 1, [1], [1], [typeof(CaeMesh.LinearHexaElement)]);
originalPart.BoundingBox.MinX = -3;
var mass = new CaeMesh.PartMassProperties(true) { Volume = 42, Area = 7 };
mass.CenterOfMass[0] = 3; mass.InertiaMatrixCG[1][2] = 5;
originalPart.MassProperties = mass;
var copy = originalPart.DeepCopy();
copy.BoundingBox.MinX = -99; copy.MassProperties.InertiaMatrixCG[1][2] = 99; copy.MassProperties.CenterOfMass[0] = 99;
Require(originalPart.BoundingBox.MinX == -3 && originalPart.MassProperties.InertiaMatrixCG[1][2] == 5
    && originalPart.MassProperties.CenterOfMass[0] == 3 && copy.MassProperties.Volume == 42, "import part copy isolation");
var output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
var receipts = new List<object>();
using (var v = new Viewport(true, 1000, 700))
{
    foreach (string path in args.Skip(1))
    {
        var model = ViewerModel.Open(path); model.Show(v); v.Render();
        var editable = new CaeModel.FeModel("editable", null, true);
        editable.Properties.ModelSpace = CaeModel.ModelSpaceEnum.ThreeD;
        var importErrors = editable.ImportModelFromInpFile(Path.GetFullPath(path), _ => { }, true);
        Require(importErrors == null || importErrors.Count == 0, "editable import errors");
        Require(editable.Materials.Keys.SequenceEqual(model.Model.Materials.Keys)
            && editable.Sections.Keys.SequenceEqual(model.Model.Sections.Keys)
            && editable.Mesh.Parts.Keys.SequenceEqual(model.Model.Mesh.Parts.Keys), "viewer/editable metadata parity");
        Require(editable.Mesh.Nodes.Count == model.NodeIds.Length && editable.Mesh.Elements.Count == model.ElementIds.Length,
            "viewer/editable mesh size parity");
        foreach (var node in editable.Mesh.Nodes.Values)
        {
            var other = model.Model.Mesh.Nodes[node.Id];
            Require(node.X == other.X && node.Y == other.Y && node.Z == other.Z, "viewer/editable coordinates");
        }
        foreach (var element in editable.Mesh.Elements.Values)
            Require(element.NodeIds.SequenceEqual(model.Model.Mesh.Elements[element.Id].NodeIds), "viewer/editable connectivity");
        if (path.Contains("dcb", StringComparison.OrdinalIgnoreCase))
            Require(model.RawUserKeywords.Any(k => k.Contains("USER MATERIAL", StringComparison.OrdinalIgnoreCase)), "legacy raw USER MATERIAL retained");
        string name = Path.GetFileNameWithoutExtension(path);
        v.WritePng(Path.Combine(output, name + "-ply.png"));
        v.Highlight(model.Plies[0].ElementIds); v.Render(); v.WritePng(Path.Combine(output, name + "-highlight.png"));
        if (path.Contains("dcb", StringComparison.OrdinalIgnoreCase))
        {
            v.StandardView(3); v.Zoom(8); v.Display(true, false);
            v.WritePng(Path.Combine(output, name + "-ply-detail.png")); v.Display(true, true);
        }
        v.Highlight([]); v.StandardView(3); v.Fit(); v.Render();
        double maxZ = model.Model.Mesh.Nodes.Values.Max(n => n.Z);
        var e = model.Model.Mesh.Elements.Values.OrderBy(e => e.Id).First(e => e.NodeIds.Count(id => Math.Abs(model.Model.Mesh.Nodes[id].Z - maxZ) < 1e-8) >= 3);
        var nodes = e.NodeIds.Select(id => model.Model.Mesh.Nodes[id]).Where(n => Math.Abs(n.Z - maxZ) < 1e-8).ToArray();
        var world = new[] { nodes.Average(n => n.X), nodes.Average(n => n.Y), maxZ };
        var screen = v.Project(world[0], world[1], world[2]);
        var picked = v.Pick((int)Math.Round(screen[0]), (int)Math.Round(screen[1]));
        Require(picked.Hit == 1 && picked.ElementId == e.Id && picked.FaceId >= 0, $"{name}: expected {e.Id}, got {picked.ElementId}");
        v.WritePng(Path.Combine(output, name + "-pick.png"));
        var missed = v.Pick(-1, -1); Require(missed.Hit == 0 && missed.ElementId == -1 && missed.NodeId == -1, "miss IDs");
        Reject(() => v.Highlight([long.MaxValue])); Reject(() => v.Scalars([1, 2])); Reject(() => v.Zoom(0));
        v.Display(false, true); v.Render(); v.WritePng(Path.Combine(output, name + "-wire.png"));
        v.Display(true, true); v.Resize(900, 600); v.Rotate(23, 12); v.Zoom(1.1); v.Render(); v.Resize(1000, 700);
        receipts.Add(new { name, path, nodes = model.NodeIds.Length, elements = model.ElementIds.Length, plies = model.Plies.Count,
            source = model.PlySource, expectedElement = e.Id, screen, world, picked });
        Console.WriteLine($"PASS {name}: {model.NodeIds.Length} nodes, {model.ElementIds.Length} cells, {model.Plies.Count} plies/layers, pick={picked.ElementId} face={picked.FaceId}");
    }
    // Non-contiguous 64-bit IDs and each supported topology, including quadratic midsides.
    var hex = new double[] { 0,0,0, 1,0,0, 1,1,0, 0,1,0, 0,0,1, 1,0,1, 1,1,1, 0,1,1,
        .5,0,0, 1,.5,0, .5,1,0, 0,.5,0, .5,0,1, 1,.5,1, .5,1,1, 0,.5,1, 0,0,.5, 1,0,.5, 1,1,.5, 0,1,.5 };
    var tet = new double[] {0,0,0, 1,0,0, 0,1,0, 0,0,1, .5,0,0, .5,.5,0, 0,.5,0, 0,0,.5, .5,0,.5, 0,.5,.5};
    var wedge = new double[] {0,0,0, 1,0,0, 0,1,0, 0,0,1, 1,0,1, 0,1,1,
        .5,0,0, .5,.5,0, 0,.5,0, .5,0,1, .5,.5,1, 0,.5,1, 0,0,.5, 1,0,.5, 0,1,.5};
    var quad = new double[] {0,0,0, 1,0,0, 1,1,0, 0,1,0, .5,0,0, 1,.5,0, .5,1,0, 0,.5,0};
    var tri = new double[] {0,0,0, 1,0,0, 0,1,0, .5,0,0, .5,.5,0, 0,.5,0};
    foreach (var (type, count, xyz) in new[] { (12,8,hex),(25,20,hex),(10,4,tet),(24,10,tet),(13,6,wedge),(26,15,wedge),(9,4,quad),(23,8,quad),(5,3,tri),(22,6,tri) })
    {
        long expected = 9_000_000_000L + type;
        long[] ids = Enumerable.Range(0, count).Select(i => 8_000_000_000L + i).ToArray();
        v.Load(ids, xyz.Take(count * 3).ToArray(), [expected], [type], [0, count], Enumerable.Range(0, count).Select(i => (long)i).ToArray());
        v.StandardView(3); v.Render();
        double x = type is 10 or 24 ? .15 : type is 13 or 26 or 5 or 22 ? .25 : .5;
        var p = v.Project(x, x, type is 9 or 23 or 5 or 22 ? 0 : 1);
        var hit = v.Pick((int)p[0], (int)p[1]);
        Require(hit.Hit == 1 && hit.ElementId == expected && hit.FaceId >= 0, $"topology {type}");
        v.Scalars(Enumerable.Range(0, count).Select(i => (double)i).ToArray(), 0, count, "Node scalar", nodes: true);
        v.Highlight([ids[0]], nodes: true); v.WritePng(Path.Combine(output, $"topology-{type}.png")); v.Highlight([]);
        Reject(() => v.Load(ids, xyz.Take(count * 3).ToArray(), [expected], [99], [0, count], Enumerable.Range(0, count).Select(i => (long)i).ToArray()));
        Require(v.Pick((int)p[0], (int)p[1]).ElementId == expected, "invalid load must preserve grid");
        Console.WriteLine($"PASS topology {type}, 64-bit element {expected}");
    }
    // Front-facing upper corner of a cube: closest node must preserve original ID.
    long[] cubeIds = Enumerable.Range(0, 8).Select(i => 101L + i * 7).ToArray();
    v.Load(cubeIds, hex.Take(24).ToArray(), [909], [12], [0,8], [0,1,2,3,4,5,6,7]);
    v.StandardView(3); v.Render(); var corner = v.Project(0.001,0.001,1);
    var nodePick = v.Pick((int)Math.Ceiling(corner[0]), (int)Math.Ceiling(corner[1]));
    Require(nodePick.NodeId == cubeIds[4] && nodePick.FaceId == 5, $"cube node/face pick: node={nodePick.NodeId}, face={nodePick.FaceId}");
    receipts.Add(new { check = "node_and_face", expectedNode = cubeIds[4], expectedFace = 5, nodePick });
    Console.WriteLine($"PASS node={nodePick.NodeId}, VTK face={nodePick.FaceId}");
}
File.WriteAllText(Path.Combine(output, "receipt.json"), JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
Console.WriteLine("ALL VIEWER CHECKS PASS");
