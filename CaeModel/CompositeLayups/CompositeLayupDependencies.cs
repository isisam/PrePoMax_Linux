using System;
using System.Linq;
using CaeGlobals;

namespace CaeModel
{
    // References remain named on deletion so the editor can explain and repair them.
    // Shared by the controller and headless data-layer checks.
    public static class CompositeLayupDependencies
    {
        private static bool Same(string a, string b) { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
        public static void RenameMaterial(FeModel model, string oldName, string newName)
        {
            foreach (var ply in model.CompositeLayups.Values.SelectMany(l => l.Plies))
                if (Same(ply.MaterialName, oldName)) ply.MaterialName = newName;
        }
        public static void RenameCoordinateSystem(FeModel model, string oldName, string newName)
        {
            foreach (var layup in model.CompositeLayups.Values)
                if (Same(layup.CoordinateSystemName, oldName)) layup.CoordinateSystemName = newName;
        }
        public static void RenameRegion(FeModel model, RegionTypeEnum type, string oldName, string newName)
        {
            foreach (var layup in model.CompositeLayups.Values)
                if (layup.RegionType == type && Same(layup.RegionName, oldName)) layup.RegionName = newName;
        }
        public static CompositeLayup Duplicate(FeModel model, string name)
        {
            var copy = model.CompositeLayups[name].DeepClone();
            copy.Name = model.CompositeLayups.GetNextNumberedKey(NamedClass.GetNameWithoutLastValue(name));
            if (copy.CreationData != null) copy.RegionType = RegionTypeEnum.Selection;
            return copy;
        }
    }
}
