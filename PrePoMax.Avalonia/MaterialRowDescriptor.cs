// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using CaeGlobals;

namespace PrePoMax.Avalonia;

/// <summary>FrmMaterial hides the temperature column outside temperature mode.
/// This per-row descriptor projection leaves the shared ViewModel unchanged.</summary>
internal sealed class MaterialRowDescriptor(object row, bool temperature, UnitSystem units) : CustomTypeDescriptor(TypeDescriptor.GetProvider(row).GetTypeDescriptor(row))
{
    public override PropertyDescriptorCollection GetProperties() => GetProperties(null);
    public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => new(
        TypeDescriptor.GetProperties(row).Cast<PropertyDescriptor>().Select(p => (PropertyDescriptor)new Cell(row, p, temperature, units)).ToArray());
    private sealed class Cell(object row, PropertyDescriptor property, bool temperature, UnitSystem units) : PropertyDescriptor(property,
        property.Name == nameof(TempDataPoint.TemperatureEq) && !temperature ? [BrowsableAttribute.No] : [])
    {
        public override Type ComponentType => typeof(MaterialRowDescriptor);
        public override string DisplayName => property.DisplayName.Replace("?", property.Name switch {
            nameof(TempDataPoint.TemperatureEq) => units.TemperatureUnitAbbreviation,
            nameof(DensityDataPoint.DensityEq) => units.DensityUnitAbbreviation,
            nameof(ThermalExpansionDataPoint.ThermalExpansionEq) => units.ThermalExpansionUnitAbbreviation,
            nameof(ThermalConductivityDataPoint.ThermalConductivityEq) => units.ThermalConductivityUnitAbbreviation,
            nameof(SpecificHeatDataPoint.SpecificHeatEq) => units.SpecificHeatUnitAbbreviation,
            var name when name.StartsWith("YoungsModulus") || name.StartsWith("ShearModulus") || name == nameof(PlasticDataPoint.StressEq) => units.PressureUnitAbbreviation,
            _ => "/" });
        public override Type PropertyType => property.PropertyType;
        public override bool IsReadOnly => property.IsReadOnly;
        public override TypeConverter Converter => property.Converter;
        public override object? GetValue(object? component) => property.GetValue(row);
        public override void SetValue(object? component, object? value) => property.SetValue(row, value);
        public override bool CanResetValue(object component) => property.CanResetValue(row);
        public override void ResetValue(object component) => property.ResetValue(row);
        public override bool ShouldSerializeValue(object component) => property.ShouldSerializeValue(row);
    }
}
