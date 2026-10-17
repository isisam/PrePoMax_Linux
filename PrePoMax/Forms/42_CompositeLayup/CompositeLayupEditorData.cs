using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    // Detached edit session. The form and headless tests use the same conversion and table operations.
    public class CompositeLayupEditorData
    {
        public CompositeLayup Layup { get; private set; }
        public CompositeLayupEditorData(CompositeLayup layup)
        {
            Layup = layup.DeepClone();
            if (Layup.CreationData != null) Layup.RegionType = RegionTypeEnum.Selection;
        }
        public CompositeLayup ReadBack() { return Layup.DeepClone(); }
        public int Add(int index, string material)
        {
            var names = Layup.Plies.Select(p => p.Name).ToArray();
            var ply = new CompositePly(names.GetNextNumberedKey("Ply"), material, 1, 0);
            index = Math.Max(0, Math.Min(index, Layup.Plies.Count));
            Layup.Plies.Insert(index, ply);
            return index;
        }
        public int Duplicate(int index)
        {
            var ply = Layup.Plies[index].DeepClone();
            ply.Name = Layup.Plies.Select(p => p.Name).ToArray().GetNextNumberedKey(NamedClass.GetNameWithoutLastValue(ply.Name));
            Layup.Plies.Insert(index + 1, ply);
            return index + 1;
        }
        public int Move(int index, int offset)
        {
            int target = Math.Max(0, Math.Min(index + offset, Layup.Plies.Count - 1));
            var ply = Layup.Plies[index];
            Layup.Plies.RemoveAt(index);
            Layup.Plies.Insert(target, ply);
            return target;
        }
        public void Reverse() { Layup.Plies.Reverse(); }
        public void Delete(int index) { Layup.Plies.RemoveAt(index); }
        public static TypeConverter Converter(int column)
        {
            return column == 2 ? (TypeConverter)new StringLengthConverter() : new StringAngleDegConverter();
        }
        public void SetCell(int row, int column, string text)
        {
            var ply = Layup.Plies[row];
            if (column == 0) ply.Name = text;
            else if (column == 1) ply.MaterialName = text;
            else if (column == 2 || column == 3)
            {
                double value = (double)Converter(column).ConvertFromString(text);
                if (double.IsNaN(value) || double.IsInfinity(value) || (column == 2 && value <= 0))
                    throw new CaeException(column == 2 ? "Thickness must be finite and positive." : "Angle must be finite.");
                if (column == 2) ply.Thickness = value; else ply.AngleDeg = value;
            }
            else if (column == 4)
            {
                int count;
                if (string.IsNullOrWhiteSpace(text)) ply.ElementsThroughThickness = null;
                else if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out count) && count > 0)
                    ply.ElementsThroughThickness = count;
                else throw new CaeException("Elements must be a positive integer, or empty.");
            }
            else throw new ArgumentOutOfRangeException(nameof(column));
        }
        public List<string> Validate(FeModel model, string oldName, out List<ExpandedCompositePly> expanded)
        {
            expanded = null;
            var errors = new List<string>();
            if (model.CompositeLayups.ContainsKey(Layup.Name) && !string.Equals(oldName, Layup.Name, StringComparison.OrdinalIgnoreCase))
                errors.Add("The composite layup name already exists.");
            foreach (var ply in Layup.Plies)
            {
                if (ply.MaterialName == null || !model.Materials.ContainsValidActiveKey(ply.MaterialName))
                    errors.Add("Ply '" + ply.Name + "': missing, inactive or invalid material '" + ply.MaterialName + "'.");
                if (ply.Thickness <= 0 || double.IsNaN(ply.Thickness) || double.IsInfinity(ply.Thickness))
                    errors.Add("Ply '" + ply.Name + "': thickness must be finite and positive.");
            }
            if (errors.Count == 0)
            {
                try { expanded = CompositeLayupExpansion.Preview(model, Layup, oldName); }
                catch (CaeException ex) { errors.Add(ex.Message); }
            }
            return errors;
        }
    }
}
