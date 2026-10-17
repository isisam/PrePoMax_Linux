using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using CaeGlobals;
using CaeModel;
using CaeMesh;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    public class ViewCompositeLayup : ViewMultiRegion
    {
        private readonly CompositeLayup _layup;
        [Category("Data"), OrderedDisplayName(0, 10, "Name")]
        public string Name { get { return _layup.Name; } set { _layup.Name = value; } }
        [Category("Region"), OrderedDisplayName(0, 10, "Region type")]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        [Category("Region"), OrderedDisplayName(1, 10, "Selection")]
        [Editor(typeof(ItemSetDataEditor), typeof(UITypeEditor))]
        public ItemSetData Selection { get { return new ItemSetData(_layup.CreationIds); } set { _layup.CreationIds = value.ItemIds; } }
        [Category("Region"), OrderedDisplayName(2, 10, "Part")]
        public string PartName { get { return _layup.RegionName; } set { _layup.RegionName = value; } }
        [Category("Region"), OrderedDisplayName(3, 10, "Element set")]
        public string ElementSetName { get { return _layup.RegionName; } set { _layup.RegionName = value; } }
        [Category("Stack"), OrderedDisplayName(0, 10, "Coordinate system")]
        [Description("Rectangular coordinate system. Global uses the model X/Y/Z axes.")]
        public string CoordinateSystemName
        {
            get { return _layup.CoordinateSystemName ?? "Global"; }
            set { _layup.CoordinateSystemName = value == "Global" ? null : value; }
        }
        [Category("Stack"), OrderedDisplayName(1, 10, "Stacking axis")]
        [Description("Axis normal to the plies. Positive angles follow the right-hand rule about this axis.")]
        public int StackingAxis
        {
            get { return _layup.StackingAxis; }
            set { if (value < 1 || value > 3) throw new CaeException("Stacking axis must be 1, 2 or 3."); _layup.StackingAxis = value; }
        }
        [Category("Stack"), OrderedDisplayName(2, 10, "Symmetric")]
        [Description("Append the complete reversed ply list, including the centre ply.")]
        public bool Symmetric { get { return _layup.Symmetric; } set { _layup.Symmetric = value; } }
        [Category("Stack"), OrderedDisplayName(3, 10, "Boundary tolerance")]
        [Description("Relative to total stack thickness; finite and in [0, 0.5). Does not repair a nonconforming mesh.")]
        public double BoundaryTolerance { get { return _layup.BoundaryTolerance; } set { _layup.BoundaryTolerance = value; } }

        public ViewCompositeLayup(CompositeLayup layup)
        {
            _layup = layup;
            SetBase(layup, new Dictionary<RegionTypeEnum, string> {
                { RegionTypeEnum.Selection, nameof(Selection) },
                { RegionTypeEnum.PartName, nameof(PartName) },
                { RegionTypeEnum.ElementSetName, nameof(ElementSetName) } });
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }
        public CompositeLayup GetBase() { return _layup; }
        public void Populate(FeModel model)
        {
            // Include missing references so opening an invalid layup never silently changes its data.
            Func<IEnumerable<string>, string, string[]> withCurrent = (values, current) =>
                values.Concat(current == null ? new string[0] : new[] { current }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            PopulateDropDownLists(new Dictionary<RegionTypeEnum, string[]> {
                { RegionTypeEnum.Selection, new[] { "Selection" } },
                { RegionTypeEnum.PartName, withCurrent(model.Mesh.Parts.Keys, _layup.RegionType == RegionTypeEnum.PartName ? _layup.RegionName : null) },
                { RegionTypeEnum.ElementSetName, withCurrent(model.Mesh.ElementSets.Values.Where(s => !s.Internal).Select(s => s.Name),
                    _layup.RegionType == RegionTypeEnum.ElementSetName ? _layup.RegionName : null) } });
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(CoordinateSystemName),
                new[] { "Global" }.Concat(model.Mesh.CoordinateSystems.Values.Where(c => c.Type == CoordinateSystemTypeEnum.Rectangular).Select(c => c.Name)).ToArray());
            var axis = DynamicCustomTypeDescriptor.GetProperty(nameof(StackingAxis));
            axis.StatandardValues.Clear();
            axis.PropertyFlags |= PropertyFlags.ExclusiveStandardValues;
            foreach (int value in new[] { 1, 2, 3 }) axis.StatandardValues.Add(new StandardValueAttribute(value));
        }
    }
}
