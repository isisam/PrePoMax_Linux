using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PrePoMax.Viewer;
using Vtkmax.Interop;
using System.Text.Json;
using Avalonia.Input;

namespace PrePoMax.Avalonia;
public sealed class MainWindow : Window
{
    private readonly VtkMaxView viewport = new();
    private readonly TreeView tree = new() { Margin = new Thickness(8) };
    private readonly TextBlock status = new() { Text = "Open an INP model", Margin = new Thickness(12, 8) };
    private ViewerModel? model;
    private PickResult lastPick = new() { NodeId = -1, ElementId = -1, FaceId = -1 };
    public MainWindow()
    {
        Title = "PrePoMax • Linux viewer prototype"; Width = 1200; Height = 820; MinWidth = 800; MinHeight = 500;
        KeyDown += (_, e) => { if (e.Key == Key.Q && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { e.Handled = true; Close(); } };
        var root = new DockPanel();
        var menu = new Menu { ItemsSource = new[]
        {
            new MenuItem { Header = "_File", ItemsSource = new[] { Item("_Open INP…", async () => await ChooseFile()), Item("Save view PNG…", async () => await SavePng()), Item("E_xit", Close) } },
            new MenuItem { Header = "_View", ItemsSource = new[] { Item("Fit", () => Change(v => v.Fit())), Item("Isometric", () => Change(v => v.StandardView())), Item("Front", () => Change(v => v.StandardView(1))), Item("Top", () => Change(v => v.StandardView(3))), Item("Rotate 15°", () => Change(v => v.Rotate(15, 5))) } }
        }};
        DockPanel.SetDock(menu, Dock.Top); root.Children.Add(menu);
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 6) };
        AddButton(toolbar, "Open INP", async () => await ChooseFile()); AddButton(toolbar, "Fit", () => Change(v => v.Fit()));
        AddButton(toolbar, "Rotate", () => Change(v => v.Rotate(15, 5))); AddButton(toolbar, "+", () => Change(v => v.Zoom(1.2)));
        AddButton(toolbar, "−", () => Change(v => v.Zoom(1 / 1.2))); AddButton(toolbar, "Surface", () => Change(v => v.Display(true, false)));
        AddButton(toolbar, "Edges", () => Change(v => v.Display(true, true))); AddButton(toolbar, "Wire", () => Change(v => v.Display(false, true)));
        AddButton(toolbar, "Highlight first ply", Highlight); AddButton(toolbar, "Clear", () => Change(v => v.Highlight([])));
        DockPanel.SetDock(toolbar, Dock.Top); root.Children.Add(toolbar);
        var help = new TextBlock { Text = "Left drag: rotate   •   Middle drag: pan   •   Wheel: zoom   •   Click: element / face / node", Margin = new Thickness(12, 6) };
        DockPanel.SetDock(help, Dock.Bottom); root.Children.Add(help);
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("285,5,*") };
        var sidebar = new DockPanel();
        var heading = new TextBlock { Text = "MODEL", Margin = new Thickness(12), FontWeight = global::Avalonia.Media.FontWeight.Bold };
        DockPanel.SetDock(heading, Dock.Top); sidebar.Children.Add(heading); sidebar.Children.Add(tree);
        grid.Children.Add(sidebar); var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1); grid.Children.Add(splitter); Grid.SetColumn(viewport, 2); grid.Children.Add(viewport);
        root.Children.Add(grid); Content = root;
        viewport.Ready += () => { if (Program.Arguments.Length > 0 && !Program.Arguments[0].StartsWith("--")) Open(Program.Arguments[0]); };
        viewport.Picked += Picked;
    }
    private static MenuItem Item(string title, Action action) { var item = new MenuItem { Header = title }; item.Click += (_, _) => action(); return item; }
    private static void AddButton(StackPanel panel, string text, Action action) { var b = new Button { Content = text }; b.Click += (_, _) => action(); panel.Children.Add(b); }
    private void Change(Action<Viewport> action)
    {
        try { if (viewport.View is { } view) { action(view); view.Render(); } }
        catch (Exception e) { status.Text = e.Message; Console.Error.WriteLine(e); }
    }
    private void Open(string path)
    {
        try
        {
            var loaded = ViewerModel.Open(path); loaded.Show(viewport.View!); viewport.View!.Render(); model = loaded;
            tree.ItemsSource = loaded.TreeGroups().Select(g => new TreeViewItem { Header = $"{g.Name} ({g.Items.Count()})",
                IsExpanded = g.Name is "Materials" or "Composite layups" or "Ply / layer display",
                ItemsSource = g.Items.Select(n => new TreeViewItem { Header = n }).ToArray() }).ToArray();
            status.Text = $"{Path.GetFileName(path)}  |  {loaded.NodeIds.Length:N0} nodes • {loaded.ElementIds.Length:N0} elements  |  {loaded.PlySource}";
            Console.WriteLine($"OPEN path={path} nodes={loaded.NodeIds.Length} elements={loaded.ElementIds.Length} plies={loaded.Plies.Count}");
            if (Program.Arguments.Contains("--evidence")) Evidence();
        }
        catch (Exception e) { status.Text = "Open failed: " + e.Message; Console.Error.WriteLine(e); }
    }
    private async Task ChooseFile()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open CalculiX input", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("CalculiX input") { Patterns = ["*.inp"] }] });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) Open(path);
    }
    private async Task SavePng()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = "view.png", DefaultExtension = "png" });
        if (file?.TryGetLocalPath() is { } path) Change(v => v.WritePng(path));
    }
    private void Highlight() { if (model?.Plies.Count > 0) Change(v => v.Highlight(model.Plies[0].ElementIds)); }
    private void Picked(PickResult r)
    {
        lastPick = r;
        status.Text = r.Hit != 0 ? $"Element {r.ElementId}  |  VTK local face {r.FaceId}  |  Node {r.NodeId}  |  ({r.X:G5}, {r.Y:G5}, {r.Z:G5})" : "No element at pointer";
        Console.WriteLine($"PICK hit={r.Hit} element={r.ElementId} face={r.FaceId} node={r.NodeId}");
        if (r.Hit != 0) Change(v => v.Highlight([r.ElementId]));
    }
    // Optional deterministic setup for the external X11 interaction harness.
    private void Evidence()
    {
        int flag = Array.IndexOf(Program.Arguments, "--evidence");
        if (flag + 1 >= Program.Arguments.Length) return;
        var output = Program.Arguments[flag + 1]; Directory.CreateDirectory(output);
        DispatcherTimer.RunOnce(() =>
        {
            Change(v => { v.StandardView(3); v.Fit(); v.WritePng(Path.Combine(output, "embedded-initial.png")); });
            var mesh = model!.Model.Mesh;
            double z = mesh.Nodes.Values.Max(n => n.Z);
            var element = mesh.Elements.Values.OrderBy(e => e.Id).First(e => e.NodeIds.Count(id => Math.Abs(mesh.Nodes[id].Z - z) < 1e-8) >= 3);
            var nodes = element.NodeIds.Select(id => mesh.Nodes[id]).Where(n => Math.Abs(n.Z - z) < 1e-8).ToArray();
            double x = nodes.Average(n => n.X), y = nodes.Average(n => n.Y);
            var monitor = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            monitor.Tick += (_, _) =>
            {
                if (viewport.View is not { } view) { monitor.Stop(); return; }
                var screen = view.Project(x, y, z);
                File.WriteAllText(Path.Combine(output, "state.json"), JsonSerializer.Serialize(new
                {
                    expectedElement = element.Id, world = new[] { x, y, z }, screen, width = viewport.Bounds.Width,
                    height = viewport.Bounds.Height, lastPick, runtime = Environment.Version.ToString()
                }, new JsonSerializerOptions { IncludeFields = true }));
                if (File.Exists(Path.Combine(output, "capture")))
                {
                    File.Delete(Path.Combine(output, "capture")); view.WritePng(Path.Combine(output, "embedded-current.png"));
                }
            };
            monitor.Start();
            Console.WriteLine("READY_FOR_X11_INPUT");
            File.WriteAllText(Path.Combine(output, "ready"), "ready");
        }, TimeSpan.FromSeconds(2));
    }
}
