// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Avalonia;

public sealed class ModelPropertiesWindow : Window
{
    public ComboBox ModelSpace { get; } = new() { ItemsSource = Enum.GetValues<ModelSpaceEnum>().Where(v => v != ModelSpaceEnum.Undefined).ToArray() };
    public ComboBox UnitSystem { get; } = new() { ItemsSource = new[] { UnitSystemType.UNIT_LESS, UnitSystemType.M_KG_S_C, UnitSystemType.MM_TON_S_C, UnitSystemType.M_TON_S_C, UnitSystemType.IN_LB_S_F } };
    public Button Accept { get; } = new() { Content = "OK" };
    public TextBlock Error { get; } = new() { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
    private bool accepted;
    public ModelPropertiesWindow(Controller controller)
    {
        Title = "Model Properties"; Width = 520; Height = 360; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ModelSpace.SelectedItem = controller.Model.Properties.ModelSpace == ModelSpaceEnum.Undefined ? ModelSpaceEnum.ThreeD : controller.Model.Properties.ModelSpace;
        UnitSystem.SelectedItem = controller.Settings.General.UnitSystemType;
        var body = new StackPanel { Margin = new Thickness(18), Spacing = 12 };
        body.Children.Add(new TextBlock { Text = "Select the model space and unit system before continuing." });
        body.Children.Add(new TextBlock { Text = "Model space" }); body.Children.Add(ModelSpace);
        body.Children.Add(new TextBlock { Text = "Unit system" }); body.Children.Add(UnitSystem); body.Children.Add(Error); body.Children.Add(Accept); Content = body;
        Closing += (_, e) => e.Cancel = !accepted;
        Accept.Click += (_, _) => {
            try { Set(controller, (ModelSpaceEnum)ModelSpace.SelectedItem!, (UnitSystemType)UnitSystem.SelectedItem!); accepted = true; Close(); }
            catch (Exception error) { Error.Text = error.Message; }
        };
    }
    public static void Set(Controller controller, ModelSpaceEnum space, UnitSystemType units)
    {
        if (space == ModelSpaceEnum.Undefined || units == UnitSystemType.Undefined) throw new CaeException("Select model space and unit system.");
        if (space.IsTwoD() && ((controller.Model.Geometry is { Nodes.Count: > 0 } g && !g.BoundingBox.Is2D()) ||
            (controller.Model.Mesh is { Nodes.Count: > 0 } m && !m.BoundingBox.Is2D())))
            throw new CaeException("Use of the 2D model space is not possible. The geometry or the mesh do not contain 2D geometry in x-y plane.");
        controller.SetNewModelPropertiesCommand(space, units);
    }
    public static bool NeedsSelection(Controller c) => c.Model.Properties.ModelSpace == ModelSpaceEnum.Undefined || c.Model.UnitSystem.UnitSystemType == UnitSystemType.Undefined;
    public static void SetNonInteractive(Controller c)
    {
        if (!NeedsSelection(c)) return;
        Set(c, c.Model.Properties.ModelSpace == ModelSpaceEnum.Undefined ? ModelSpaceEnum.ThreeD : c.Model.Properties.ModelSpace,
            c.Model.UnitSystem.UnitSystemType == UnitSystemType.Undefined ? UnitSystemType.UNIT_LESS : c.Model.UnitSystem.UnitSystemType);
    }
}

public sealed partial class MainWindow
{
    private async Task SelectModelPropertiesIfNeeded(Controller controller)
    {
        if (!ModelPropertiesWindow.NeedsSelection(controller)) return;
        if (Program.Arguments.Contains("--non-interactive"))
        {
            ModelPropertiesWindow.SetNonInteractive(controller);
            Log("The model properties are not defined. Model space: " + controller.Model.Properties.ModelSpace + ", unit system: " + controller.Model.UnitSystem.UnitSystemType + ".");
        }
        else await new ModelPropertiesWindow(controller).ShowDialog(this);
    }
}
