using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace PrePoMax.L10n
{
    public sealed class TextSample
    {
        public string Context, Source, Text, Kind;
        public int Width, RequiredWidth;
        public bool Translated { get { return Source != Text; } }
        public bool Overflow { get { return Translated && RequiredWidth > Width + 4 && Width > 0; } }
    }
    public static class UiTranslator
    {
        private sealed class Value { public string Source, Last; }
        private sealed class State
        {
            public State() { }
            public readonly Dictionary<string, Value> Values = new Dictionary<string, Value>();
            public bool Hooked;
            public string Language;
        }
        private static readonly ConditionalWeakTable<object, State> _states = new ConditionalWeakTable<object, State>();
        private static bool _installed;
        private static DateTime _lastScan;
        private static Timer _timer;
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            Translator.Initialize();
            Translator.SetLanguage(null);
            Application.Idle += (s, e) => {
                if ((DateTime.UtcNow - _lastScan).TotalMilliseconds < 150) return;
                _lastScan = DateTime.UtcNow;
                RefreshOpenForms();
            };
            Application.AddMessageFilter(new ShowFilter());
            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => { if (Translator.Enabled) RefreshOpenForms(); };
            _timer.Start();
            Translator.LanguageChanged += (s, e) => RefreshOpenForms();
        }
        private sealed class ShowFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                // Catch newly shown controls/dialogs as well as forms reused after Hide().
                if (m.Msg == 0x18 && m.WParam != IntPtr.Zero)
                {
                    Control control = Control.FromHandle(m.HWnd);
                    if (control != null) Translate(control.FindForm() ?? control);
                }
                return false;
            }
        }
        public static void RefreshOpenForms()
        {
            var forms = new List<Form>();
            foreach (Form form in Application.OpenForms) forms.Add(form);
            foreach (Form form in forms)
                if (!form.IsDisposed)
                {
                    if (form.InvokeRequired) form.BeginInvoke((Action)(() => Translate(form)));
                    else Translate(form);
                }
        }
        private static string Apply(object owner, string property, string current, Action<string> set, string context)
        {
            var state = _states.GetOrCreateValue(owner);
            Value value;
            if (!state.Values.TryGetValue(property, out value))
                state.Values[property] = value = new Value { Source = current, Last = current };
            if (value.Last != current) value.Source = current;
            string translated = Translator.Translate(value.Source, context);
            if (translated == value.Source) translated = Translator.Message(value.Source);
            value.Last = translated;
            if (current != translated) set(translated);
            return value.Source;
        }
        public static void Translate(Control root) { Walk(root, null); }
        public static void Translate(ToolStripItem item)
        {
            Form form = item.Owner?.FindForm();
            string key = (form == null ? "" : form.GetType().Name) + "." + item.Name;
            Item(item, key, null);
        }
        public static List<TextSample> Inspect(Control root)
        {
            var samples = new List<TextSample>(); Walk(root, samples); return samples;
        }
        private static void Sample(List<TextSample> samples, string context, string source, string text,
            string kind, int width, Font font)
        {
            if (samples == null || string.IsNullOrWhiteSpace(source)) return;
            samples.Add(new TextSample { Context = context, Source = source, Text = text, Kind = kind,
                Width = width, RequiredWidth = TextRenderer.MeasureText(text, font).Width });
        }
        private static void Walk(Control control, List<TextSample> samples)
        {
            if (control.IsDisposed) return;
            Form form = control as Form ?? control.FindForm();
            string context = (form == null ? control.GetType().Name : form.GetType().Name) + "." + control.Name;
            // Input, output, paths, keywords, units and model names are data, never captions.
            bool caption = control is Form || control is Label || control is ButtonBase ||
                control is GroupBox || control is TabPage ||
                (control is TextBoxBase box && box.ReadOnly && (box.Tag as string) == "L10n");
            if (caption)
            {
                string source = Apply(control, "Text", control.Text, v => control.Text = v, context);
                if (!(control is Form)) Sample(samples, context, source, control.Text, control.GetType().Name,
                    (control.AutoSize || control is TextBoxBase || (control is Label && control.Height > control.Font.Height * 2))
                        ? 0 : control.Width - control.Padding.Horizontal, control.Font);
                else Sample(samples, context, source, control.Text, "Form", 0, control.Font);
            }
            if (control is ToolStrip strip) Items(strip.Items, context, samples);
            if (control.ContextMenuStrip != null) Items(control.ContextMenuStrip.Items, context + ".ContextMenu", samples);
            if (control.ContextMenu != null) LegacyItems(control.ContextMenu.MenuItems, context, samples);
            if (control is ListView list)
            {
                HookList(list);
                foreach (ColumnHeader col in list.Columns)
                {
                    string key = context + "." + col.Name;
                    string source = Apply(col, "Text", col.Text, v => col.Text = v, key);
                    Sample(samples, key, source, col.Text, "ColumnHeader", list.HeaderStyle == ColumnHeaderStyle.None ? 0 : col.Width, list.Font);
                }
            }
            if (control is DataGridView grid)
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    string key = context + "." + col.Name;
                    string source = Apply(col, "HeaderText", col.HeaderText, v => col.HeaderText = v, key);
                    Apply(col, "ToolTipText", col.ToolTipText, v => col.ToolTipText = v, key + ".ToolTipText");
                    Sample(samples, key, source, col.HeaderText, "DataGridViewColumn", col.Width, grid.Font);
                }
            if (control is TreeView tree) HookTree(tree);
            if (control is PropertyGrid properties)
            {
                if (Translator.Enabled)
                    foreach (object item in properties.SelectedObjects) LocalizedProvider.Install(item);
                var state = _states.GetOrCreateValue(properties);
                if (state.Language != null && state.Language != Translator.Language) properties.Refresh();
                state.Language = Translator.Language;
            }
            if (control is Form captionForm)
            {
                if (captionForm.Menu != null) LegacyItems(captionForm.Menu.MenuItems, context, samples);
                var state = _states.GetOrCreateValue(captionForm);
                if (!state.Hooked)
                {
                    state.Hooked = true;
                    captionForm.VisibleChanged += (s, e) => { if (captionForm.Visible) Translate(captionForm); };
                }
            }
            ToolTips(control, control, context, samples);
            foreach (Control child in control.Controls) Walk(child, samples);
        }
        private static void Items(ToolStripItemCollection items, string context, List<TextSample> samples)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripControlHost) continue;
                string key = context.Split('.')[0] + "." + item.Name;
                Item(item, key, samples);
                if (item is ToolStripDropDownItem dropdown) Items(dropdown.DropDownItems, context, samples);
            }
        }
        private static void Item(ToolStripItem item, string key, List<TextSample> samples)
        {
            // An implicit tooltip derives from Text. Read it before changing Text, and never materialize
            // that derived value as an explicit tooltip (which would corrupt English restoration).
            string tip = item.ToolTipText;
            bool implicitTip = item.AutoToolTip && (tip == item.Text || tip == item.Text.Replace("&", ""));
            string source = Apply(item, "Text", item.Text, v => item.Text = v, key);
            string tipSource = implicitTip ? source.Replace("&", "") :
                Apply(item, "ToolTipText", tip, v => item.ToolTipText = v, key + ".ToolTipText");
            Sample(samples, key, source, item.Text, item.GetType().Name, item.AutoSize ? 0 : item.Width, item.Font);
            Sample(samples, key + ".ToolTipText", tipSource, item.ToolTipText, "ToolTipText", 0, item.Font);
        }
        private static void LegacyItems(Menu.MenuItemCollection items, string context, List<TextSample> samples)
        {
            foreach (MenuItem item in items)
            {
                string key = context + "." + item.Name;
                string source = Apply(item, "Text", item.Text, v => item.Text = v, key);
                Sample(samples, key, source, item.Text, "MenuItem", 0, SystemFonts.MenuFont);
                LegacyItems(item.MenuItems, key, samples);
            }
        }
        private static void ToolTips(Control owner, Control target, string context, List<TextSample> samples)
        {
            // Designer components are private fields, including inherited form components.
            for (Type type = owner.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (typeof(ToolTip).IsAssignableFrom(field.FieldType) && field.GetValue(owner) is ToolTip tip)
                    {
                        Apply(tip, "ToolTipTitle", tip.ToolTipTitle, v => tip.ToolTipTitle = v, context + "." + field.Name + ".Title");
                        ToolTipTree(tip, target, context.Split('.')[0], samples);
                    }
                    else if (typeof(ContextMenuStrip).IsAssignableFrom(field.FieldType) && field.GetValue(owner) is ContextMenuStrip menu)
                        Items(menu.Items, context.Split('.')[0] + "." + field.Name, samples);
                    else if (typeof(ContextMenu).IsAssignableFrom(field.FieldType) && field.GetValue(owner) is ContextMenu legacy)
                        LegacyItems(legacy.MenuItems, context.Split('.')[0] + "." + field.Name, samples);
                }
        }
        private static void ToolTipTree(ToolTip tip, Control control, string form, List<TextSample> samples)
        {
            string key = form + "." + control.Name + ".ToolTip";
            string source = Apply(tip, key, tip.GetToolTip(control), v => tip.SetToolTip(control, v), key);
            Sample(samples, key, source, tip.GetToolTip(control), "ToolTip", 0, control.Font);
            foreach (Control child in control.Controls) ToolTipTree(tip, child, form, samples);
        }
        private static void HookTree(TreeView tree)
        {
            var state = _states.GetOrCreateValue(tree);
            if (state.Hooked)
            {
                var mode = Translator.Enabled ? TreeViewDrawMode.OwnerDrawText : TreeViewDrawMode.Normal;
                if (tree.DrawMode != mode) { tree.DrawMode = mode; tree.Invalidate(); }
                return;
            }
            if (!state.Hooked && Translator.Enabled && tree.DrawMode == TreeViewDrawMode.Normal)
            {
                state.Hooked = true;
                tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
                tree.DrawNode += (s, e) => {
                    string display = TreeCaption(e.Node);
                    if (display == e.Node.Text) { e.DrawDefault = true; return; }
                    Color foreground = e.Node.ForeColor.IsEmpty ? tree.ForeColor : e.Node.ForeColor;
                    Color background = tree.BackColor;
                    if ((e.State & TreeNodeStates.Selected) != 0)
                    { foreground = SystemColors.HighlightText; background = SystemColors.Highlight; }
                    Rectangle bounds = e.Bounds;
                    bounds.Width = Math.Max(bounds.Width, TextRenderer.MeasureText(display, tree.Font).Width + 6);
                    TextRenderer.DrawText(e.Graphics, display, e.Node.NodeFont ?? tree.Font, bounds,
                        foreground, background, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                };
            }
        }
        public static string TreeCaption(TreeNode node)
        {
            // Category identity is still English: routing, selection, serialization and user names are untouched.
            if (node.Tag != null && node.Tag.GetType().Name != "OrderedDictionaryTreeCategoryTag" &&
                node.TreeView?.Name != "tvProperties") return node.Text;
            if (node.Name.StartsWith("OrderedDictionaryGroup:", StringComparison.Ordinal)) return node.Text;
            string text = node.Text;
            int count = text.LastIndexOf(" (", StringComparison.Ordinal);
            if (count > 0 && text.EndsWith(")", StringComparison.Ordinal))
                return Translator.Translate(text.Substring(0, count)) + text.Substring(count);
            return Translator.Translate(text);
        }
        private static void HookList(ListView list)
        {
            // These lists contain UI types, not model entity names. Preserve the original Text used by handlers.
            if (list.Name != "lvSettings" && list.Name != "lvAddedProperties" && list.Name != "lvTypes" &&
                list.Name != "lvActiveTransformations") return;
            var state = _states.GetOrCreateValue(list);
            if (state.Hooked)
            {
                if (list.OwnerDraw != Translator.Enabled) { list.OwnerDraw = Translator.Enabled; list.Invalidate(); }
                return;
            }
            if (!Translator.Enabled || list.OwnerDraw) return;
            state.Hooked = true; list.OwnerDraw = true;
            list.DrawColumnHeader += (s, e) => e.DrawDefault = true;
            list.DrawItem += (s, e) => {
                if (list.View == View.Details) return;
                e.DrawBackground();
                TextRenderer.DrawText(e.Graphics, Translator.Translate(e.Item.Text), list.Font, e.Bounds,
                    e.Item.Selected ? SystemColors.HighlightText : list.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.DrawFocusRectangle();
            };
            list.DrawSubItem += (s, e) => {
                e.DrawBackground();
                Rectangle bounds = e.Bounds;
                if (e.ColumnIndex == list.Columns.Count - 1)
                    bounds.Width = Math.Max(bounds.Width, list.ClientSize.Width - bounds.Left - 2);
                TextRenderer.DrawText(e.Graphics, Translator.Translate(e.SubItem.Text), list.Font, bounds,
                    e.Item.Selected ? SystemColors.HighlightText : list.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };
        }
    }

    internal sealed class LocalizedProvider : TypeDescriptionProvider
    {
        private static readonly ConditionalWeakTable<object, LocalizedProvider> Installed = new ConditionalWeakTable<object, LocalizedProvider>();
        private LocalizedProvider(TypeDescriptionProvider parent) : base(parent) { }
        public static void Install(object instance)
        {
            if (instance == null || Installed.TryGetValue(instance, out _)) return;
            // The application's dynamic descriptor has its own translation hook and must retain its identity.
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(instance))
                if (property.GetType().Namespace == "DynamicTypeDescriptor") return;
            var provider = new LocalizedProvider(TypeDescriptor.GetProvider(instance));
            Installed.Add(instance, provider); TypeDescriptor.AddProvider(provider, instance);
        }
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        { return new Descriptor(base.GetTypeDescriptor(objectType, instance)); }
        private sealed class Descriptor : CustomTypeDescriptor
        {
            public Descriptor(ICustomTypeDescriptor parent) : base(parent) { }
            public override PropertyDescriptorCollection GetProperties() { return Wrap(base.GetProperties()); }
            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) { return Wrap(base.GetProperties(attributes)); }
            private PropertyDescriptorCollection Wrap(PropertyDescriptorCollection properties)
            {
                var result = new List<PropertyDescriptor>();
                foreach (PropertyDescriptor property in properties) result.Add(new Property(property));
                return new PropertyDescriptorCollection(result.ToArray(), true);
            }
        }
        private sealed class Property : PropertyDescriptor
        {
            private readonly PropertyDescriptor _inner;
            public Property(PropertyDescriptor inner) : base(inner) { _inner = inner; }
            private string Context { get { return ComponentType.Name + "." + Name; } }
            public override string DisplayName { get { return Translator.Translate(_inner.DisplayName, Context); } }
            public override string Category { get { return Translator.Translate(_inner.Category, Context + ".Category"); } }
            public override string Description { get { return Translator.Translate(_inner.Description, Context + ".Description"); } }
            public override Type ComponentType { get { return _inner.ComponentType; } }
            public override Type PropertyType { get { return _inner.PropertyType; } }
            public override bool IsReadOnly { get { return _inner.IsReadOnly; } }
            public override TypeConverter Converter { get { return _inner.Converter; } }
            public override object GetEditor(Type editorBaseType) { return _inner.GetEditor(editorBaseType); }
            public override object GetValue(object component) { return _inner.GetValue(component); }
            public override void SetValue(object component, object value) { _inner.SetValue(component, value); }
            public override bool CanResetValue(object component) { return _inner.CanResetValue(component); }
            public override void ResetValue(object component) { _inner.ResetValue(component); }
            public override bool ShouldSerializeValue(object component) { return _inner.ShouldSerializeValue(component); }
        }
    }
}
