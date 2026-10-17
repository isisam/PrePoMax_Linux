// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    // A transient result shared by validation, effective section assignment and the keyword writer.
    public class ExpandedCompositePly
    {
        public SolidSection Section { get; internal set; }
        public string OrientationName { get; internal set; }
        public double[] A { get; internal set; }
        public double[] B { get; internal set; }
        public double AngleDeg { get; internal set; }
        public int[] ElementIds { get; internal set; }
    }

    public static class CompositeLayupExpansion
    {
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static string Number(double value) { return value.ToString("G17", CultureInfo.InvariantCulture); }
        private static string Ids(IEnumerable<int> ids)
        {
            return string.Join(", ", ids.OrderBy(id => id).Take(8));
        }
        public static int[] ResolveRegion(FeModel model, IMultiRegion region)
        {
            var mesh = model.Mesh;
            IEnumerable<int> ids;
            bool fromParts = false;
            if (region.RegionType == RegionTypeEnum.PartName)
            {
                if (region.RegionName == null || !mesh.Parts.ContainsKey(region.RegionName))
                    throw new CaeException("Missing part '" + region.RegionName + "'.");
                ids = mesh.Parts[region.RegionName].Labels;
            }
            else if (region.RegionType == RegionTypeEnum.ElementSetName)
            {
                if (region.RegionName == null || !mesh.ElementSets.ContainsKey(region.RegionName))
                    throw new CaeException("Missing element set '" + region.RegionName + "'.");
                var set = mesh.ElementSets[region.RegionName];
                ids = set.Labels;
                fromParts = set.CreatedFromParts;
            }
            else if (region.RegionType == RegionTypeEnum.Selection)
            {
                ids = region.CreationIds ?? new int[0];
                fromParts = region.CreationData != null && region.CreationData.SelectItem == vtkSelectItem.Part;
                if (region.CreationData != null && (region.CreationData.SelectItem == vtkSelectItem.Geometry ||
                                                    region.CreationData.SelectItem == vtkSelectItem.GeometrySurface))
                    ids = mesh.GetIdsFromGeometryIds(ids.ToArray(), vtkSelectItem.Element);
            }
            else throw new CaeException("A composite layup requires a part or element set of solid elements.");
            if (fromParts)
            {
                var partIds = new HashSet<int>(ids);
                var parts = mesh.Parts.Values.Where(p => partIds.Contains(p.PartId)).ToArray();
                if (parts.Length != partIds.Count) throw new CaeException("The region contains missing part IDs.");
                ids = parts.SelectMany(p => p.Labels);
            }
            return ids.Distinct().OrderBy(id => id).ToArray();
        }
        public static List<ExpandedCompositePly> Expand(FeModel model)
        {
            var result = new List<ExpandedCompositePly>();
            var errors = new List<string>();
            var assigned = new HashSet<int>();
            var names = new HashSet<string>(model.Mesh.GetReservedElementSetNames()
                .Select(n => CalculixNameShortener.GetShortenedName(n)), StringComparer.OrdinalIgnoreCase);
            var orientationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var layup in model.CompositeLayups.Values)
            {
                if (!layup.Active) continue;
                try
                {
                    var expanded = Expand(model, layup);
                    int[] overlap = expanded.SelectMany(p => p.ElementIds).Where(assigned.Contains).ToArray();
                    if (overlap.Length > 0)
                        throw new CaeException("Overlaps another active layup: " + overlap.Length + " elements; IDs " + Ids(overlap) + ".");
                    foreach (var ply in expanded)
                    {
                        if (!names.Add(ply.Section.RegionName) || !orientationNames.Add(ply.OrientationName))
                            throw new CaeException("Generated name collision for '" + ply.Section.RegionName + "'. Rename the layup or ply.");
                        assigned.UnionWith(ply.ElementIds);
                    }
                    result.AddRange(expanded);
                }
                catch (CaeException ex) { errors.Add("Composite layup '" + layup.Name + "': " + ex.Message); }
            }
            if (errors.Count > 0) throw new CaeException(string.Join(Environment.NewLine, errors));
            return result;
        }
        // Validate an unsaved editor value without changing the live model, caches or mesh.
        // Ignore the original being edited; other invalid layups must remain independently repairable.
        public static List<ExpandedCompositePly> Preview(FeModel model, CompositeLayup layup, string oldName)
        {
            var expanded = Expand(model, layup);
            var ids = new HashSet<int>(expanded.SelectMany(p => p.ElementIds));
            var names = new HashSet<string>(model.Mesh.GetReservedElementSetNames()
                .Select(n => CalculixNameShortener.GetShortenedName(n)), StringComparer.OrdinalIgnoreCase);
            foreach (var ply in expanded)
                if (!names.Add(ply.Section.RegionName))
                    throw new CaeException("Generated name collision for '" + ply.Section.RegionName + "'.");
            foreach (var other in model.CompositeLayups.Values)
            {
                if (!other.Active || string.Equals(other.Name, oldName, StringComparison.OrdinalIgnoreCase)) continue;
                int[] region;
                try { region = ResolveRegion(model, other); }
                catch (CaeException) { continue; }
                int[] overlap = region.Where(ids.Contains).ToArray();
                if (overlap.Length > 0)
                    throw new CaeException("Overlaps active layup '" + other.Name + "': " + overlap.Length +
                                           " elements; IDs " + Ids(overlap) + ".");
            }
            return expanded;
        }
        private static List<ExpandedCompositePly> Expand(FeModel model, CompositeLayup layup)
        {
            if (model.Properties.ModelSpace != ModelSpaceEnum.ThreeD)
                throw new CaeException("Only a 3D solid model is supported.");
            if (layup.StackingAxis < 1 || layup.StackingAxis > 3)
                throw new CaeException("StackingAxis must be 1, 2 or 3.");
            if (!Finite(layup.BoundaryTolerance) || layup.BoundaryTolerance < 0 || layup.BoundaryTolerance >= 0.5)
                throw new CaeException("BoundaryTolerance must be finite, nonnegative and less than 0.5.");
            if (layup.Plies == null || layup.Plies.Count == 0 || layup.Plies.Any(p => p == null))
                throw new CaeException("At least one non-null ply is required.");
            foreach (var ply in layup.Plies)
            {
                if (!Finite(ply.Thickness) || ply.Thickness <= 0 || !Finite(ply.AngleDeg))
                    throw new CaeException("Ply '" + ply.Name + "' requires finite Thickness > 0 and finite AngleDeg.");
                if (ply.ElementsThroughThickness.HasValue && ply.ElementsThroughThickness.Value <= 0)
                    throw new CaeException("Ply '" + ply.Name + "': ElementsThroughThickness must be positive when supplied.");
                if (ply.MaterialName == null || !model.Materials.ContainsValidActiveKey(ply.MaterialName))
                    throw new CaeException("Ply '" + ply.Name + "': missing, inactive or invalid material '" + ply.MaterialName + "'.");
            }
            Vec3D[] basis = { new Vec3D(1, 0, 0), new Vec3D(0, 1, 0), new Vec3D(0, 0, 1) };
            Vec3D origin = new Vec3D();
            if (layup.CoordinateSystemName != null)
            {
                CoordinateSystem cs;
                if (!model.Mesh.CoordinateSystems.TryGetValue(layup.CoordinateSystemName, out cs))
                    throw new CaeException("Missing coordinate system '" + layup.CoordinateSystemName + "'.");
                if (cs.Type != CoordinateSystemTypeEnum.Rectangular)
                    throw new CaeException("Coordinate system '" + cs.Name + "' must be rectangular.");
                if (!cs.TryCheckEquations() || !cs.Active)
                    throw new CaeException("Coordinate system '" + cs.Name + "' is invalid or inactive.");
                origin = cs.Center();
                basis = new[] { cs.DirectionX(), cs.DirectionY(), cs.DirectionZ() };
            }
            if (origin.Coor.Any(v => !Finite(v)) || basis.Any(v => v.Coor.Any(x => !Finite(x)) || Math.Abs(v.Len - 1) > 1E-10))
                throw new CaeException("The rectangular coordinate system must have a finite orthonormal basis.");
            Vec3D axis = basis[layup.StackingAxis - 1];
            var ids = ResolveRegion(model, layup);
            if (ids.Length == 0) throw new CaeException("The region is empty.");
            var ranges = new Dictionary<int, double[]>();
            var activePartIds = new HashSet<int>(model.Mesh.Parts.Values.Where(p => p.Active).Select(p => p.PartId));
            foreach (int id in ids)
            {
                FeElement element;
                if (!model.Mesh.Elements.TryGetValue(id, out element) || !(element is FeElement3D))
                    throw new CaeException("The region requires solid elements; invalid element ID " + id + ".");
                if (!activePartIds.Contains(element.PartId))
                    throw new CaeException("Element " + id + " belongs to an inactive or missing part.");
                var coordinates = element.NodeIds.Select(n => model.Mesh.Nodes[n].Coor).ToArray();
                var z = coordinates.Select(c => (c[0] - origin.X) * axis.X + (c[1] - origin.Y) * axis.Y + (c[2] - origin.Z) * axis.Z).ToArray();
                if (z.Any(v => !Finite(v))) throw new CaeException("Nonfinite node coordinates at element " + id + ".");
                ranges.Add(id, new[] { z.Min(), z.Max() });
            }
            var regionIds = new HashSet<int>(ids);
            foreach (var section in model.Sections.Values.Where(s => s.Active && !(s is MassSection)))
            {
                int[] overlap = ResolveRegion(model, section).Where(regionIds.Contains).ToArray();
                if (overlap.Length > 0)
                    throw new CaeException("Overlaps ordinary section '" + section.Name + "': " + overlap.Length + " elements; IDs " + Ids(overlap) + ".");
            }
            var plies = layup.GetExpandedPlies();
            double total = plies.Sum(p => p.Thickness);
            double minimum = ranges.Values.Min(r => r[0]);
            double extent = ranges.Values.Max(r => r[1]) - minimum;
            double tolerance = total * layup.BoundaryTolerance;
            if (!Finite(total) || !Finite(extent) || Math.Abs(extent - total) > tolerance)
                throw new CaeException("Thickness mismatch: region extent = " + Number(extent) +
                                       ", ply total thickness = " + Number(total) + ", tolerance = " + Number(tolerance) + ".");
            var boundaries = new List<double> { minimum };
            foreach (var ply in plies) boundaries.Add(boundaries.Last() + ply.Thickness);
            var groups = plies.Select(p => new List<int>()).ToArray();
            var crossing = new List<int>();
            var outside = new List<int>();
            var ambiguous = new List<int>();
            foreach (int id in ids)
            {
                double lo = ranges[id][0], hi = ranges[id][1];
                var matches = Enumerable.Range(0, plies.Count)
                    .Where(i => lo >= boundaries[i] - tolerance && hi <= boundaries[i + 1] + tolerance).ToArray();
                if (matches.Length > 1 || hi <= lo) ambiguous.Add(id);
                else if (matches.Length == 1) groups[matches[0]].Add(id);
                else if (lo < minimum - tolerance || hi > boundaries.Last() + tolerance) outside.Add(id);
                else crossing.Add(id);
            }
            if (crossing.Count + outside.Count + ambiguous.Count > 0)
                throw new CaeException("Elements crossing a ply boundary: " + crossing.Count + " (IDs " + Ids(crossing) +
                                       "); outside the stack: " + outside.Count + " (IDs " + Ids(outside) +
                                       "); ambiguous/zero extent: " + ambiguous.Count + " (IDs " + Ids(ambiguous) + ").");
            var result = new List<ExpandedCompositePly>();
            for (int i = 0; i < plies.Count; i++)
            {
                var ply = plies[i];
                if (groups[i].Count == 0) throw new CaeException("Empty ply " + (i + 1) + " ('" + ply.Name + "').");
                if (ply.ElementsThroughThickness.HasValue)
                {
                    var bands = new List<double[]>();
                    foreach (int id in groups[i])
                    {
                        var range = ranges[id];
                        if (!bands.Any(r => Math.Abs(r[0] - range[0]) <= tolerance && Math.Abs(r[1] - range[1]) <= tolerance))
                            bands.Add(range);
                    }
                    bands.Sort((x, y) => x[0].CompareTo(y[0]));
                    if (bands.Count != ply.ElementsThroughThickness.Value ||
                        Math.Abs(bands[0][0] - boundaries[i]) > tolerance ||
                        Math.Abs(bands.Last()[1] - boundaries[i + 1]) > tolerance ||
                        Enumerable.Range(1, bands.Count - 1).Any(j => Math.Abs(bands[j][0] - bands[j - 1][1]) > tolerance))
                        throw new CaeException("Ply '" + ply.Name + "': ElementsThroughThickness = " + ply.ElementsThroughThickness +
                                               ", but found " + bands.Count + " projected mesh bands (must tile the ply).");
                }
                string suffix = layup.Name + "_" + (i + 1).ToString("D2") + "_" + ply.Name;
                string setName = CalculixNameShortener.GetShortenedName("LU_" + suffix);
                result.Add(new ExpandedCompositePly
                {
                    Section = new SolidSection(setName, ply.MaterialName, setName, RegionTypeEnum.ElementSetName, 1, false),
                    OrientationName = CalculixNameShortener.GetShortenedName("OR_" + suffix),
                    // Cyclic permutation keeps direction 1 along the next CS axis and stacking along local 3.
                    A = basis[layup.StackingAxis % 3].Coor,
                    B = basis[(layup.StackingAxis + 1) % 3].Coor,
                    AngleDeg = ply.AngleDeg,
                    ElementIds = groups[i].ToArray()
                });
            }
            return result;
        }
    }
}
