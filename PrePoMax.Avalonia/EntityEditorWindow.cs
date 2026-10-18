// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PrePoMax.Avalonia;

/// <summary>Real controls shared by interactive use and routed-event acceptance tests.</summary>
public sealed class EntityEditorWindow : Window
{
    public EntityFormSession Session { get; }
    public DescriptorPropertyGrid Properties { get; } = new();
    public DescriptorPropertyGrid DataProperties { get; } = new();
    public ComboBox PageSelector { get; } = new() { MinWidth = 230 };
    public ComboBox RowSelector { get; } = new() { MinWidth = 230, HorizontalAlignment = HorizontalAlignment.Stretch };
    public TextBox MaterialName { get; } = new();
    public TextBox MaterialDescription { get; } = new() { AcceptsReturn = true, MinHeight = 45 };
    public CheckBox MaterialTemperature { get; } = new() { Content = "Temperature dependent" };
    public TextBlock Error { get; } = new() { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap };
    public Dictionary<string, Button> Buttons { get; } = [];
    public ComboBox MaterialType { get; } = new() { ItemsSource = MaterialFormSession.AvailableProperties, SelectedIndex = 1 };
    public event Action? Applied;
    public event Action? DraftChanged;
    public event Action? AddNewRequested;
    private bool preparing = true, dirty;
    private FormPage? page;
    private readonly Grid propertyArea = new() { RowDefinitions = new RowDefinitions("*,Auto,Auto,Auto,*") };
    private readonly StackPanel dataTools = new() { Orientation = Orientation.Horizontal };
    private bool selecting;
    private int acceptedPage = -1, acceptedRow = -1;
    public IdSelectionPanel? SelectionPanel { get; private set; }
    public NumericUpDown Jumps { get; } = new() { Value = 300, Minimum = 1, Maximum = 10000000, Increment = 1, Width = 130 };

    public EntityEditorWindow(EntityFormSession session, Controller? controller = null)
    {
        Session = session; Title = (session.OriginalName == null ? "New " : "Edit ") + session.Kind;
        Width = 900; Height = 900; MinWidth = 680; MinHeight = 550;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"), Margin = new Thickness(14) };
        var header = new StackPanel { Spacing = 6 };
        if (session is MaterialFormSession material)
        {
            MaterialName.Text = material.Draft.Name;
            MaterialDescription.Text = ((CaeModel.Material)material.Draft).Description;
            MaterialTemperature.IsChecked = material.TemperatureDependent;
            MaterialName.TextChanged += (_, _) => { if (!preparing) dirty = true; };
            MaterialDescription.TextChanged += (_, _) => { if (!preparing) dirty = true; };
            MaterialTemperature.IsCheckedChanged += (_, _) =>
            {
                if (preparing) return;
                if (!Flush()) { preparing = true; MaterialTemperature.IsChecked = material.TemperatureDependent; preparing = false; return; }
                material.TemperatureDependent = MaterialTemperature.IsChecked == true; dirty = true; ShowPage();
            };
            header.Children.Add(new TextBlock { Text = "Name" }); header.Children.Add(MaterialName);
            header.Children.Add(new TextBlock { Text = "Description" }); header.Children.Add(MaterialDescription); header.Children.Add(MaterialTemperature);
            var tools = new WrapPanel { Orientation = Orientation.Horizontal };
            tools.Children.Add(MaterialType);
            tools.Children.Add(Button("Add property", () => { if (!Flush()) return; material.AddProperty((string)MaterialType.SelectedItem!); dirty = true; ReloadPages(session.Pages.Count - 1); }));
            tools.Children.Add(Button("Remove property", () => { if (PageSelector.SelectedIndex < 0) return; material.RemoveProperty(PageSelector.SelectedIndex); dirty = true; ReloadPages(); }));
            tools.Children.Add(Button("Move up", () => Move(-1)));
            tools.Children.Add(Button("Move down", () => Move(1)));
            tools.Children.Add(Button("Prefix USERFATCZM", () => { material.Draft.Name = MaterialName.Text ?? ""; material.PrefixFatczmName(); MaterialName.Text = material.Draft.Name; dirty = true; }));
            header.Children.Add(tools);
            void Move(int delta)
            {
                int from = PageSelector.SelectedIndex, to = from + delta;
                if (from < 0 || to < 0 || to >= session.Pages.Count || !Flush()) return;
                material.MoveProperty(from, to); dirty = true; ReloadPages(to);
            }
        }
        root.Children.Add(header);
        if (session is StepFormSession step)
        {
            var preset = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            preset.Children.Add(new TextBlock { Text = "Jumps", VerticalAlignment = VerticalAlignment.Center }); preset.Children.Add(Jumps);
            preset.Children.Add(Button("FATCZM fatigue step", () => { if (!Flush()) return; step.FatiguePreset((int)Jumps.Value!); dirty = true; Properties.Rebuild(); }));
            header.Children.Add(preset);
            var controlType = new ComboBox { ItemsSource = StepFormSession.ControlTypes, SelectedIndex = 0 };
            var controlTools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 }; controlTools.Children.Add(controlType);
            controlTools.Children.Add(Button("Add step control", () => { if (!Flush()) return; step.AddControl((string)controlType.SelectedItem!); dirty = true; ReloadPages(step.Pages.Count - 1); }));
            controlTools.Children.Add(Button("Remove step control", () => { if (PageSelector.SelectedIndex <= 0) return; step.RemoveControl(PageSelector.SelectedIndex - 1); dirty = true; ReloadPages(); }));
            header.Children.Add(controlTools);
            header.Children.Add(new TextBlock { Text = "Step-1: static peak-load ramp. Step-2: Direct, 1 increment/jump, Spooles, SDV. Existing steps are unchanged.", TextWrapping = TextWrapping.Wrap });
        }
        if (session is IIdSelectionSession selection && controller != null)
        {
            SelectionPanel = new IdSelectionPanel(controller, selection) { IsVisible = selection.SelectionVisible };
            SelectionPanel.Changed += () => { dirty = true; Properties.Rebuild(); DraftChanged?.Invoke(); };
            header.Children.Add(SelectionPanel); Closed += (_, _) => SelectionPanel.Dispose();
        }
        var chooser = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 6) };
        chooser.Children.Add(new TextBlock { Text = "Type / property", VerticalAlignment = VerticalAlignment.Center }); chooser.Children.Add(PageSelector);
        Grid.SetRow(chooser, 1); root.Children.Add(chooser);
        propertyArea.Children.Add(Properties);
        var rowLabel = new TextBlock { Text = "Data points (model units; FATCZM units are metadata, convert dimensional constants manually)", TextWrapping = TextWrapping.Wrap };
        Grid.SetRow(rowLabel, 1); propertyArea.Children.Add(rowLabel);
        Grid.SetRow(RowSelector, 2); propertyArea.Children.Add(RowSelector);
        dataTools.Children.Add(Button("Add data row", () =>
        {
            if (!Flush() || page?.DataRows is not { } data) return;
            if (page.View is ViewUserMaterial { Fatczm: true }) throw new InvalidOperationException("The FATCZM preset requires exactly 26 constants.");
            data.Add(Activator.CreateInstance(data.GetType().GetGenericArguments()[0])); dirty = true; ShowPage(); RowSelector.SelectedIndex = data.Count - 1;
        }));
        dataTools.Children.Add(Button("Remove data row", () =>
        {
            if (page?.DataRows is not { } data || RowSelector.SelectedIndex < 0) return;
            if (page.View is ViewUserMaterial { Fatczm: true }) throw new InvalidOperationException("The FATCZM preset requires exactly 26 constants.");
            data.RemoveAt(RowSelector.SelectedIndex); dirty = true; ShowPage();
        }));
        Grid.SetRow(dataTools, 3); propertyArea.Children.Add(dataTools);
        Grid.SetRow(DataProperties, 4); propertyArea.Children.Add(DataProperties);
        Grid.SetRow(propertyArea, 2); root.Children.Add(propertyArea);
        Grid.SetRow(Error, 3); root.Children.Add(Error);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
        actions.Children.Add(Button("OK", async () => { if (await Submit()) Close(); }));
        actions.Children.Add(Button("Apply", async () => { await Submit(); }));
        if (session.OkAddNew) actions.Children.Add(Button("OK + add new", async () => { if (await Submit()) { AddNewRequested?.Invoke(); Close(); } }));
        actions.Children.Add(Button("Cancel", Close));
        Grid.SetRow(actions, 4); root.Children.Add(actions); Content = root;
        PageSelector.SelectionChanged += (_, _) =>
        {
            if (selecting) return;
            if (!Flush()) { selecting = true; PageSelector.SelectedIndex = acceptedPage; selecting = false; return; }
            acceptedPage = PageSelector.SelectedIndex; ShowPage();
        };
        RowSelector.SelectionChanged += (_, _) =>
        {
            if (selecting) return;
            if (!DataProperties.CommitPending()) { selecting = true; RowSelector.SelectedIndex = acceptedRow; selecting = false; return; }
            acceptedRow = RowSelector.SelectedIndex;
            DataProperties.SelectedObject = page?.DataRows is { } rows && acceptedRow >= 0 ? DataRow(rows[acceptedRow]!) : null;
        };
        Properties.PropertyChanged += Changed; DataProperties.PropertyChanged += Changed;
        void Changed(string name)
        {
            dirty = true; Session.Changed(name);
            if (Session is StepFormSession step && Properties.SelectedObject is ViewStepControlParameter) step.ControlsChanged = true;
            if (SelectionPanel != null && session is IIdSelectionSession s) SelectionPanel.IsVisible = s.SelectionVisible;
            DraftChanged?.Invoke();
        }
        ReloadPages(); preparing = false;
    }
    private Button Button(string title, Action action)
    {
        var button = new Button { Content = title, Margin = new Thickness(2) };
        button.Click += (_, _) => { try { action(); } catch (Exception ex) { Error.Text = DescriptorPropertyGrid.Message(ex); } };
        Buttons[title] = button; return button;
    }
    public void ReloadPages(int index = 0)
    {
        selecting = true; PageSelector.ItemsSource = Session.Pages; PageSelector.SelectedIndex = Session.Pages.Count > 0 ? Math.Clamp(index, 0, Session.Pages.Count - 1) : -1;
        acceptedPage = PageSelector.SelectedIndex; selecting = false;
        ShowPage();
    }
    private void ShowPage()
    {
        page = PageSelector.SelectedItem as FormPage;
        bool showScalar = Session is not MaterialFormSession material || page == null || material.ShowScalar(page.View);
        Properties.SelectedObject = showScalar ? page?.View : null; Properties.IsVisible = showScalar;
        var data = page?.DataRows;
        string Label(object row, int index) => row is UserMaterialDataPoint um ? $"{index + 1}. {um.Name} — {um.Description}" : "Row " + (index + 1);
        selecting = true; RowSelector.ItemsSource = data?.Cast<object>().Select(Label).ToArray(); RowSelector.SelectedIndex = data?.Count > 0 ? 0 : -1;
        acceptedRow = RowSelector.SelectedIndex; selecting = false;
        DataProperties.SelectedObject = data?.Count > 0 ? DataRow(data[0]!) : null;
        bool showRows = data != null && (Session is not MaterialFormSession m || page != null && m.ShowRows(page.View));
        propertyArea.RowDefinitions[0].Height = !showScalar ? new GridLength(0) : page?.View is ViewUserMaterial ? new GridLength(210) : new GridLength(1, GridUnitType.Star);
        RowSelector.IsVisible = showRows; DataProperties.IsVisible = showRows; dataTools.IsVisible = showRows; propertyArea.Children[1].IsVisible = showRows;
        propertyArea.RowDefinitions[4].Height = showRows ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
    }
    private object DataRow(object row) => Session is MaterialFormSession material
        ? new MaterialRowDescriptor(row, material.TemperatureDependent, material.Units) : row;
    public bool Flush() => Properties.CommitPending() && DataProperties.CommitPending();
    private bool submitting;
    private async Task<bool> Submit()
    {
        if (submitting) return false;
        submitting = true;
        try
        {
            if (!Flush()) return false;
            if (Session is MaterialFormSession material && Session.Pages.Any(p => p.View is ViewUserMaterial { Fatczm: true }) && !(MaterialName.Text ?? "").StartsWith("USERFATCZM", StringComparison.Ordinal))
            {
                if (!await Confirm("FATCZM requires a USERFATCZM material name. Prefix this name with USERFATCZM?")) return false;
                MaterialName.Text = "USERFATCZM" + MaterialName.Text;
            }
            if (Session is LoadFormSession load && (dirty || Session.OriginalName == null))
            { load.Validate(); if (load.Confirmation() is { } question && !await Confirm(question)) return false; }
            if (Session is BoundaryFormSession boundary && (dirty || Session.OriginalName == null))
            { boundary.Validate(); if (boundary.Confirmation() is { } question && !await Confirm(question)) return false; }
            if (Session is StepFormSession { ControlsChanged: true, ControlWarningAccepted: false } step)
            { if (!await Confirm(StepFormSession.ControlWarning)) return false; step.ControlWarningAccepted = true; }
            return Apply();
        }
        catch (Exception ex) { Error.Text = DescriptorPropertyGrid.Message(ex); return false; }
        finally { submitting = false; }
    }
    private async Task<bool> Confirm(string message)
    {
        var dialog = new Window { Title = "Confirm", Width = 510, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var body = new StackPanel { Margin = new Thickness(14), Spacing = 12 };
        body.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        var yes = new Button { Content = "OK" }; yes.Click += (_, _) => dialog.Close(true);
        var no = new Button { Content = "Cancel" }; no.Click += (_, _) => dialog.Close(false);
        var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; tools.Children.Add(yes); tools.Children.Add(no); body.Children.Add(tools); dialog.Content = body;
        return await dialog.ShowDialog<bool>(this);
    }
    public bool Apply()
    {
        try
        {
            if (!Flush()) { Error.Text = "Correct the highlighted property before applying."; return false; }
            if (Session is MaterialFormSession material)
            { material.Draft.Name = MaterialName.Text ?? ""; ((CaeModel.Material)material.Draft).Description = MaterialDescription.Text ?? ""; }
            if (dirty || Session.OriginalName == null) { Session.Apply(); dirty = false; Applied?.Invoke(); }
            Error.Text = ""; return true;
        }
        catch (Exception ex) { Error.Text = DescriptorPropertyGrid.Message(ex); return false; }
    }
}
