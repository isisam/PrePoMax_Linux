// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class Program
{
    private static string runtime;
    private static Assembly app, shared;
    private static Type Find(string name) => shared?.GetType(name) ?? app?.GetType(name);
    private static string TypeName(Type t) => t.FullName;
    private static object Describe(PropertyDescriptor p) => new {
        p.Name, Type = TypeName(p.PropertyType), p.DisplayName, p.Category, p.Description,
        p.IsReadOnly, p.IsBrowsable, Converter = TypeName(p.Converter.GetType()),
        Editor = ((EditorAttribute)p.Attributes[typeof(EditorAttribute)])?.EditorTypeName?.Split(',')[0]
    };
    private static string Serialize(object value)
    {
#if NETFRAMEWORK
        return new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(value);
#else
        return System.Text.Json.JsonSerializer.Serialize(value);
#endif
    }
    private static string[] Names(string path)
    {
#if NETFRAMEWORK
        return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(path));
#else
        return System.Text.Json.JsonSerializer.Deserialize<string[]>(File.ReadAllText(path));
#endif
    }
    private static object New(string type, params object[] args) => Activator.CreateInstance(Find(type) ?? Assembly.Load("CaeModel").GetType(type), args);
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static int Main(string[] args)
    {
        try
        {
            runtime = Path.GetFullPath(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
                string name = new AssemblyName(e.Name).Name;
                foreach (string dir in new[] { runtime, Path.Combine(runtime, "lib") }) {
                    string path = Path.Combine(dir, name + ".dll");
                    if (!File.Exists(path)) path = Path.Combine(dir, name + ".exe");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            string dll = Path.Combine(runtime, "PrePoMax.ViewModels.dll");
            if (!File.Exists(dll)) dll = Path.Combine(runtime, "lib", "PrePoMax.ViewModels.dll");
            if (File.Exists(dll)) shared = Assembly.LoadFrom(dll);
            if (File.Exists(Path.Combine(runtime, "PrePoMax.exe"))) app = Assembly.LoadFrom(Path.Combine(runtime, "PrePoMax.exe"));
            var result = new SortedDictionary<string, object>();
            var resources = new System.Resources.ResourceManager("PrePoMax.Properties.Resources", shared ?? app);
            var strings = new SortedDictionary<string, string>();
            foreach (string key in Names(Path.Combine(Path.GetDirectoryName(args[1]), "view-resource-keys.json")))
                strings[key] = resources.GetString(key, System.Globalization.CultureInfo.InvariantCulture);
            // Even an empty text catalogue must have a valid manifest: enum converters
            // probe it for optional localized names, and must receive null, not throw.
            Require(resources.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, true) != null, "Neutral resource manifest missing");
            result["$resources"] = strings;
            int editorCount = 0, identityCount = 0;
            foreach (string name in Names(args[1]))
            {
                Type t = Find(name); Require(t != null, "Missing type " + name);
                foreach (EditorAttribute editor in t.GetCustomAttributes(typeof(EditorAttribute), true).Concat(
                    t.GetProperties().SelectMany(p => p.GetCustomAttributes(typeof(EditorAttribute), true)))) {
#if NETFRAMEWORK
                    Require(Type.GetType(editor.EditorTypeName, true) != null, "Unresolved editor " + editor.EditorTypeName);
                    Require(Type.GetType(editor.EditorBaseTypeName, true) != null, "Unresolved editor base");
#else
                    Require(PrePoMax.Avalonia.PropertyEditorMap.Resolve(editor) != PrePoMax.Avalonia.PropertyEditorKind.Unsupported, "Missing Avalonia editor mapping " + editor.EditorTypeName);
#endif
                    editorCount++;
                }
                if (shared != null) {
                    Type binderType = Assembly.Load("CaeGlobals").GetType("CaeGlobals.PmxSerializationBinder");
                    object binder = binderType.GetField("Instance").GetValue(null);
                    Type actual = (Type)binderType.GetMethod("BindToType").Invoke(binder, new object[] { "PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null", name });
                    Require(actual == t, "Legacy binder failed " + name); identityCount++;
                    if (app != null) Require(app.GetType(name, true) == t, "Windows forwarder failed " + name);
                    foreach (Type graph in new[] { t.MakeArrayType(), typeof(List<>).MakeGenericType(t) }) {
                        object[] wire = { graph, null, null };
                        binderType.GetMethod("BindToName").Invoke(binder, wire);
                        string asm = (string)wire[1] ?? graph.Assembly.FullName;
                        string wireName = (string)wire[2] ?? graph.FullName;
                        Require(!wireName.Contains("PrePoMax.ViewModels,"), "Moved generic wire identity leaked");
                        Require((Type)binderType.GetMethod("BindToType").Invoke(binder, new object[] { asm, wireName }) == graph, "Generic/array identity failed " + name);
                    }
                }
                result[name] = new {
                    Base = t.BaseType?.FullName,
                    Properties = TypeDescriptor.GetProperties(t).Cast<PropertyDescriptor>().OrderBy(p => p.Name).Select(Describe).ToArray(),
                    Fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Select(f => f.Name + ":" + TypeName(f.FieldType)).OrderBy(x => x).ToArray(),
                    Constructors = t.GetConstructors().Select(c => string.Join(",", c.GetParameters().Select(p => TypeName(p.ParameterType)))).OrderBy(x => x).ToArray()
                };
            }
            // Exercise actual per-instance dynamic region dropdowns and visibility, rather than
            // merely reading reflection attributes. The same code runs against the L2c binary.
            Type region = Assembly.Load("CaeGlobals").GetType("CaeGlobals.RegionTypeEnum");
            object section = New("CaeModel.SolidSection", "Section", "Material", "Part", Enum.Parse(region, "PartName"), 1d, false);
            object view = New("PrePoMax.ViewSolidSection", section);
            view.GetType().GetMethods().Single(m => m.Name == "PopulateDropDownLists" && m.GetParameters().Length == 6).Invoke(view, new object[] {
                new[] { "Material", "Other" }, new[] { "Part" }, new string[0], new[] { "Set" }, new string[0], new string[0] });
            result["$section-part"] = TypeDescriptor.GetProperties(view).Cast<PropertyDescriptor>().Select(Describe).ToArray();
            PropertyDescriptor rp = TypeDescriptor.GetProperties(view)["RegionType"];
            rp.SetValue(view, "Element set name");
            Require((string)section.GetType().GetProperty("RegionName").GetValue(section) == "Set", "Region switch did not bind the dropdown value");
            result["$section-set"] = TypeDescriptor.GetProperties(view).Cast<PropertyDescriptor>().Select(Describe).ToArray();
            result["$section-values"] = new {
                Regions = rp.Converter.GetStandardValues(new Context(view, rp)).Cast<object>().Select(v => v.ToString()).ToArray(),
                Materials = TypeDescriptor.GetProperties(view)["MaterialName"].Converter.GetStandardValues(new Context(view, TypeDescriptor.GetProperties(view)["MaterialName"])).Cast<object>().Select(v => v.ToString()).ToArray()
            };
            Type userMaterial = Assembly.Load("CaeModel").GetType("CaeModel.UserMaterial");
            foreach (bool preCrack in new[] { false, true }) {
                object material = userMaterial.GetMethod("CreateFatczm").Invoke(null, new object[] { preCrack });
                object userView = New("PrePoMax.ViewUserMaterial", material);
                IList points = (IList)userView.GetType().GetProperty("DataPoints").GetValue(userView);
                Require(points.Count == 26, "FATCZM must retain the 26-constant preset");
                var rows = points.Cast<object>().Select(point => new {
                    Name = point.GetType().GetProperty("Name").GetValue(point),
                    Description = point.GetType().GetProperty("Description").GetValue(point),
                    Value = point.GetType().GetProperty("ValueEq").GetValue(point).ToString()
                }).ToArray();
                PropertyDescriptor depvar = TypeDescriptor.GetProperties(userView)["NumberOfStateVariables"];
                bool invalid = false;
                try { depvar.SetValue(userView, -1); } catch { invalid = true; }
                Require(invalid && (int)depvar.GetValue(userView) == 6, "DEPVAR validation must preserve the prior value");
                Require(TypeDescriptor.GetProperties(userView)["Fatczm"].IsReadOnly, "Preset identity is read-only");
                Require(ReferenceEquals(material, userView.GetType().GetMethod("GetBase").Invoke(userView, null)), "GetBase retains the typed FATCZM property");
                result["$fatczm-" + preCrack] = rows;
            }
            File.WriteAllText(args[2], Serialize(result));
            Console.WriteLine("PASS " + result.Count + " snapshots, " + editorCount + " editor attributes, " + identityCount + " legacy identities");
            if (shared != null) {
                Type binderType = Assembly.Load("CaeGlobals").GetType("CaeGlobals.PmxSerializationBinder");
                object binder = binderType.GetField("Instance").GetValue(null);
                Type container = Assembly.Load("PrePoMax.PropertyMetadata").GetType("DynamicTypeDescriptor.MultiChoiceContainer");
                Type graph = typeof(List<>).MakeGenericType(container);
                object[] wire = { graph, null, null };
                binderType.GetMethod("BindToName").Invoke(binder, wire);
                Require(((string)wire[2]).Contains("CaeGlobals,") && !((string)wire[2]).Contains("PrePoMax.PropertyMetadata,"), "Metadata generic wire identity leaked");
                string[] forbidden = { "System.Windows.Forms", "System.Drawing.Common", "System.Design", "System.Drawing.Design", "UserControls", "vtkControl", "Kitware.VTK" };
                Require(!shared.GetReferencedAssemblies().Any(a => forbidden.Contains(a.Name)), "UI assembly dependency leaked into ViewModels");
                Console.WriteLine("PASS shared assembly has no Windows UI/VTK references");
            }
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private sealed class Context : ITypeDescriptorContext
    {
        public Context(object value, PropertyDescriptor property) { Instance = value; PropertyDescriptor = property; }
        public IContainer Container => null;
        public object Instance { get; }
        public PropertyDescriptor PropertyDescriptor { get; }
        public object GetService(Type t) => null;
        public bool OnComponentChanging() => true;
        public void OnComponentChanged() { }
    }
}
