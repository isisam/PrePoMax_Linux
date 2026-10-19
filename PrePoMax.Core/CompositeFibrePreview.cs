using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CaeModel;
using vtkControl;

namespace PrePoMax
{
    // Display only: consumes the same expanded basis/angle as the CalculiX writer.
    public static class CompositeFibrePreview
    {
        public static vtkMaxActorData[] Build(FeModel model, IList<ExpandedCompositePly> plies,
                                             IEnumerable<int> selected, double size)
        {
            var points = new[] { new List<double[]>(), new List<double[]>(), new List<double[]>() };
            foreach (int index in selected.Distinct())
            {
                var ply = plies[index];
                double angle = ply.AngleDeg * Math.PI / 180;
                double[] fibre = Enumerable.Range(0, 3).Select(k => Math.Cos(angle) * ply.A[k] + Math.Sin(angle) * ply.B[k]).ToArray();
                double[] transverse = Enumerable.Range(0, 3).Select(k => -Math.Sin(angle) * ply.A[k] + Math.Cos(angle) * ply.B[k]).ToArray();
                double[] normal = { ply.A[1] * ply.B[2] - ply.A[2] * ply.B[1],
                                    ply.A[2] * ply.B[0] - ply.A[0] * ply.B[2],
                                    ply.A[0] * ply.B[1] - ply.A[1] * ply.B[0] };
                foreach (int id in ply.ElementIds)
                {
                    var element = model.Mesh.Elements[id];
                    int n = element.NodeIds.Length;
                    // Use corner-node centroid for both linear and quadratic solids.
                    int corners = n == 10 ? 4 : n == 15 ? 6 : n == 20 ? 8 : n;
                    var xyz = element.NodeIds.Take(corners).Select(i => model.Mesh.Nodes[i].Coor).ToArray();
                    double[] center = Enumerable.Range(0, 3).Select(k => xyz.Average(p => p[k])).ToArray();
                    // In-plane element span avoids very short glyphs in thin laminates.
                    Func<double[], double> span = v => xyz.Max(p => Enumerable.Range(0, 3).Sum(k => p[k] * v[k]))
                                                       - xyz.Min(p => Enumerable.Range(0, 3).Sum(k => p[k] * v[k]));
                    double length = Math.Min(span(ply.A), span(ply.B)) * .65 * Math.Max(.1, Math.Min(size, 3));
                    Action<int, double[], double, double> line = (group, direction, lo, hi) => {
                        points[group].Add(Enumerable.Range(0, 3).Select(k => center[k] + direction[k] * length * lo).ToArray());
                        points[group].Add(Enumerable.Range(0, 3).Select(k => center[k] + direction[k] * length * hi).ToArray());
                    };
                    line(0, fibre, -.5, .5);
                    line(1, transverse, 0, .28);
                    line(2, normal, 0, .2);
                }
            }
            Color[] colors = { Color.Crimson, Color.SeaGreen, Color.RoyalBlue };
            return Enumerable.Range(0, 3).Where(i => points[i].Count > 0).Select(i => {
                var data = new vtkMaxActorData { Name = "__composite_fibre_" + i, Color = colors[i],
                    LineWidth = i == 0 ? 3 : 2, Lighting = false, UseGlobalLighting = false,
                    Pickable = false, BackfaceCulling = false, SectionViewPossible = false };
                data.Geometry.Nodes.Coor = points[i].ToArray();
                data.Geometry.Cells.CellNodeIds = Enumerable.Range(0, points[i].Count / 2).Select(j => new[] { 2 * j, 2 * j + 1 }).ToArray();
                data.Geometry.Cells.Types = Enumerable.Repeat(3, points[i].Count / 2).ToArray(); // VTK_LINE
                return data;
            }).ToArray();
        }
    }
}
