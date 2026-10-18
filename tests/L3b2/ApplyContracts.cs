// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if NETFRAMEWORK
using System.Windows.Forms;
#else
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using PrePoMax.Avalonia;
#endif

internal static class ApplyContracts
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly List<object> records = new List<object>();
    private static readonly Dictionary<string, object> modes = new Dictionary<string, object>();
    private static readonly Dictionary<string, int[]> selections = new Dictionary<string, int[]>();
    private static object controller;
#if NETFRAMEWORK
    private static object selectionForm;
#endif
    private static string output, fixture, step, meshName, sectionName;
    private static object[] row(double[] values) => new object[] { new double[][] { values } };
    private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object New(string name, params object[] values) => Activator.CreateInstance(T(name), values);
    private static object Get(object obj, string name) => obj.GetType().GetProperty(name, All).GetValue(obj);
    private static void Set(object obj, string name, object value) => obj.GetType().GetProperty(name, All).SetValue(obj, value);
    private static object Call(object obj, string name, params object[] values)
    {
        var method = obj.GetType().GetMethods(All).Where(m => m.Name == name && m.GetParameters().Length >= values.Length &&
            m.GetParameters().Skip(values.Length).All(p => p.IsOptional)).OrderBy(m => m.GetParameters().Length).First();
        return method.Invoke(obj, values.Concat(method.GetParameters().Skip(values.Length).Select(p => p.DefaultValue)).ToArray());
    }
    private static object EnumValue(string type, string name) => Enum.Parse(T(type), name);
    private static string Message(Exception error) => error is TargetInvocationException && error.InnerException != null ? Message(error.InnerException) : error.Message;
    private static object Field(object obj, string name)
    {
        for (Type t = obj.GetType(); t != null; t = t.BaseType) { var field = t.GetField(name, All | BindingFlags.DeclaredOnly); if (field != null) return field.GetValue(obj); }
        throw new MissingFieldException(name);
    }
    public static int Run(string runtime, string model, string path)
    {
        output = path; fixture = model;
        string core = Path.Combine(runtime, "PrePoMax.Core.dll");
        if (!File.Exists(core)) core = Path.Combine(runtime, "lib", "PrePoMax.Core.dll");
        Assembly.LoadFrom(core);
#if NETFRAMEWORK
        Assembly.LoadFrom(Path.Combine(runtime, "PrePoMax.exe"));
        Execute(); return 0;
#else
        AppBuilder.Configure<ApplyApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(new string[0]);
        return Environment.ExitCode;
#endif
    }
    private static void Save(bool passed, string error = null)
    {
        var result = new { passed, cases = records, modes, selections, error, scope = "complete form Apply rejection; negative cases must not publish commands; material table visibility; real ID controls and saved history" };
#if NETFRAMEWORK
        File.WriteAllText(output, new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(result));
#else
        File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
#endif
    }
    private static void Execute()
    {
        try
        {
            T("PrePoMax.SettingsContainer").GetProperty("IsSaveDisabled", BindingFlags.Static | BindingFlags.Public).SetValue(null, true);
            controller = New("PrePoMax.Controller", New("PrePoMax.NullMainView"));
            Set(controller, "RegenerationWorkDirectory", Path.GetDirectoryName(output)); Call(controller, "Open", fixture, null);
            var names = (string[])Call(controller, "GetStepNames"); step = names.FirstOrDefault();
            if (step == null) { step = "TakenStep"; Call(controller, "AddStepCommand", New("CaeModel.StaticStep", step), true); }
            meshName = ((string[])Call(controller, "GetAllMeshEntityNames")).First();
            var sections = (string[])Call(controller, "GetSectionNames");
            sectionName = sections.FirstOrDefault();
            if (sectionName == null)
            {
                sectionName = "TakenSection";
                var mat = ((string[])Call(controller, "GetMaterialNames")).First(); var part = ((string[])Call(controller, "GetModelPartNames")).First();
                Call(controller, "AddSectionCommand", New("CaeModel.SolidSection", sectionName, mat, part, EnumValue("CaeGlobals.RegionTypeEnum", "PartName"), 1d, false));
            }
#if NETFRAMEWORK
            selectionForm = New("PrePoMax.FrmSelectItemSet", controller);
#endif
            foreach (string property in new[] { "Density", "Elastic", "Thermal expansion" }) Test("Material", property, null, property);
            Test("Section", "duplicate", sectionName); Test("Section", "empty selection"); Test("Section", "incompatible region");
            foreach (string kind in new[] { "NodeSet", "ElementSet", "Surface" })
            { Test(kind, "empty selection"); Test(kind, "duplicate", meshName); Test(kind, "duplicate case", meshName == meshName.ToUpperInvariant() ? meshName.ToLowerInvariant() : meshName.ToUpperInvariant()); }
            foreach (string value in new[] { step, step.ToUpperInvariant(), step.ToLowerInvariant() }) Test("Step", "duplicate " + value, value);
            Test("BoundaryCondition", "empty selection"); Test("BoundaryCondition", "unconstrained displacement");
            var bcs = (string[])Call(controller, "GetBoundaryConditionNamesForStep", step);
            if (bcs.Length == 0)
            {
                Call(controller, "AddNodeSetCommand", New("CaeMesh.FeNodeSet", "ApplyTakenNodes", new int[] { 1 }));
                Call(controller, "AddBoundaryConditionCommand", step, New("CaeModel.FixedBC", "TakenBC", "ApplyTakenNodes", EnumValue("CaeGlobals.RegionTypeEnum", "NodeSetName"), false));
            }
            Test("BoundaryCondition", "duplicate", bcs.FirstOrDefault() ?? "TakenBC");
            foreach (string type in new[] { "Concentrated force", "Moment", "Pressure" }) Test("Load", "zero " + type, null, type);
            Test("Load", "empty selection");
            MaterialModes();
            SelectionPaths();
            Save(true);
        }
        catch (Exception error) { Save(false, error.ToString()); throw; }
    }
    private static void SelectionPaths()
    {
        foreach (string kind in new[] { "NodeSet", "ElementSet", "Surface" })
        {
            string savedName = null;
            for (int edit = 0; edit < 2; edit++)
            {
                string prefix = kind + (edit == 0 ? ":new:" : ":edit:");
                object form;
#if NETFRAMEWORK
                form = New("PrePoMax.Forms.Frm" + kind, controller);
                T("PrePoMax.ItemSetDataEditor").GetMethod("SetForms", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { selectionForm, false, form });
                Call(form, "PrepareForm", null, savedName);
                if (Field(selectionForm, "btnMoreLess") is Button more && more.Text == "More") more.PerformClick();
                ((RadioButton)Field(selectionForm, "rbId")).Checked = true;
                void Click(string button)
                {
                    // This upstream revision retains the OK handler without a
                    // designer button. Exercise its ItemSetData return contract.
                    if (button == "OK selection") { Call(selectionForm, "btnOK_Click", null, EventArgs.Empty); return; }
                    string name = button == "Add IDs" ? "btnAddId" : button == "Remove IDs" ? "btnRemoveId" : button == "Undo last" ? "btnUndoSelection" :
                        button == "Invert" ? "btnInvertSelection" : button == "Select all" ? "btnSelectAll" : "btnClearSelection";
                    ((Button)Field(selectionForm, name)).PerformClick(); Application.DoEvents();
                }
                void Input(string value) => Set(Field(selectionForm, "tbIds"), "Text", value);
#else
                var c = (PrePoMax.Controller)controller;
                object source = savedName == null ? null : Call(controller, "Get" + kind, savedName);
                var session = (EntityFormSession)New("PrePoMax.Avalonia." + kind + "FormSession", controller, source);
                var editor = new EntityEditorWindow(session, c); form = editor; editor.Show();
                void Click(string button) => editor.SelectionPanel.Buttons[button].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                void Input(string value) => editor.SelectionPanel.IdInput.Text = value;
#endif
                void Snapshot(string action)
                {
                    var ids = (int[])Call(controller, "GetSelectionIds"); selections[prefix + action] = ids.OrderBy(i => i).ToArray();
                    if (action == "clear" && ids.Length != 0) throw new Exception("Clear failed");
                }
                string first = kind == "Surface" ? "10" : "1", second = kind == "Surface" ? "11" : "2";
                if (edit == 0)
                {
                    Input(first + ", " + second); Click("Add IDs"); Snapshot("add");
                    Input(second); Click("Remove IDs"); Snapshot("remove"); Click("Undo last"); Snapshot("undo");
                    Click("Invert"); Snapshot("invert"); Click("Select all"); Snapshot("all"); Click("Clear"); Snapshot("clear");
                    Input(first); Click("Add IDs"); Snapshot("final");
                }
                else { Input(second); Click("Add IDs"); Snapshot("append"); }
                Click("OK selection");
#if NETFRAMEWORK
                var returned = (int[])Get(Get(selectionForm, "ItemSetData"), "ItemIds"); selections[prefix + "returned"] = returned.OrderBy(i => i).ToArray();
                // NullMainView does not forward a selection-changed callback to the
                // active form. Deliver that same callback before the real OnApply.
                Call(form, "SelectionChanged", returned); savedName = (string)Get(Get(form, kind), "Name");
                Call(form, "OnApply", false); ((Form)form).Dispose();
#else
                selections[prefix + "returned"] = editor.SelectionPanel.ItemSetData.ItemIds.OrderBy(i => i).ToArray();
                savedName = editor.Session.Draft.Name;
                // Selection edits mark the real editor dirty, including edit sessions.
                editor.Buttons["Apply"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (!string.IsNullOrEmpty(editor.Error.Text)) throw new Exception(editor.Error.Text);
                editor.Close();
#endif
                object saved = Call(controller, "Get" + kind, savedName);
                var labels = (int[])Get(saved, kind == "Surface" ? "FaceIds" : "Labels");
                selections[prefix + "saved"] = labels.OrderBy(i => i).ToArray();
                object history = Get(saved, "CreationData");
                if (history == null || ((IList)Get(history, "Nodes")).Count == 0) throw new Exception("Saved selection history missing");
                selections[prefix + "history-node-count"] = new[] { ((IList)Get(history, "Nodes")).Count };
            }
        }
    }
    private static void MaterialModes()
    {
        string[] types = { "Density", "Elastic", "Engineering Constants", "Lamina", "Elastic with density", "Plastic", "Thermal expansion", "Thermal conductivity", "Specific heat", "Slip wear", "User Material", "Fatigue CZM (FATCZM)", "Fatigue CZM pre-crack" };
        foreach (string type in types)
        {
#if NETFRAMEWORK
            var form = (Form)New("PrePoMax.Forms.FrmMaterial", controller);
            Call(form, "PrepareForm", null, null);
            var material = New("CaeModel.Material", "USERFATCZM_Mode");
            object property = type == "Density" ? New("CaeModel.Density", row(new double[] { 1, 0 })) :
                type == "Elastic" ? New("CaeModel.Elastic", row(new double[] { 1, .3, 0 })) :
                type == "Engineering Constants" ? New("CaeModel.ElasticEngineeringConstants", row(new double[] { 1, 1, 1, .3, .3, .3, 1, 1, 1, 0 })) :
                type == "Lamina" ? New("CaeModel.ElasticLamina", row(new double[] { 1, 1, .3, 1, 1, 1, 0 })) :
                type == "Elastic with density" ? New("CaeModel.ElasticWithDensity", 1d, .3d, 1d) :
                type == "Plastic" ? New("CaeModel.Plastic", row(new double[] { 1, 0, 0 })) :
                type == "Thermal expansion" ? New("CaeModel.ThermalExpansion", row(new double[] { 1, 0 })) :
                type == "Thermal conductivity" ? New("CaeModel.ThermalConductivity", row(new double[] { 1, 0 })) :
                type == "Specific heat" ? New("CaeModel.SpecificHeat", row(new double[] { 1, 0 })) :
                type == "Slip wear" ? New("CaeModel.SlipWear", 1d, 1d) :
                type == "User Material" ? New("CaeModel.UserMaterial", new double[] { 1 }, 0, false) :
                T("CaeModel.UserMaterial").GetMethod("CreateFatczm").Invoke(null, new object[] { type == "Fatigue CZM pre-crack" });
            Call(material, "AddProperty", property); Set(form, "Material", material); form.Show();
            ((ListView)Field(form, "lvAddedProperties")).Items[0].Selected = true; Application.DoEvents();
            var tabs = (TabControl)Field(form, "tcProperties"); var pages = (TabPage[])Field(form, "_pages");
            var table = (DataGridView)Field(form, "dgvData");
            bool normalProperties = tabs.TabPages.Contains(pages[0]), normalRows = tabs.TabPages.Contains(pages[1]);
            bool normalTemperature = normalRows && table.Columns.Contains("TemperatureEq") && table.Columns["TemperatureEq"].Visible;
            ((CheckBox)Field(form, "cbTemperatureDependent")).Checked = true; Application.DoEvents();
            modes[type] = new { normalProperties, normalRows, normalTemperature,
                temperatureProperties = tabs.TabPages.Contains(pages[0]), temperatureRows = tabs.TabPages.Contains(pages[1]),
                temperatureColumn = tabs.TabPages.Contains(pages[1]) && table.Columns.Contains("TemperatureEq") && table.Columns["TemperatureEq"].Visible };
            form.Dispose();
#else
            var session = new MaterialFormSession((PrePoMax.Controller)controller); var editor = new EntityEditorWindow(session); editor.Show();
            editor.MaterialType.SelectedItem = type; editor.Buttons["Add property"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bool normalProperties = editor.Properties.IsVisible, normalRows = editor.RowSelector.IsVisible;
            bool normalTemperature = normalRows && editor.DataProperties.Editors.ContainsKey("TemperatureEq");
            editor.MaterialTemperature.IsChecked = true;
            modes[type] = new { normalProperties, normalRows, normalTemperature,
                temperatureProperties = editor.Properties.IsVisible, temperatureRows = editor.RowSelector.IsVisible,
                temperatureColumn = editor.RowSelector.IsVisible && editor.DataProperties.Editors.ContainsKey("TemperatureEq") };
            editor.Close();
#endif
        }
    }
    private static void Test(string kind, string scenario, string name = null, string subtype = null)
    {
        object form, draft;
        string property = kind == "BoundaryCondition" ? "BoundaryCondition" : kind == "Load" ? "FELoad" : kind == "NodeSet" ? "NodeSet" : kind == "ElementSet" ? "ElementSet" : kind == "Surface" ? "Surface" : kind;
        bool displacement = scenario == "unconstrained displacement";
#if NETFRAMEWORK
        string formName = kind == "BoundaryCondition" ? "BC" : kind;
        form = New("PrePoMax.Forms.Frm" + formName, controller);
        T("PrePoMax.ItemSetDataEditor").GetMethod("SetForms", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { selectionForm, false, form });
        Call(form, "PrepareForm", kind == "Load" || kind == "BoundaryCondition" ? step : null, null);
        if (kind == "Material")
        {
            draft = New("CaeModel.Material", "ApplyMaterial");
            object p = subtype == "Density" ? New("CaeModel.Density", row(new double[] { 0, 0 })) : subtype == "Elastic" ? New("CaeModel.Elastic", row(new double[] { 0, .3, 0 })) : New("CaeModel.ThermalExpansion", row(new double[] { 0, 0 }));
            Call(draft, "AddProperty", p); Set(form, "Material", draft);
        }
        else
        {
            draft = kind == "Section" ? New("CaeModel.SolidSection", "ApplySection", ((string[])Call(controller, "GetMaterialNames")).First(), "", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"), 1d, false) :
                kind == "NodeSet" ? New("CaeMesh.FeNodeSet", "ApplyNodes", null) :
                kind == "ElementSet" ? New("CaeMesh.FeElementSet", "ApplyElements", null, false) :
                kind == "Surface" ? New("CaeMesh.FeSurface", "ApplySurface") :
                kind == "Step" ? New("CaeModel.StaticStep", "ApplyStep") :
                kind == "BoundaryCondition" ? displacement ? New("CaeModel.DisplacementRotation", "ApplyBC", "", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"), false, false, 0d) : New("CaeModel.FixedBC", "ApplyBC", "", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"), false) :
                subtype == "Pressure" ? New("CaeModel.DLoad", "ApplyLoad", "", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"), 0d, false, false, 0d, false) :
                subtype == "Moment" ? New("CaeModel.MomentLoad", "ApplyLoad", "", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"), 0d, 0d, 0d, false, false, 0d) :
                New("CaeModel.CLoad", "ApplyLoad", "", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"), 0d, 0d, 0d, false, false, 0d, false);
        }
#else
        var session = (EntityFormSession)(kind == "Material" ? New("PrePoMax.Avalonia.MaterialFormSession", controller, null) :
            kind == "Section" ? New("PrePoMax.Avalonia.SectionFormSession", controller, null) :
            kind == "NodeSet" ? New("PrePoMax.Avalonia.NodeSetFormSession", controller, null) :
            kind == "ElementSet" ? New("PrePoMax.Avalonia.ElementSetFormSession", controller, null) :
            kind == "Surface" ? New("PrePoMax.Avalonia.SurfaceFormSession", controller, null) :
            kind == "Step" ? New("PrePoMax.Avalonia.StepFormSession", controller, null) :
            kind == "BoundaryCondition" ? New("PrePoMax.Avalonia.BoundaryFormSession", controller, step, null, displacement) :
            New("PrePoMax.Avalonia.LoadFormSession", controller, step, null, subtype ?? "Concentrated force"));
        if (kind == "Material") Call(session, "AddProperty", subtype);
        draft = session.Draft; form = null;
#endif
        if (name != null) Set(draft, "Name", name);
        if (kind == "Section")
        {
            Set(draft, "RegionType", EnumValue("CaeGlobals.RegionTypeEnum", scenario == "incompatible region" ? "ElementSetName" : "Selection"));
            Set(draft, "CreationIds", null); Set(draft, "RegionName", "MissingRegion");
        }
        if (kind == "BoundaryCondition" || kind == "Load")
        {
            Set(draft, "RegionType", EnumValue("CaeGlobals.RegionTypeEnum", "Selection"));
            Set(draft, "CreationIds", scenario == "empty selection" ? null : new int[] { 1 });
        }
#if NETFRAMEWORK
        if (kind != "Material")
        {
            Set(form, property, draft);
            string field = kind == "BoundaryCondition" ? "_viewBc" : "_view" + kind;
            if (kind == "Load") field = "_viewLoad";
            var grid = (PropertyGrid)Field(form, "propertyGrid"); grid.SelectedObject = Field(form, field);
        }
#else
        form = new EntityEditorWindow(session);
        ((Window)form).Show();
#endif
        int before = (int)Call(controller, "GetActiveCommandCount"); string error = null;
#if NETFRAMEWORK
        try { Call(form, kind == "Material" ? "Add" : "OnApply", kind == "Material" ? new object[0] : new object[] { false }); }
        catch (Exception ex) { error = Message(ex); }
#else
        var editor = (EntityEditorWindow)form;
        editor.Buttons["Apply"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); error = editor.Error.Text;
#endif
        int after = (int)Call(controller, "GetActiveCommandCount");
        bool passed = !string.IsNullOrWhiteSpace(error) && before == after;
        records.Add(new { kind, scenario, inputName = name, error, passed, commands_unchanged = before == after });
#if NETFRAMEWORK
        ((Form)form).Dispose();
#else
        ((Window)form).Close();
#endif
        if (!passed) throw new Exception("Negative Apply was accepted: " + kind + " " + scenario + " " + error);
    }
#if !NETFRAMEWORK
    public sealed class ApplyApp : Application
    {
        public override void Initialize() => Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        public override void OnFrameworkInitializationCompleted()
        {
            var life = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime;
            life.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            life.MainWindow = new Window();
            life.MainWindow.Opened += async (_, _) => { await System.Threading.Tasks.Task.Yield(); try { Execute(); } catch (Exception e) { Console.Error.WriteLine(e); Environment.ExitCode = 1; } finally { life.Shutdown(Environment.ExitCode); } };
            base.OnFrameworkInitializationCompleted();
        }
    }
#endif
}
