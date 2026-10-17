// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;
using CaeGlobals;
using CaeModel;

namespace APIs.Python.Model
{
    // Detached values; mutating them cannot bypass recorded model commands.
    public class CompositePlyAPI : BaseAPI
    {
        internal CompositePly Value { get; private set; }
        internal CompositePlyAPI(CompositePly value) { Value = value.DeepClone(); }
        [PythonAPI("The ply name.", Returns = "str")]
        public string name { get { return Value.Name; } }
        [PythonAPI("The ply material.", Returns = "str")]
        public string material_name { get { return Value.MaterialName; } }
        [PythonAPI("The thickness in model length units.", Returns = "float")]
        public double thickness { get { return Value.Thickness; } }
        [PythonAPI("The right-handed fibre rotation in degrees.", Returns = "float")]
        public double angle_deg { get { return Value.AngleDeg; } }
        [PythonAPI("The optional projected mesh band count.", Returns = "int or None")]
        public int? elements_through_thickness { get { return Value.ElementsThroughThickness; } }
    }

    public class CompositeLayupAPI : BaseAPI
    {
        private readonly CompositeLayup _value;
        internal CompositeLayupAPI(CompositeLayup value) { _value = value.DeepClone(); }
        [PythonAPI("The layup name.", Returns = "str")]
        public string name { get { return _value.Name; } }
        [PythonAPI("Whether the layup is active.", Returns = "bool")]
        public bool active { get { return _value.Active; } }
        [PythonAPI("Whether the ply list is mirrored.", Returns = "bool")]
        public bool symmetric { get { return _value.Symmetric; } }
        [PythonAPI("The coordinate system name, or None for global.", Returns = "str or None")]
        public string coordinate_system_name { get { return _value.CoordinateSystemName; } }
        [PythonAPI("The stacking axis in the selected coordinate system.", Returns = "int")]
        public int stacking_axis { get { return _value.StackingAxis; } }
        [PythonAPI("The relative boundary tolerance.", Returns = "float")]
        public double boundary_tolerance { get { return _value.BoundaryTolerance; } }
        [PythonAPI("Returns detached plies, optionally including the symmetric mirror.",
                   Arguments = "expanded: bool = False", Returns = "list[CompositePly]")]
        public CompositePlyAPI[] get_plies(object expanded = null)
        {
            bool mirror = expanded != null && PythonValueConverter.ToBool(expanded, "expanded");
            return (mirror ? _value.GetExpandedPlies() : _value.Plies).Select(p => new CompositePlyAPI(p)).ToArray();
        }
    }

    public class CompositeLayupsAPI : BaseAPI
    {
        private readonly ModelResolver _resolver;
        private readonly SectionService _sections;
        internal CompositeLayupsAPI(ModelResolver resolver, SectionService sections)
        {
            _resolver = resolver;
            _sections = sections;
        }
        [PythonAPI("Returns layup names in model order.", Returns = "list[str]")]
        public string[] get_names() { return _resolver.GetModel().CompositeLayups.Keys.ToArray(); }
        [PythonAPI("Returns a detached layup snapshot.", Arguments = "name: str", Returns = "CompositeLayup")]
        public CompositeLayupAPI get(object name)
        {
            return new CompositeLayupAPI(Resolve(PythonValueConverter.ToText(name, "name", false)));
        }
        private CompositeLayup Resolve(string name)
        {
            CompositeLayup result;
            if (!_resolver.GetModel().CompositeLayups.TryGetValue(name, out result))
                throw new PythonAPIException("The composite layup '" + name + "' does not exist.");
            return result;
        }
        [PythonAPI("Adds a solid composite layup. Geometry is validated on export; invalid layups remain editable.",
                   Arguments = "name: str, region: Selection or Region, plies: list[CompositePly], " +
                               "coordinate_system_name: str = None, stacking_axis: int = 3, symmetric: bool = False, " +
                               "boundary_tolerance: float = 0.000001, active: bool = True", Returns = "CompositeLayup")]
        public CompositeLayupAPI add(object name, object region, object plies, object coordinate_system_name = null,
                                     object stacking_axis = null, object symmetric = null,
                                     object boundary_tolerance = null, object active = null)
        {
            var value = Build(null, name, region, plies, coordinate_system_name, stacking_axis, symmetric, boundary_tolerance, active);
            _resolver.ModelGateway.AddCompositeLayup(value);
            return new CompositeLayupAPI(value);
        }
        [PythonAPI("Replaces a layup, preserving its position. Supply the complete new definition.",
                   Arguments = "old_name: str, name: str, region: Selection or Region, plies: list[CompositePly], " +
                               "coordinate_system_name: str = None, stacking_axis: int = 3, symmetric: bool = False, " +
                               "boundary_tolerance: float = 0.000001, active: bool = True", Returns = "CompositeLayup")]
        public CompositeLayupAPI replace(object old_name, object name, object region, object plies,
                                         object coordinate_system_name = null, object stacking_axis = null,
                                         object symmetric = null, object boundary_tolerance = null, object active = null)
        {
            string old = Resolve(PythonValueConverter.ToText(old_name, "old_name", false)).Name;
            var value = Build(old, name, region, plies, coordinate_system_name, stacking_axis, symmetric, boundary_tolerance, active);
            _resolver.ModelGateway.ReplaceCompositeLayup(old, value);
            return new CompositeLayupAPI(value);
        }
        [PythonAPI("Removes one composite layup.", Arguments = "name: str")]
        public void remove(object name)
        {
            _resolver.ModelGateway.RemoveCompositeLayup(Resolve(PythonValueConverter.ToText(name, "name", false)).Name);
        }
        [PythonAPI("Duplicates layups with independent ply and retained-selection data.", Arguments = "names: str or list[str]")]
        public void duplicate(object names)
        {
            var values = Names(names);
            _resolver.ModelGateway.DuplicateCompositeLayups(values);
        }
        [PythonAPI("Renames a composite layup.", Arguments = "name: str, new_name: str")]
        public void rename(object name, object new_name)
        {
            var item = Resolve(PythonValueConverter.ToText(name, "name", false));
            string text = PythonValueConverter.ToText(new_name, "new_name", false);
            PythonNameComparer.ValidateFormat(text, "new_name", item.Name);
            if (_resolver.GetModel().CompositeLayups.ContainsKey(text) && !PythonNameComparer.AreSame(text, item.Name))
                throw new PythonAPIException("The composite layup '" + text + "' already exists.");
            _resolver.ModelGateway.RenameModelItem(item, text, null);
        }
        [PythonAPI("Activates composite layups.", Arguments = "names: str or list[str]")]
        public void activate(object names) { SetActive(names, true); }
        [PythonAPI("Deactivates composite layups.", Arguments = "names: str or list[str]")]
        public void deactivate(object names) { SetActive(names, false); }
        private string[] Names(object names)
        {
            return (names is string ? new[] { PythonValueConverter.ToText(names, "names", false) }
                : PythonObjectListConverter.ToList<string>(names, "names", "a list of layup names").ToArray())
                .Select(n => Resolve(n).Name).ToArray();
        }
        private void SetActive(object names, bool active)
        {
            var items = Names(names).Select(n => (NamedClass)Resolve(n)).ToArray();
            _resolver.ModelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        private CompositeLayup Build(string oldName, object name, object region, object plies, object cs,
                                     object axis, object symmetric, object tolerance, object active)
        {
            string text = PythonValueConverter.ToText(name, "name", false);
            PythonNameComparer.ValidateFormat(text, "name", oldName);
            if (_resolver.GetModel().CompositeLayups.ContainsKey(text) && !PythonNameComparer.AreSame(text, oldName))
                throw new PythonAPIException("The composite layup '" + text + "' already exists.");
            var value = new CompositeLayup(text, null, RegionTypeEnum.Selection);
            _sections.ApplyCompositeRegion(value, region);
            value.Plies = PythonObjectListConverter.ToList<CompositePlyAPI>(plies, "plies", "a list of pmx.create.composite_ply values")
                .Select(p => p.Value.DeepClone()).ToList();
            if (value.Plies.Count == 0) throw new PythonAPIException("At least one ply is required.");
            value.CoordinateSystemName = cs == null ? null : PythonValueConverter.ToText(cs, "coordinate_system_name", false);
            if (value.CoordinateSystemName != null)
            {
                CaeMesh.CoordinateSystem stored;
                if (!_resolver.GetModel().Mesh.CoordinateSystems.TryGetValue(value.CoordinateSystemName, out stored))
                    throw new PythonAPIException("The coordinate system '" + value.CoordinateSystemName + "' does not exist.");
                value.CoordinateSystemName = stored.Name;
            }
            value.StackingAxis = axis == null ? 3 : PythonValueConverter.ToInt(axis, "stacking_axis");
            value.Symmetric = symmetric != null && PythonValueConverter.ToBool(symmetric, "symmetric");
            value.BoundaryTolerance = tolerance == null ? 1E-6 : PythonValueConverter.ToDouble(tolerance, "boundary_tolerance");
            value.Active = active == null || PythonValueConverter.ToBool(active, "active");
            if (value.StackingAxis < 1 || value.StackingAxis > 3)
                throw new PythonAPIException("stacking_axis must be 1, 2 or 3.");
            if (value.BoundaryTolerance < 0 || value.BoundaryTolerance >= 0.5)
                throw new PythonAPIException("boundary_tolerance must be nonnegative and less than 0.5.");
            foreach (var ply in value.Plies) ply.MaterialName = _sections.GetStoredMaterialName(ply.MaterialName);
            return value;
        }
    }
}
