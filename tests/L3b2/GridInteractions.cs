// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using CaeGlobals;
using CaeModel;
using PrePoMax;
using PrePoMax.Avalonia;

internal static class GridInteractions
{
    public static string Output = "";
    [STAThread] public static int Main(string[] args)
    {
        Output = args[0]; CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        AppBuilder.Configure<GridTestApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
        return Environment.ExitCode;
    }
}
public sealed class GridTestApp : Application
{
    private readonly List<string> checks = [];
    public override void Initialize() => Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        var life = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var grid = new DescriptorPropertyGrid();
        life.MainWindow = new Window { Width = 850, Height = 900, Content = grid };
        life.MainWindow.Opened += async (_, _) =>
        {
            try
            {
                void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks.Add(name); }
                async Task Text(string property, string value)
                {
                    var editor = (TextBox)grid.Editors[property]; editor.Text = value;
                    editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                }
                new UnitSystem(UnitSystemType.MM_TON_S_C).SetConverterUnits();
                var elastic = new ViewElastic(new Elastic([[1000, .3, 0]])); grid.SelectedObject = elastic;
                await Text("YoungsModulus", "2 GPa"); Check(Math.Abs(elastic.GetYoungsModulusValue() - 2000) < .001, "shared elastic: 2 GPa -> 2000 MPa");
                await Text("YoungsModulus", "=1000*3"); Check(elastic.GetYoungsModulusValue() == 3000, "shared elastic: equation");
                MyNCalc.ExistingParameters = new CaeGlobals.OrderedDictionary<string, object>("Parameters") { ["modulus"] = 4000d };
                await Text("YoungsModulus", "=modulus*2"); Check(elastic.GetYoungsModulusValue() == 8000, "shared elastic: parameter equation");
                await Text("YoungsModulus", "=unknown_parameter");
                Check(grid.Errors.ContainsKey("YoungsModulus") && elastic.GetYoungsModulusValue() == 8000, "invalid equation retains previous value and inline error");
                await Text("YoungsModulus", "9000"); Check(grid.Errors.Count == 0 && elastic.GetYoungsModulusValue() == 9000, "valid correction clears error");

                var field = new ViewElementFieldOutput(new ElementFieldOutput("Output", ElementFieldVariable.S)); grid.SelectedObject = field;
                var flags = (StackPanel)((Expander)grid.Editors["Variables"]).Content!;
                flags.Children.OfType<CheckBox>().Single(c => c.Content?.ToString() == "SDV").IsChecked = true;
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                Check(field.Variables.HasFlag(ViewElementFieldVariable.SDV) && field.Variables.HasFlag(ViewElementFieldVariable.S), "shared output flags add SDV without losing S");
                flags = (StackPanel)((Expander)grid.Editors["Variables"]).Content!;
                flags.Children.OfType<CheckBox>().Single(c => c.Content?.ToString() == "S").IsChecked = false;
                Check(!field.Variables.HasFlag(ViewElementFieldVariable.S), "shared output flags remove S");
                var color = new PrePoMax.Forms.ViewColor(System.Drawing.Color.Red); grid.SelectedObject = color;
                var colorEditor = (StackPanel)grid.Editors["Color"];
                var palette = colorEditor.Children.OfType<ComboBox>().Single();
                Check(!palette.Items.Cast<object>().Any(x => x.ToString() == "RebeccaPurple"), "Windows net48 color palette excludes .NET 10 addition");
                palette.SelectedItem = "Blue";
                Check(color.GetColor().ToArgb() == System.Drawing.Color.Blue.ToArgb(), "shared color dropdown changes model");
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                var colorText = ((StackPanel)grid.Editors["Color"]).Children.OfType<TextBox>().Single(); colorText.Text = "12, 34, 56";
                colorText.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                Check(color.GetColor().R == 12 && color.GetColor().G == 34 && color.GetColor().B == 56, "shared color free RGB text");

                var section = new ViewSolidSection(new SolidSection("Section", "Material", "Part", RegionTypeEnum.PartName, 1, false));
                section.PopulateDropDownLists(["Material"], ["Part"], [], ["Set"], [], []);
                grid.PropertyChanged += _ => section.UpdateRegionVisibility(); grid.SelectedObject = section;
                Check(grid.Editors.ContainsKey("PartName") && !grid.Editors.ContainsKey("ElementSetName"), "shared section part visibility");
                ((ComboBox)grid.Editors["RegionType"]).SelectedItem = "Element set name";
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                Check(!grid.Editors.ContainsKey("PartName") && grid.Editors.ContainsKey("ElementSetName"), "shared section dynamic visibility after dropdown");

                // A descriptor fixture isolates generic editor branches not currently
                // exposed by the supported entity forms. It is not an entity acceptance.
                var fixture = new EditorFixture(); grid.SelectedObject = fixture;
                await Text("Child.Value", "7"); Check(fixture.Child.Value == 7, "descriptor fixture nested writable object");
                Check(((TextBox)grid.Editors["ReadOnly"]).IsReadOnly, "descriptor fixture read-only");
                Check(((TextBox)grid.Editors["Unsupported"]).IsReadOnly && ((TextBox)grid.Editors["Unsupported"]).Text!.StartsWith("unsupported"), "unsupported editor remains visible and read-only");
                var options = (StackPanel)((Expander)grid.Editors["Flags"]).Content!;
                options.Children.OfType<CheckBox>().Single(c => c.Content?.ToString() == "None").IsChecked = true;
                Check(fixture.Flags == ExampleFlags.None, "zero flag clears all bits");
                Check(DescriptorPropertyGrid.Display("\t\u200bVisible") == "Visible", "ordering prefixes stripped for display");
                File.WriteAllText(GridInteractions.Output, JsonSerializer.Serialize(new { passed = true, checks, real_controls = true, entity_acceptance = false }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception error) { File.WriteAllText(GridInteractions.Output, JsonSerializer.Serialize(new { passed = false, checks, error = error.ToString() })); Environment.ExitCode = 1; }
            finally { life.Shutdown(Environment.ExitCode); }
        };
        base.OnFrameworkInitializationCompleted();
    }
}
[Flags] public enum ExampleFlags { None = 0, One = 1, Two = 2 }
[TypeConverter(typeof(ExpandableObjectConverter))] public sealed class ChildValue { public int Value { get; set; } }
public sealed class EditorFixture
{
    public ChildValue Child { get; } = new();
    public string ReadOnly => "fixed";
    [Editor("No.Such.Editor", "System.Drawing.Design.UITypeEditor")] public string Unsupported { get; set; } = "retained";
    [Editor("UserControls.FlagsEnumEditorUIEditor", "System.Drawing.Design.UITypeEditor")] public ExampleFlags Flags { get; set; } = ExampleFlags.One | ExampleFlags.Two;
}
