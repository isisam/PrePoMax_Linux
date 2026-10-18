// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CaeGlobals;
using CaeModel;
using PrePoMax;
using PrePoMax.PropertyViews;
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
                var controller = new Controller(new NullMainView()); controller.New();
                controller.SetNewModelPropertiesCommand(ModelSpaceEnum.ThreeD, UnitSystemType.MM_TON_S_C);
                foreach (string type in MaterialFormSession.AvailableProperties)
                {
                    var session = new MaterialFormSession(controller);
                    var editor = new EntityEditorWindow(session); editor.Show();
                    void Click(string button) => editor.Buttons[button].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    editor.MaterialType.SelectedItem = type; Click("Add property");
                    Check(session.Pages.Count == 1 && string.IsNullOrEmpty(editor.Error.Text), "material add: " + type);
                    var view = session.Pages[0].View;
                    bool user = view is ViewUserMaterial, plastic = view is ViewPlastic;
                    bool scalarOnly = view is ViewElasticWithDensity or ViewSlipWear;
                    Check(editor.RowSelector.IsVisible == (user || plastic), "material normal table mode: " + type);
                    editor.MaterialTemperature.IsChecked = true;
                    Check(session.TemperatureDependent && editor.RowSelector.IsVisible == !scalarOnly, "material temperature table mode: " + type);
                    if (view is ViewThermalExpansion)
                        Check(!editor.Properties.Editors.ContainsKey("ThermalExpansion") && editor.Properties.Editors.ContainsKey("ZeroTemperature"), "thermal expansion zero-temperature metadata retained");
                    if (!scalarOnly && !user)
                    {
                        Check(editor.DataProperties.Editors.ContainsKey("TemperatureEq"), "temperature column shown: " + type);
                        if (type == "Elastic") Check(DescriptorPropertyGrid.Properties(editor.DataProperties.SelectedObject!).Single(p => p.Name == "YoungsModulusEq").DisplayName.Contains("MPa"), "material table header uses model pressure unit");
                        int count = session.Pages[0].DataRows!.Count;
                        Click("Add data row"); Check(session.Pages[0].DataRows!.Count == count + 1, "add data row: " + type);
                        Click("Remove data row"); Check(session.Pages[0].DataRows!.Count == count, "remove data row: " + type);
                    }
                    editor.MaterialTemperature.IsChecked = false;
                    if (plastic) Check(!editor.DataProperties.Editors.ContainsKey("TemperatureEq"), "plastic normal mode hides temperature column");
                    if (user && ((ViewUserMaterial)view).Fatczm)
                    {
                        Check(session.Pages[0].DataRows!.Count == 26, "FATCZM fixed 26 rows: " + type);
                        Click("Remove data row"); Check(session.Pages[0].DataRows!.Count == 26 && !string.IsNullOrEmpty(editor.Error.Text), "FATCZM refuses row removal: " + type);
                    }
                    editor.Close();
                }
                controller.AddStepCommand(new StaticStep("ControlsStep"), false);
                var stepSession = new StepFormSession(controller, (StaticStep)controller.GetStep("ControlsStep"));
                var stepEditor = new EntityEditorWindow(stepSession); stepEditor.Show();
                var controlType = stepEditor.GetVisualDescendants().OfType<ComboBox>().Single(c => c.Items.Cast<object>().SequenceEqual(StepFormSession.ControlTypes));
                foreach (string type in StepFormSession.ControlTypes)
                {
                    controlType.SelectedItem = type; stepEditor.Buttons["Add step control"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                Check(stepSession.Pages.Count == 5, "all four typed step controls added through controls");
                stepEditor.PageSelector.SelectedIndex = 2;
                var timeControl = (TextBox)stepEditor.Properties.Editors["I0"]; timeControl.Text = "8";
                timeControl.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                int commands = controller.GetActiveCommandCount();
                stepEditor.Buttons["Apply"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await System.Threading.Tasks.Task.Delay(20);
                var question = life.Windows.Single(w => w.Title == "Confirm");
                Check(question.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == StepFormSession.ControlWarning), "step-control Windows warning shown");
                question.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == "Cancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await System.Threading.Tasks.Task.Delay(20);
                Check(controller.GetActiveCommandCount() == commands, "cancel step-control warning preserves model");
                stepEditor.Buttons["Apply"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await System.Threading.Tasks.Task.Delay(20);
                question = life.Windows.Single(w => w.Title == "Confirm");
                question.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == "OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await System.Threading.Tasks.Task.Delay(20);
                Check(controller.GetActiveCommandCount() == commands + 1 && controller.GetStep("ControlsStep").StepControls.Parameters.Count == 4, "accept warning publishes four typed controls with one replace command");
                Check(controller.GetStep("ControlsStep").StepControls.Parameters.OfType<TimeIncrementationStepControlParameter>().Single().I0 == 8, "edited step-control value persisted");
                stepEditor.PageSelector.SelectedIndex = 4; stepEditor.Buttons["Remove step control"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(stepSession.Pages.Count == 4, "remove step control through real control"); stepEditor.Close();
                // Regression gates requested by Lota: no-op commands, step apply rules and model properties.
                var unchanged = new EntityEditorWindow(new StepFormSession(controller, controller.GetStep("ControlsStep"))); unchanged.Show();
                commands = controller.GetActiveCommandCount(); unchanged.Buttons["OK"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(30); Check(controller.GetActiveCommandCount() == commands, "unchanged step OK adds no command");
                var unitController = new Controller(new NullMainView()); unitController.New();
                Check(ModelPropertiesWindow.NeedsSelection(unitController), "undefined model needs properties selection");
                var dialog = new ModelPropertiesWindow(unitController); dialog.Show();
                commands = unitController.GetActiveCommandCount();
                Check(ModelPropertiesWindow.NeedsSelection(unitController) && unitController.GetActiveCommandCount() == commands, "interactive properties opening assigns no defaults");
                dialog.ModelSpace.SelectedItem = ModelSpaceEnum.ThreeD; dialog.UnitSystem.SelectedItem = UnitSystemType.MM_TON_S_C;
                dialog.Accept.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(!ModelPropertiesWindow.NeedsSelection(unitController) && unitController.Model.UnitSystem.UnitSystemType == UnitSystemType.MM_TON_S_C, "interactive properties routed OK publishes selection");
                unitController.New(); ModelPropertiesWindow.SetNonInteractive(unitController);
                Check(unitController.Model.Properties.ModelSpace == ModelSpaceEnum.ThreeD && unitController.Model.UnitSystem.UnitSystemType == UnitSystemType.UNIT_LESS, "noninteractive undefined defaults exactly 3D and UNIT_LESS");
                unitController.SetNewModelPropertiesCommand(ModelSpaceEnum.PlaneStress, UnitSystemType.MM_TON_S_C);
                commands = unitController.GetActiveCommandCount(); ModelPropertiesWindow.SetNonInteractive(unitController);
                Check(unitController.GetActiveCommandCount() == commands && unitController.Model.Properties.ModelSpace == ModelSpaceEnum.PlaneStress, "noninteractive defined properties are preserved without command");
                var buckle = new BuckleStep("InvalidBuckle") { Accuracy = double.NaN };
                var bad = StepFormSession.Create(unitController, buckle); var badEditor = new EntityEditorWindow(bad); badEditor.Show();
                commands = unitController.GetActiveCommandCount(); badEditor.Buttons["Apply"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(badEditor.Error.Text == "The accuracy of the buckle step must be a positive finite number." && unitController.GetActiveCommandCount() == commands, "buckle create validates before command"); badEditor.Close();
                unitController.AddStepCommand(buckle, false);
                var badEdit = new EntityEditorWindow(new StepFormSession(unitController, unitController.GetStep(buckle.Name))); badEdit.Show();
                commands = unitController.GetActiveCommandCount(); badEdit.Buttons["OK"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(unitController.GetActiveCommandCount() == commands, "unchanged legacy buckle skips value validation");
                bad = new StepFormSession(unitController, unitController.GetStep(buckle.Name)); bad.MarkChanged();
                try { bad.Apply(); Check(false, "changed invalid buckle rejected"); } catch (CaeException e) { Check(e.Message.Contains("positive finite"), "changed step validates values"); }
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
