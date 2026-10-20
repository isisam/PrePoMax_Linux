using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CaeModel;
using vtkControl;

namespace PrePoMax
{
    // Transient display geometry only. Never moves mesh nodes or changes expansion/export.
    public static class CompositeFibrePreview
    {
        private static double Dot(double[] a, double[] b) { return a.Zip(b, (x, y) => x * y).Sum(); }
        private static double[] Sub(double[] a, double[] b) { return a.Zip(b, (x, y) => x - y).ToArray(); }
        private static double[] Cross(double[] a, double[] b)
        { return new[] { a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0] }; }

        public static vtkMaxActorData[] Build(FeModel model, IList<ExpandedCompositePly> plies,
                                             int sourceCount, bool symmetric, int selected, double size, double gap = 1)
        {
            var actors = new List<vtkMaxActorData>();
            if (plies.Count == 0) return actors.ToArray();
            // A gap measured against the whole in-plane footprint, rather than thin ply
            // thickness, separates plates in an isometric view. Slider zero closes the gap.
            var allNodes = plies.SelectMany(p => p.ElementIds).SelectMany(id => model.Mesh.Elements[id].NodeIds)
                                .Distinct().Select(id => model.Mesh.Nodes[id].Coor).ToArray();
            Func<double[], double> span = v => allNodes.Max(p => Dot(p, v)) - allNodes.Min(p => Dot(p, v));
            double spacing = 1.2 * Math.Max(span(plies[0].A), span(plies[0].B)) * Math.Max(0, Math.Min(gap, 2));
            for (int index = 0; index < plies.Count; index++)
            {
                // A selected physical ply is the only coloured geometry, including in
                // symmetric layups. Partners can be selected separately in the editor.
                if (selected >= 0 && index != selected) continue;
                var ply = plies[index];
                int source = CompositePlyPalette.Source(index, sourceCount, symmetric);
                Color color = CompositePlyPalette.Get(source);
                double angle = ply.AngleDeg * Math.PI / 180;
                double[] fibre = Enumerable.Range(0, 3).Select(k => Math.Cos(angle)*ply.A[k] + Math.Sin(angle)*ply.B[k]).ToArray();
                double[] normal = Cross(ply.A, ply.B);
                double offset = selected < 0 ? (index - (plies.Count - 1)*.5)*spacing : 0;
                Func<double[], double[]> move = p => Enumerable.Range(0, 3).Select(k => p[k] + normal[k]*offset).ToArray();
                var glyphs = new List<double[]>();
                var nodes = new List<double[]>();
                var faces = new List<int[]>();
                var edges = new List<int[]>();
                var nodeMap = new Dictionary<int, int>();
                var edgeKeys = new HashSet<string>();
                foreach (int id in ply.ElementIds)
                {
                    var element = model.Mesh.Elements[id];
                    int n = element.NodeIds.Length;
                    int corners = n == 10 ? 4 : n == 13 ? 5 : n == 15 ? 6 : n == 20 ? 8 : n;
                    var xyz = element.NodeIds.Take(corners).Select(i => model.Mesh.Nodes[i].Coor).ToArray();
                    double[] center = Enumerable.Range(0, 3).Select(k => xyz.Average(p => p[k])).ToArray();
                    Func<double[], double> localSpan = v => xyz.Max(p => Dot(p, v)) - xyz.Min(p => Dot(p, v));
                    double halfLength = .30 * Math.Min(localSpan(ply.A), localSpan(ply.B)) * Math.Max(.1, Math.Min(size, 3));
                    // Clip against the element's actual face planes. This keeps a glyph
                    // inside skewed and triangular footprints, even at maximum size.
                    foreach (var rawFace in element.GetAllVtkCells())
                    {
                        int count = rawFace.Length == 6 ? 3 : rawFace.Length == 8 ? 4 : rawFace.Length;
                        int[] ids = rawFace.Take(count).ToArray();
                        var fp = ids.Select(i => model.Mesh.Nodes[i].Coor).ToArray();
                        var fn = Cross(Sub(fp[1], fp[0]), Sub(fp[2], fp[0]));
                        double denominator = Math.Abs(Dot(fn, fibre));
                        if (denominator > 1e-12)
                            halfLength = Math.Min(halfLength, .9 * Math.Abs(Dot(fn, Sub(fp[0], center))) / denominator);
                        foreach (int nodeId in ids)
                            if (!nodeMap.ContainsKey(nodeId)) { nodeMap[nodeId] = nodes.Count; nodes.Add(move(model.Mesh.Nodes[nodeId].Coor)); }
                        int[] face = ids.Select(i => nodeMap[i]).ToArray();
                        faces.Add(face);
                        for (int j = 0; j < count; j++)
                        {
                            int a = face[j], b = face[(j+1)%count];
                            if (edgeKeys.Add(Math.Min(a,b) + ":" + Math.Max(a,b))) edges.Add(new[] {a,b});
                        }
                    }
                    glyphs.Add(move(Enumerable.Range(0, 3).Select(k => center[k] - fibre[k]*halfLength).ToArray()));
                    glyphs.Add(move(Enumerable.Range(0, 3).Select(k => center[k] + fibre[k]*halfLength).ToArray()));
                }
                Action<string, Color, double, double[][], int[][], int[]> add = (suffix, rgba, width, points, cells, types) => {
                    if (cells.Length == 0) return;
                    var data = new vtkMaxActorData { Name = "__composite_fibre_" + index + suffix,
                        Color = rgba, LineWidth = (float)width, Lighting = false, UseGlobalLighting = false,
                        Pickable = false, BackfaceCulling = false, SectionViewPossible = false };
                    data.Geometry.Nodes.Coor = points;
                    data.Geometry.Cells.CellNodeIds = cells;
                    data.Geometry.Cells.Types = types;
                    actors.Add(data);
                };
                add("_0", color, 3, glyphs.ToArray(), Enumerable.Range(0, glyphs.Count/2).Select(j => new[] {2*j,2*j+1}).ToArray(), Enumerable.Repeat(3, glyphs.Count/2).ToArray());
                add("_edges", color, 1.5, nodes.ToArray(), edges.ToArray(), Enumerable.Repeat(3, edges.Count).ToArray());
                add("_faces", Color.FromArgb(38, color), 1, nodes.ToArray(), faces.ToArray(), faces.Select(f => f.Length == 3 ? 5 : f.Length == 4 ? 9 : 7).ToArray());
            }
            return actors.ToArray();
        }
    }
}
