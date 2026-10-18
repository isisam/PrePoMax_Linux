// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
#if NETFRAMEWORK
using System.Windows.Forms;
#endif

internal static class FormContracts
{
    private static readonly List<Assembly> assemblies = new List<Assembly>();
    private static Type T(string name) => assemblies.Select(a => a.GetType(name)).First(t => t != null);
    private static object New(string name, params object[] args) => Activator.CreateInstance(T(name), args);
    private static object E(string type, string name) => Enum.Parse(T(type), name);
    private static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethods().Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(obj, args);
    private static string Message(Exception e) => e is TargetInvocationException && e.InnerException != null ? Message(e.InnerException) : e.Message;
    [STAThread] public static int Main(string[] args)
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
                foreach (string dir in new[] { args[0], Path.Combine(args[0], "lib") }) {
                    string path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path);
                } return null;
            };
            foreach (string name in new[] { "CaeGlobals", "CaeMesh", "CaeModel", "PrePoMax.ViewModels" }) {
                string p = Path.Combine(args[0], name + ".dll"); if (!File.Exists(p)) p = Path.Combine(args[0], "lib", name + ".dll"); assemblies.Add(Assembly.LoadFrom(p));
            }
            if (args.Length == 4 && args[2] == "--apply") return ApplyContracts.Run(args[0], args[3], args[1]);
            var views = new SortedDictionary<string, object>();
            object Make(string model, string view, params object[] values) { var result = New("PrePoMax." + view, New("CaeModel." + model, values)); views[view] = result; return result; }
            Make("Density", "ViewDensity", new object[] { new double[][] { new double[] { 1, 0 } } });
            Make("Elastic", "ViewElastic", new object[] { new double[][] { new double[] { 1000, .3, 0 } } });
            Make("ElasticEngineeringConstants", "ViewElasticEngineeringConstants", new object[] { new double[][] { new double[] { 1000, 1000, 1000, .3, .3, .3, 400, 400, 400, 0 } } });
            Make("ElasticLamina", "ViewElasticLamina", new object[] { new double[][] { new double[] { 1000, 1000, .3, 400, 400, 400, 0 } } });
            foreach (bool pc in new[] { false, true }) {
                var model = T("CaeModel.UserMaterial").GetMethod("CreateFatczm").Invoke(null, new object[] { pc });
                var view = New("PrePoMax.ViewUserMaterial", model); views["Fatczm-" + pc] = view;
                var rows = (IList)view.GetType().GetProperty("DataPoints").GetValue(view);
                for (int i = 0; i < rows.Count; i++) views["Fatczm-" + pc + "-row" + (i + 1)] = rows[i];
            }
            Make("UserMaterial", "ViewUserMaterial", new double[] { 1, 2 }, 0, false);
            var section = Make("SolidSection", "ViewSolidSection", "Section", "Material", "Part", E("CaeGlobals.RegionTypeEnum", "PartName"), 1d, false);
            Call(section, "PopulateDropDownLists", new[] { "Material", "Other" }, new[] { "Part" }, new string[0], new[] { "Set" }, new string[0], new string[0]);
            var node = New("CaeMesh.FeNodeSet", "NodeSet", new int[] { 1, 2 }); views["ViewNodeSet"] = New("PrePoMax.Forms.ViewNodeSet", null, node);
            views["ViewElementSet"] = New("PrePoMax.Forms.ViewElementSet", New("CaeMesh.FeElementSet", "ElementSet", new int[] { 1 }, false));
            var surface = New("PrePoMax.Forms.ViewFeSurface", New("CaeMesh.FeSurface", "Surface")); Call(surface, "PopulateDropDownList", new object[] { new[] { "Nodes" } }); views["ViewFeSurface"] = surface;
            var step = New("CaeModel.StaticStep", "Step"); var vs = New("PrePoMax.ViewStaticStep", step, true); views["ViewStaticStep"] = vs;
            foreach (string control in new[] { "Reset", "TimeIncrementation", "Field", "Contact" }) {
                string name = "View" + control + "StepControlParameter";
                string ns = "PrePoMax.PropertyViews.";
                views[name] = New(ns + name, New("CaeModel." + control + "StepControlParameter"));
            }
            object nodeRegion = E("CaeGlobals.RegionTypeEnum", "NodeSetName");
            var bc = Make("FixedBC", "ViewFixedBC", "Fixed", "Nodes", nodeRegion, false);
            Call(bc, "PopulateDropDownLists", new[] { "Nodes" }, new[] { "Surface" }, new[] { "RP" });
            var dr = Make("DisplacementRotation", "ViewDisplacementRotation", "Displacement", "Nodes", nodeRegion, false, false, 0d);
            Call(dr, "PopulateDropDownLists", new[] { "Nodes" }, new[] { "Surface" }, new[] { "RP" }, new string[0], new string[0], new string[0]);
            var force = Make("CLoad", "ViewCLoad", "Force", "Nodes", nodeRegion, 1d, 0d, 0d, false, false, 0d, false);
            Call(force, "PopulateDropDownLists", new[] { "Nodes" }, new[] { "RP" }, new string[0], new string[0]);
            var moment = Make("MomentLoad", "ViewMomentLoad", "Moment", "Nodes", nodeRegion, 1d, 0d, 0d, false, false, 0d);
            Call(moment, "PopulateDropDownLists", new[] { "Nodes" }, new[] { "RP" }, new string[0]);
            var pressure = Make("DLoad", "ViewDLoad", "Pressure", "Surface", E("CaeGlobals.RegionTypeEnum", "SurfaceName"), 1d, false, false, 0d, false);
            Call(pressure, "PopulateDropDownLists", new[] { "Surface" }, new string[0], new string[0]);
            var snapshots = new SortedDictionary<string, object>();
            foreach (var pair in views) snapshots[pair.Key] = Describe(pair.Value);
            TypeDescriptor.GetProperties(section)["RegionType"].SetValue(section, "Element set name"); snapshots["section-element-set"] = Describe(section);
            TypeDescriptor.GetProperties(surface)["CreateSurfaceFrom"].SetValue(surface, "NodeSet"); snapshots["surface-node-set"] = Describe(surface);
            foreach (string type in new[] { "Automatic", "Direct" }) {
                TypeDescriptor.GetProperties(vs)["IncrementationType"].SetValue(vs, E("CaeModel.IncrementationTypeEnum", type)); Call(vs, "UpdateVisibility"); snapshots["step-" + type] = Describe(vs);
            }
#if NETFRAMEWORK
            string json = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(snapshots);
#else
            string json = System.Text.Json.JsonSerializer.Serialize(snapshots);
#endif
            File.WriteAllText(args[1], json); Console.WriteLine("PASS " + snapshots.Count + " form snapshots (metadata, dropdowns, three invalid names where available)"); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static object Describe(object view)
    {
        PropertyDescriptor[] properties;
#if NETFRAMEWORK
        using (var form = new Form { ShowInTaskbar = false, Width = 700, Height = 700 })
        using (var grid = new PropertyGrid { Dock = DockStyle.Fill, PropertySort = PropertySort.CategorizedAlphabetical, SelectedObject = view }) {
            form.Controls.Add(grid); form.Show(); Application.DoEvents();
            var root = grid.SelectedGridItem; while (root.Parent != null) root = root.Parent;
            properties = root.GridItems.Cast<GridItem>().SelectMany(c => c.GridItemType == GridItemType.Category ? c.GridItems.Cast<GridItem>() : new[] { c })
                .Where(i => i.PropertyDescriptor != null).Select(i => i.PropertyDescriptor).ToArray(); form.Hide();
        }
#else
        properties = PrePoMax.Avalonia.DescriptorPropertyGrid.Properties(view);
#endif
        var invalid = new List<object>();
        var name = TypeDescriptor.GetProperties(view)["Name"];
        if (name != null && !name.IsReadOnly && name.IsBrowsable && !view.GetType().Name.EndsWith("DataPoint")) foreach (string text in new[] { "", "Bad Name", "Missing" }) {
            object previous = name.GetValue(view); string error = null;
            try { name.SetValue(view, text); } catch (Exception e) { error = Message(e); }
            if (error == null || !Equals(previous, name.GetValue(view))) throw new Exception("Invalid name accepted or prior value lost: " + view.GetType().Name);
            invalid.Add(new { input = text, error });
        }
        return new { properties = properties.Select(p => { var ctx = new Context(view, p); return new {
            p.Name, p.DisplayName, p.Category, p.Description, p.IsReadOnly, converter = p.Converter.GetType().FullName,
            exclusive = p.Converter.GetStandardValuesExclusive(ctx), values = p.Converter.GetStandardValuesSupported(ctx) ?
#if NETFRAMEWORK
                p.Converter.GetStandardValues(ctx)?.Cast<object>()
#else
                PrePoMax.Avalonia.DescriptorPropertyGrid.StandardValues(p, ctx)
#endif
                .Select(v => p.Converter.ConvertToString(ctx, CultureInfo.InvariantCulture, v)).ToArray() : null
        }; }).ToArray(), invalid };
    }
    private sealed class Context(object value, PropertyDescriptor p) : ITypeDescriptorContext
    {
        public IContainer Container => null; public object Instance => value; public PropertyDescriptor PropertyDescriptor => p;
        public object GetService(Type type) => null; public bool OnComponentChanging() => true; public void OnComponentChanged() { }
    }
}
