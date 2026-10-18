// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace PrePoMax.Avalonia;

/// <summary>A small PropertyGrid. Descriptors and their converters are the only property schema.</summary>
public sealed class DescriptorPropertyGrid : UserControl
{
    private readonly StackPanel rows = new() { Spacing = 4 };
    private readonly TextBlock description = new() { TextWrapping = TextWrapping.Wrap, MinHeight = 65, Margin = new Thickness(8) };
    private readonly Dictionary<string, Func<bool>> commits = [];
    private readonly Dictionary<string, string> errors = [];
    private object? selected;
    private bool rebuilding;
    public event Action<string>? PropertyChanged;
    public Func<object, PropertyDescriptor, Task<object?>>? RegionEditor { get; set; }
    public object? SelectedObject { get => selected; set { selected = value; errors.Clear(); Rebuild(); } }
    public IReadOnlyDictionary<string, string> Errors => errors;
    public IReadOnlyDictionary<string, Control> Editors => editors;
    private readonly Dictionary<string, Control> editors = [];

    public DescriptorPropertyGrid()
    {
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        layout.Children.Add(new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        var pane = new Border { Child = description, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0, 1, 0, 0) };
        Grid.SetRow(pane, 1); layout.Children.Add(pane); Content = layout;
    }

    public static PropertyDescriptor[] Properties(object value) => Ordered(TypeDescriptor.GetProperties(value).Cast<PropertyDescriptor>());
    private static PropertyDescriptor[] Ordered(IEnumerable<PropertyDescriptor> properties) => properties.Where(p => p.IsBrowsable)
        // Upstream uses both tabs and zero-width spaces as ordering prefixes.
        // Windows' grid puts the longer prefix first; ordinal sorting reverses it.
        .OrderByDescending(p => PrefixLength(p.Category)).ThenBy(p => Display(p.Category), StringComparer.CurrentCulture)
        .ThenByDescending(p => PrefixLength(p.DisplayName)).ThenBy(p => Display(p.DisplayName), StringComparer.CurrentCulture).ToArray();
    private static int PrefixLength(string value) => value.TakeWhile(c => c is '\t' or '\u200b').Count();
    public static string Display(string value) => value.Replace("\t", "").Replace("\u200b", "");
    public bool CommitPending()
    {
        foreach (var commit in commits.Values.ToArray()) if (!commit()) return false;
        return errors.Count == 0;
    }
    public void Rebuild()
    {
        rebuilding = true;
        try { rows.Children.Clear(); commits.Clear(); editors.Clear(); if (selected != null) Build(rows, selected, Properties(selected), "", 0); }
        finally { rebuilding = false; }
    }
    private void Build(StackPanel target, object owner, PropertyDescriptor[] properties, string prefix, int depth)
    {
        string? category = null;
        foreach (var p in properties)
        {
            if (category != p.Category) { category = p.Category; target.Children.Add(new TextBlock { Text = Display(category), FontWeight = FontWeight.Bold, Margin = new Thickness(5, 10, 5, 3) }); }
            string key = prefix + p.Name;
            var ctx = new DescriptorContext(owner, p);
            var error = new TextBlock { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, Text = errors.GetValueOrDefault(key) };
            var cell = new StackPanel { Spacing = 2 };
            var line = new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), Margin = new Thickness(6, 1) };
            line.Children.Add(new TextBlock { Text = Display(p.DisplayName), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
            object? value;
            try { value = p.GetValue(owner); }
            catch (Exception ex) { value = null; errors[key] = Message(ex); }
            string Format(object? v) { try { return p.Converter.ConvertToString(ctx, CultureInfo.CurrentCulture, v) ?? ""; } catch { return v?.ToString() ?? ""; } }
            bool Set(object? newValue)
            {
                object? previous = p.GetValue(owner);
                try
                {
                    p.SetValue(owner, newValue);
                    errors.Remove(key); error.Text = "";
                    PropertyChanged?.Invoke(p.Name);
                    Dispatcher.UIThread.Post(Rebuild, DispatcherPriority.Background);
                    return true;
                }
                catch (Exception ex)
                {
                    // Equation and value setters normally validate before mutation. Also restore
                    // the old descriptor value for setters that assign before throwing.
                    try { p.SetValue(owner, previous); } catch { /* retain the original validation message */ }
                    errors[key] = Message(ex); error.Text = errors[key]; return false;
                }
            }
            bool TextSet(string text)
            {
                try { return Set(p.Converter.ConvertFromString(ctx, CultureInfo.CurrentCulture, text)); }
                catch (Exception ex) { errors[key] = Message(ex); error.Text = errors[key]; return false; }
            }
            var kind = PropertyEditorMap.Resolve(p.Attributes[typeof(EditorAttribute)] as EditorAttribute);
            Control editor;
            if (value != null && depth < 8 && p.Converter.GetPropertiesSupported(ctx) && kind == PropertyEditorKind.Default)
            {
                var nested = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                Build(nested, value, Ordered(p.Converter.GetProperties(ctx, value, [BrowsableAttribute.Yes])!.Cast<PropertyDescriptor>()), key + ".", depth + 1);
                editor = new Expander { Header = Format(value), Content = nested, HorizontalAlignment = HorizontalAlignment.Stretch };
            }
            else if (kind == PropertyEditorKind.Region && !p.IsReadOnly && RegionEditor != null)
            {
                var button = new Button { Content = Format(value) + " …" };
                button.Click += async (_, _) => { var result = await RegionEditor(owner, p); if (result != null) Set(result); };
                editor = button;
            }
            else if (kind == PropertyEditorKind.Color && !p.IsReadOnly && p.Converter.CanConvertFrom(ctx, typeof(string)))
                editor = MakeText();
            else if (kind == PropertyEditorKind.Flags && p.PropertyType.IsEnum && !p.IsReadOnly)
            {
                var choices = new StackPanel();
                long bits = Convert.ToInt64(value);
                foreach (object option in p.Converter.GetStandardValues(ctx)?.Cast<object>() ?? Enum.GetValues(p.PropertyType).Cast<object>())
                {
                    long mask = Convert.ToInt64(option);
                    var check = new CheckBox { Content = Format(option), IsChecked = mask == 0 ? bits == 0 : (bits & mask) == mask };
                    check.IsCheckedChanged += (_, _) => { if (!rebuilding) { long current = Convert.ToInt64(p.GetValue(owner)); Set(Enum.ToObject(p.PropertyType, check.IsChecked == true ? current | mask : current & ~mask)); } };
                    choices.Children.Add(check);
                }
                editor = new Expander { Header = Format(value), Content = choices };
            }
            else if (kind != PropertyEditorKind.Default)
                editor = new TextBox { Text = "unsupported (" + kind + "): " + Format(value), IsReadOnly = true };
            else if (p.IsReadOnly)
                editor = new TextBox { Text = Format(value), IsReadOnly = true };
            else if (p.PropertyType == typeof(bool) && !p.Converter.GetStandardValuesExclusive(ctx))
            {
                var check = new CheckBox { IsChecked = (bool?)value };
                check.IsCheckedChanged += (_, _) => { if (!rebuilding) Set(check.IsChecked == true); };
                editor = check;
            }
            else if (p.Converter.GetStandardValuesSupported(ctx))
            {
                var values = p.Converter.GetStandardValues(ctx)?.Cast<object>().ToArray() ?? [];
                var dropdown = new ComboBox { ItemsSource = values.Select(Format).ToArray(), HorizontalAlignment = HorizontalAlignment.Stretch };
                dropdown.SelectedIndex = Array.FindIndex(values, v => Equals(v, value) || Format(v) == Format(value));
                if (p.Converter.GetStandardValuesExclusive(ctx))
                {
                    dropdown.SelectionChanged += (_, _) => { if (!rebuilding && dropdown.SelectedIndex >= 0) Set(values[dropdown.SelectedIndex]); };
                    editor = dropdown;
                }
                else
                {
                    var text = MakeText(); var panel = new StackPanel { Spacing = 2 };
                    dropdown.SelectionChanged += (_, _) => { if (dropdown.SelectedIndex >= 0) { text.Text = Format(values[dropdown.SelectedIndex]); TextSet(text.Text); } };
                    panel.Children.Add(text); panel.Children.Add(dropdown); editor = panel;
                }
            }
            else if (p.Converter.CanConvertFrom(ctx, typeof(string))) editor = MakeText();
            else editor = new TextBox { Text = "unsupported (" + p.PropertyType.Name + "): " + Format(value), IsReadOnly = true };

            TextBox MakeText()
            {
                string accepted = Format(value); bool restoring = false;
                var text = new TextBox { Text = accepted };
                bool Commit()
                {
                    if (text.Text == accepted) return !errors.ContainsKey(key);
                    if (!TextSet(text.Text ?? "")) { restoring = true; text.Text = Format(p.GetValue(owner)); accepted = text.Text ?? ""; restoring = false; return false; }
                    accepted = Format(p.GetValue(owner)); restoring = true; text.Text = accepted; restoring = false; return true;
                }
                commits[key] = Commit;
                text.LostFocus += (_, _) => { if (!rebuilding) Commit(); };
                text.KeyDown += (_, e) => { if (e.Key == Key.Enter) { if (text.Text == accepted) { errors.Remove(key); error.Text = ""; } Commit(); e.Handled = true; } };
                text.TextChanged += (_, _) => { if (!restoring && text.Text != accepted) { errors.Remove(key); error.Text = ""; } };
                return text;
            }
            editors[key] = editor;
            editor.GotFocus += (_, _) => description.Text = Display(p.DisplayName) + Environment.NewLine + p.Description;
            line.PointerEntered += (_, _) => description.Text = Display(p.DisplayName) + Environment.NewLine + p.Description;
            Grid.SetColumn(editor, 1); line.Children.Add(editor); cell.Children.Add(line); cell.Children.Add(error); target.Children.Add(cell);
        }
    }
    internal static string Message(Exception error) => error is TargetInvocationException { InnerException: { } inner } ? Message(inner) : error.Message;
}

public sealed class DescriptorContext(object instance, PropertyDescriptor property) : ITypeDescriptorContext
{
    public IContainer? Container => null;
    public object Instance => instance;
    public PropertyDescriptor PropertyDescriptor => property;
    public object? GetService(Type serviceType) => null;
    public bool OnComponentChanging() => true;
    public void OnComponentChanged() { }
}
