// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using PrePoMax.PropertyViews;

namespace PrePoMax.Avalonia;

public sealed record FormPage(string Title, object View, IList? DataRows = null)
{
    public override string ToString() => Title;
}

/// <summary>Ports FrmProperties.CheckName and each supported form's prepare/apply logic.
/// Sessions own detached drafts; only the Controller command methods publish changes.</summary>
public abstract class EntityFormSession(Controller controller, string? originalName)
{
    protected Controller Controller { get; } = controller;
    public string? OriginalName { get; private set; } = originalName;
    public abstract string Kind { get; }
    public abstract NamedClass Draft { get; }
    public abstract IReadOnlyList<FormPage> Pages { get; }
    public virtual bool OkAddNew => OriginalName == null;
    public virtual void Changed(string property) { }
    public abstract void Validate();
    protected abstract void Publish();
    public void Apply() { Validate(); Publish(); OriginalName = Draft.Name; }
    protected void CheckName(IEnumerable<string> names, string label) => CheckName(OriginalName, Draft.Name, names, label);
    public static void CheckName(string? previous, string name, IEnumerable<string> names, string label)
    {
        if ((previous == null || !string.Equals(previous, name, StringComparison.OrdinalIgnoreCase)) && names.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new CaeException("The selected " + label + " name already exists. Uppercase and lowercase letters are regarded as equal.");
    }
}

public sealed class MaterialFormSession : EntityFormSession
{
    private readonly Material material;
    private readonly List<ViewMaterialProperty> properties;
    public bool TemperatureDependent
    {
        get => material.TemperatureDependent;
        set
        {
            material.TemperatureDependent = value;
            foreach (var view in properties.OfType<ViewThermalExpansion>()) view.SetTemperatureDependence(value);
        }
    }
    public bool ShowScalar(object view) => !TemperatureDependent || view is not (ViewDensity or ViewElastic or ViewElasticEngineeringConstants or ViewElasticLamina or ViewThermalConductivity or ViewSpecificHeat);
    public bool ShowRows(object view) => view is ViewPlastic or ViewUserMaterial || TemperatureDependent && view is ViewDensity or ViewElastic or ViewElasticEngineeringConstants or ViewElasticLamina or ViewThermalExpansion or ViewThermalConductivity or ViewSpecificHeat;
    public override string Kind => "Material";
    public override NamedClass Draft => material;
    public override IReadOnlyList<FormPage> Pages => properties.Select(v => new FormPage(v.Name, v,
        v.GetType().GetProperty("DataPoints")?.GetValue(v) as IList)).ToArray();
    public static readonly string[] AvailableProperties = ["Density", "Elastic", "Engineering Constants", "Lamina", "Elastic with density",
        "Plastic", "Thermal expansion", "Thermal conductivity", "Specific heat", "Slip wear", "User Material", "Fatigue CZM (FATCZM)", "Fatigue CZM pre-crack"];
    public MaterialFormSession(Controller c, Material? source = null) : base(c, source?.Name)
    {
        material = source?.DeepClone() ?? new Material(c.GetMaterialNames().GetNextNumberedKey("Material"));
        properties = material.Properties.Select(Wrap).ToList();
    }
    private ViewMaterialProperty Wrap(MaterialProperty p) => p switch
    {
        Density x => new ViewDensity(x), Elastic x => new ViewElastic(x),
        ElasticEngineeringConstants x => new ViewElasticEngineeringConstants(x), ElasticLamina x => new ViewElasticLamina(x),
        ElasticWithDensity x => new ViewElasticWithDensity(x), Plastic x => new ViewPlastic(x),
        ThermalExpansion x => new ViewThermalExpansion(x, material.TemperatureDependent), ThermalConductivity x => new ViewThermalConductivity(x),
        SpecificHeat x => new ViewSpecificHeat(x), SlipWear x => new ViewSlipWear(x), UserMaterial x => new ViewUserMaterial(x),
        _ => throw new NotSupportedException("Unsupported material property: " + p.GetType().Name)
    };
    public void AddProperty(string type)
    {
        MaterialProperty p = type switch
        {
            "Density" => new Density(new double[][] { [0, 0] }),
            "Elastic" => new Elastic(new double[][] { [0, 0, 0] }),
            "Engineering Constants" => new ElasticEngineeringConstants(new double[][] { new double[ElasticEngineeringConstants.NumberOfValues] }),
            "Lamina" => new ElasticLamina(new double[][] { new double[ElasticLamina.NumberOfValues] }),
            "Elastic with density" => new ElasticWithDensity(0, 0, 0),
            "Plastic" => new Plastic(new double[][] { [0, 0, 0] }),
            "Thermal expansion" => new ThermalExpansion(new double[][] { [0, 0] }),
            "Thermal conductivity" => new ThermalConductivity(new double[][] { [0, 0] }),
            "Specific heat" => new SpecificHeat(new double[][] { [0, 0] }),
            "Slip wear" => new SlipWear(0, 0),
            "User Material" => new UserMaterial(new double[] { 0 }),
            "Fatigue CZM (FATCZM)" => UserMaterial.CreateFatczm(),
            "Fatigue CZM pre-crack" => UserMaterial.CreateFatczm(true),
            _ => throw new NotSupportedException(type)
        };
        if (properties.Any(v => v.Name == Wrap(p).Name)) throw new CaeException("This material property has already been added.");
        // FrmMaterial refuses a second elasticity/density choice and retains the draft.
        bool Elasticity(MaterialProperty x) => x is Elastic or ElasticEngineeringConstants or ElasticLamina or UserMaterial or ElasticWithDensity;
        if (Elasticity(p) && properties.Any(v => Elasticity(v.GetBase()))) throw new CaeException("A material can contain only one elastic property.");
        if (p is Density or ElasticWithDensity && properties.Any(v => v is ViewDensity or ViewElasticWithDensity)) throw new CaeException("A material can contain only one density.");
        properties.Add(Wrap(p));
    }
    public void RemoveProperty(int index) => properties.RemoveAt(index);
    public void MoveProperty(int from, int to) { var p = properties[from]; properties.RemoveAt(from); properties.Insert(to, p); }
    public void PrefixFatczmName()
    {
        if (!material.Name.StartsWith("USERFATCZM", StringComparison.Ordinal)) material.Name = "USERFATCZM" + material.Name;
    }
    public override void Validate()
    {
        CheckName(Controller.GetMaterialNames(), "material");
        var result = new Material(material.Name) { Description = material.Description, TemperatureDependent = material.TemperatureDependent };
        int density = 0, elasticity = 0;
        foreach (var v in properties)
        {
            var p = v.GetBase(); p.CheckEquations();
            if (p is Elastic or ElasticEngineeringConstants or ElasticLamina or UserMaterial or ElasticWithDensity && ++elasticity > 1)
                throw new CaeException("A material can contain only one elastic property.");
            if (p is UserMaterial um) um.CheckMaterialName(material.Name);
            if (p is Density or ElasticWithDensity && ++density > 1) throw new CaeException("A material can contain only one density.");
            if (v is ViewDensity vd && vd.DataPoints.Any(r => r.Density.Value <= 0)) throw new CaeException("The density must be larger than 0.");
            if (v is ViewElastic ve && ve.GetYoungsModulusValue() <= 0) throw new CaeException("The Young's modulus must be larger than 0.");
            if (v is ViewElasticWithDensity ed)
            {
                if (ed.GetYoungsModulusValue() <= 0) throw new CaeException("The Young's modulus must be larger than 0.");
                if (ed.GetDensityValue() <= 0) throw new CaeException("The density must be larger than 0.");
            }
            if (material.TemperatureDependent && p is ElasticEngineeringConstants ec) ec.CheckTemperatures();
            if (material.TemperatureDependent && p is ElasticLamina el) el.CheckTemperatures();
            if (v is ViewThermalExpansion ex && ex.DataPoints.Any(r => r.ThermalExpansion.Value <= 0)) throw new CaeException("The thermal expansion coefficient must be larger than 0.");
            if (v is ViewThermalConductivity tc && tc.DataPoints.Any(r => r.ThermalConductivity.Value <= 0)) throw new CaeException("The thermal conductivity coefficient must be larger than 0.");
            if (v is ViewSpecificHeat sh && sh.DataPoints.Any(r => r.SpecificHeat.Value <= 0)) throw new CaeException("The thermal conductivity coefficient must be larger than 0.");
            if (v is ViewSlipWear sw)
            {
                if (sw.GetHardnessValue() <= 0) throw new CaeException("The hardness must be larger than 0.");
                if (sw.GetWearCoefficientValue() <= 0) throw new CaeException("The wear coefficient must be larger than 0.");
            }
            result.AddProperty(p);
        }
        material.Properties.Clear(); material.Properties.AddRange(result.Properties);
    }
    protected override void Publish()
    {
        if (OriginalName == null) Controller.AddMaterialCommand(material.DeepClone());
        else Controller.ReplaceMaterialCommand(OriginalName, material.DeepClone());
    }
}

public sealed class SectionFormSession : EntityFormSession
{
    private readonly SolidSection section;
    public ViewSolidSection View { get; }
    public override string Kind => "Section";
    public override NamedClass Draft => section;
    public override IReadOnlyList<FormPage> Pages => [new("Solid Section", View)];
    public SectionFormSession(Controller c, SolidSection? source = null) : base(c, source?.Name)
    {
        if (source == null && c.GetMaterialNames().Length == 0) throw new CaeException("There is no material defined for the solid section definition.");
        section = source?.DeepClone() ?? new SolidSection(c.GetSectionNames().GetNextNumberedKey("Solid_Section"), c.GetMaterialNames()[0],
            c.GetModelPartNames().FirstOrDefault() ?? "", RegionTypeEnum.PartName, 1, c.Model.Properties.ModelSpace.IsTwoD());
        View = new ViewSolidSection(section);
        View.PopulateDropDownLists(c.GetMaterialNames(), c.GetModelPartNames(), null!, c.GetUserElementSetNames(), null!, null!);
    }
    public override void Changed(string property) => View.UpdateRegionVisibility();
    public override void Validate()
    {
        CheckName(Controller.GetSectionNames(), "section"); section.CheckEquations();
        if (section.RegionType == RegionTypeEnum.Selection && (section.CreationIds == null || section.CreationIds.Length == 0))
            throw new CaeException("The section selection must contain at least one item.");
        bool twoD = Controller.Model.Properties.ModelSpace.IsTwoD();
        bool compatible = section.RegionType switch
        {
            RegionTypeEnum.PartName => Controller.Model.Mesh.Parts.TryGetValue(section.RegionName, out var p) && p.PartType == (twoD ? CaeMesh.PartType.Shell : CaeMesh.PartType.Solid),
            RegionTypeEnum.ElementSetName => Controller.Model.Mesh.ElementSets.TryGetValue(section.RegionName, out var e) &&
                (twoD ? Controller.AreElementsAllShellElements(e.Labels) : Controller.AreElementsAllSolidElements3D(e.Labels)),
            _ => false
        };
        if (!compatible) throw new CaeException("The section type and the section region are not compatible.");
        if (section.TwoD && section.Thickness.Value <= 0) throw new CaeException("The section thickness must be larger than 0.");
        if (!Controller.Model.Materials.ContainsKey(section.MaterialName)) throw new CaeException("The section material does not exist.");
    }
    protected override void Publish()
    {
        if (OriginalName == null) Controller.AddSectionCommand(section.DeepClone());
        else Controller.ReplaceSectionCommand(OriginalName, section.DeepClone());
    }
}
