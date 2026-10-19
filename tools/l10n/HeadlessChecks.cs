using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.ComponentModel;
using PrePoMax.L10n;

namespace PrePoMax.L10n.Tests
{
public static class L10nChecks
{
    private sealed class DescriptorContext : ITypeDescriptorContext
    {
        public object Instance { get; set; }
        public PropertyDescriptor PropertyDescriptor { get; set; }
        public IContainer Container { get { return null; } }
        public object GetService(Type serviceType) { return null; }
        public bool OnComponentChanging() { return true; }
        public void OnComponentChanged() { }
    }
    public static void CheckChoice(object instance, string property, string expected)
    {
        var descriptor = TypeDescriptor.GetProperties(instance)[property];
        var context = new DescriptorContext { Instance = instance, PropertyDescriptor = descriptor };
        object original = descriptor.GetValue(instance);
        string displayed = descriptor.Converter.ConvertToString(context, System.Globalization.CultureInfo.CurrentCulture, original);
        Assert(displayed == expected, "localized choice display: " + property + " = " + displayed);
        object converted = descriptor.Converter.ConvertFromString(context, System.Globalization.CultureInfo.CurrentCulture, displayed);
        Assert(object.Equals(converted, original), "localized choice parses to original value: " + property);
    }
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception("L10N CHECK FAILED: " + message); }
    private static IEnumerable<Control> Controls(Control root)
    {
        yield return root;
        // ToolStrip hosts reorder their child Control collection during overflow layout. Their
        // logical Items collection below records text and order, including hosted combo values.
        if (root is ToolStrip) yield break;
        foreach (Control child in root.Controls)
            foreach (Control item in Controls(child)) yield return item;
    }
    private static IEnumerable<ToolStripItem> Items(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            yield return item;
            if (item is ToolStripDropDownItem drop)
                foreach (var child in Items(drop.DropDownItems)) yield return child;
        }
    }
    private static string Snapshot(Form form)
    {
        var text = new StringBuilder();
        foreach (Control control in Controls(form))
        {
            text.Append(control.Name).Append('\0').Append(control.Text).Append('\0');
            if (control is TreeView tree)
                foreach (TreeNode node in tree.Nodes) text.Append(node.Name).Append('\0').Append(node.Text).Append('\0');
            if (control is ToolStrip strip)
                foreach (var item in Items(strip.Items)) text.Append(item.Name).Append('\0').Append(item.Text).Append('\0').Append(item.ToolTipText).Append('\0');
            if (control.ContextMenuStrip != null)
                foreach (var item in Items(control.ContextMenuStrip.Items)) text.Append(item.Name).Append('\0').Append(item.Text).Append('\0').Append(item.ToolTipText).Append('\0');
            if (control is DataGridView grid)
                foreach (DataGridViewColumn col in grid.Columns) text.Append(col.HeaderText).Append('\0').Append(col.ToolTipText).Append('\0');
            if (control is ListView list)
                foreach (ColumnHeader col in list.Columns) text.Append(col.Text).Append('\0');
        }
        return text.ToString();
    }
    public static string Run(Form main, object controller, string output)
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"progress.txt"),"Starting form inventory\n");
        var forms = new Dictionary<Type, Form>();
        forms[main.GetType()] = main;
        foreach (FieldInfo field in main.GetType().GetFields(Fields))
            if (field.GetValue(main) is Form form && !form.IsDisposed) forms[form.GetType()] = form;
        var owned = new List<Form>();
        var failures = new List<string>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "PrePoMax" || a.GetName().Name == "UserControls").ToArray();
        foreach (Type type in assemblies.SelectMany(a => a.GetTypes()).Where(t => !t.IsAbstract && typeof(Form).IsAssignableFrom(t)).OrderBy(t => t.FullName))
        {
            File.AppendAllText(Path.Combine(output,"progress.txt"),"Construct " + type.FullName + "\n");
            if (forms.ContainsKey(type)) continue;
            Exception error = null;
            foreach (ConstructorInfo constructor in type.GetConstructors().OrderBy(c => c.GetParameters().Length))
            {
                try
                {
                    var args = constructor.GetParameters().Select(p =>
                    {
                        if (p.ParameterType.IsInstanceOfType(controller)) return controller;
                        if (p.ParameterType.IsInstanceOfType(main)) return (object)main;
                        if (p.ParameterType == typeof(string)) return "";
                        if (p.ParameterType.IsArray) return Array.CreateInstance(p.ParameterType.GetElementType(), 0);
                        if (p.ParameterType.IsValueType || p.ParameterType.GetConstructor(Type.EmptyTypes) != null)
                            return Activator.CreateInstance(p.ParameterType);
                        return null;
                    }).ToArray();
                    var form = (Form)constructor.Invoke(args); forms[type] = form; owned.Add(form); error = null; break;
                }
                catch (Exception ex) { error = ex.InnerException ?? ex; }
            }
            if (!forms.ContainsKey(type)) failures.Add(type.FullName + ": " + (error?.ToString() ?? "no public constructor"));
        }
        var records = new List<object>();
        var allSamples = new List<TextSample>();
        string catalogue = Translator.CataloguePath;
        try
        {
            Translator.SetLanguage("en");
            var englishSnapshots = new Dictionary<Type, string>();
            foreach (var pair in forms)
            {
                UiTranslator.Translate(pair.Value);
                englishSnapshots[pair.Key] = Snapshot(pair.Value);
                UiTranslator.Translate(pair.Value);
                Assert(Snapshot(pair.Value) == englishSnapshots[pair.Key], pair.Key.Name + " English no-op");
            }
            Translator.SetLanguage("zh-TW");
            foreach (var pair in forms.OrderBy(p => p.Key.FullName))
            {
                Form form = pair.Value;
                File.AppendAllText(Path.Combine(output,"progress.txt"),"Check " + pair.Key.FullName + "\n");
                var samples = UiTranslator.Inspect(form);
                string chinese = Snapshot(form);
                UiTranslator.Translate(form); Assert(Snapshot(form) == chinese, pair.Key.Name + " idempotence");
                allSamples.AddRange(samples);
                records.Add(new { form = pair.Key.FullName, total = samples.Count, translated = samples.Count(s => s.Translated),
                    overflow = samples.Count(s => s.Overflow), englishNoOp = true, restoredExactly = true, wasVisible = form.Visible });
            }
            Translator.SetLanguage("en");
            foreach (var pair in forms)
            {
                UiTranslator.Translate(pair.Value);
                if (Snapshot(pair.Value) != englishSnapshots[pair.Key])
                {
                    File.WriteAllText(Path.Combine(output,"english_expected_" + pair.Key.Name + ".txt"),englishSnapshots[pair.Key],Encoding.UTF8);
                    File.WriteAllText(Path.Combine(output,"english_actual_" + pair.Key.Name + ".txt"),Snapshot(pair.Value),Encoding.UTF8);
                }
                Assert(Snapshot(pair.Value) == englishSnapshots[pair.Key], pair.Key.Name + " exact English restoration");
            }
            Synthetic(output);
            var serializer = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
            File.WriteAllText(Path.Combine(output, "forms.json"), serializer.Serialize(records), Encoding.UTF8);
            File.WriteAllText(Path.Combine(output, "samples.json"), serializer.Serialize(allSamples), Encoding.UTF8);
            File.WriteAllText(Path.Combine(output, "construction_failures.json"), serializer.Serialize(failures), Encoding.UTF8);
            File.WriteAllText(Path.Combine(output, "overflow.json"), serializer.Serialize(allSamples.Where(s => s.Overflow).ToArray()), Encoding.UTF8);
            var missing = allSamples.Where(s => !s.Translated).GroupBy(s => s.Source).Select(g => new { source = g.Key, contexts = g.Select(s => s.Context).Distinct().ToArray() }).ToArray();
            File.WriteAllText(Path.Combine(output, "untranslated.json"), serializer.Serialize(missing), Encoding.UTF8);
            return "Forms " + forms.Count + "; construction failures " + failures.Count + "; translated " +
                allSamples.Count(s => s.Translated) + "/" + allSamples.Count + "; potential overflow " + allSamples.Count(s => s.Overflow);
        }
        finally
        {
            foreach (Form form in owned) form.Dispose();
            Translator.Initialize(catalogue); Translator.SetLanguage("en");
        }
    }
    private static void Synthetic(string output)
    {
        string path = Translator.CataloguePath;
        using (var form = new Form { Name = "Probe", Text = "Material" })
        using (var tips = new ToolTip())
        {
            var label = new Label { Name = "Label", Text = "Mesh" };
            var input = new TextBox { Text = "Mesh" };
            var keywords = new RichTextBox { Text = "*MATERIAL, NAME=Mesh\n1.25, mm, MPa" };
            var combo = new ComboBox { Text = "Mesh" }; combo.Items.Add("Material");
            var menu = new MenuStrip(); var item = new ToolStripMenuItem("Load") { ToolTipText = "Material" }; menu.Items.Add(item);
            var context = new ContextMenuStrip(); context.Items.Add("Contact");label.ContextMenuStrip = context;
            var tabs = new TabControl(); tabs.TabPages.Add(new TabPage("Results"));
            var grid = new DataGridView(); grid.Columns.Add("Material", "Material");grid.Rows.Add("Mesh");
            var probe = new DescriptorProbe(); var properties = new PropertyGrid { SelectedObject = probe };
            form.Controls.AddRange(new Control[] {label,input,keywords,combo,menu,tabs,grid,properties});
            string english = Snapshot(form);
            Translator.SetLanguage("zh-TW"); UiTranslator.Translate(form);
            string validation = "Elements crossing a ply boundary: 1 (IDs 7); outside the stack: 0 (IDs ); ambiguous/zero extent: 0 (IDs ).";
            Assert(Translator.Message(validation) == "跨越層邊界的元素：1（編號 7）；位於疊層外的元素：0（編號 ）；範圍不明確或為零的元素：0（編號 ）。",
                "dynamic message retains populated and empty arguments");
            Assert(label.Text == "網格" && item.Text == "載重" && item.ToolTipText == "材料", "caption/menu/tooltip");
            Assert(context.Items[0].Text == "接觸" && tabs.TabPages[0].Text == "結果" && grid.Columns[0].HeaderText == "材料", "context/tab/grid header");
            Assert(input.Text == "Mesh" && combo.Text == "Mesh" && (string)combo.Items[0] == "Material" && (string)grid.Rows[0].Cells[0].Value == "Mesh", "model/input values untouched");
            Assert(keywords.Text == "*MATERIAL, NAME=Mesh\n1.25, mm, MPa", "keyword/unit/number untouched");
            var descriptor=TypeDescriptor.GetProperties(probe)["Value"];
            Assert(descriptor.DisplayName=="材料" && descriptor.Category=="一般" && descriptor.Description=="材料名稱", "fallback property metadata");
            Assert((string)descriptor.GetValue(probe)=="Mesh", "property value unchanged");
            label.Text = "Part"; UiTranslator.Translate(form); Assert(label.Text == "零件", "dynamic caption");
            Translator.SetLanguage("en");UiTranslator.Translate(form);Assert(label.Text == "Part", "dynamic source restored");
            label.Text = "Mesh"; UiTranslator.Translate(form);Assert(Snapshot(form) == english,"synthetic English bytes");
            Translator.Initialize(Path.Combine(output,"absent.json")); Translator.SetLanguage("zh-TW");UiTranslator.Translate(form);
            Assert(Snapshot(form) == english && Translator.Translate("Unknown UI text") == "Unknown UI text", "missing catalogue/entry fallback");
            string bad=Path.Combine(output,"invalid.json");File.WriteAllText(bad,"{ broken JSON");Translator.Initialize(bad);
            Assert(!Translator.Enabled && Translator.CatalogueError != null,"malformed catalogue fallback");
            Translator.Initialize(path);Translator.SetLanguage("en");
            string contextFile=Path.Combine(output,"context.json");File.WriteAllText(contextFile,"{\"Mesh\":\"網格\",\"Probe.Label\":\"情境\"}");
            Translator.Initialize(contextFile);Translator.SetLanguage("zh-TW");Assert(Translator.Translate("Mesh","Probe.Label")=="情境","optional context key");
            Translator.Initialize(path);Translator.SetLanguage("en");
        }
    }
    public sealed class DescriptorProbe
    {
        [Category("General"), DisplayName("Material"), Description("Material name")]
        public string Value { get; set; } = "Mesh";
    }
}
}
