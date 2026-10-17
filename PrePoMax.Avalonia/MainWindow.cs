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
    private readonly IViewportHost viewport = Environment.GetEnvironmentVariable("VTKMAX_EMBED") == "generic" ? new GenericVtkView() : new VtkMaxView();
    private readonly TreeView tree = new() { Margin = new Thickness(8) };
    private readonly TextBlock status = new() { Text = "Open an INP model", Margin = new Thickness(12, 8) };
    private ViewerModel? model;
    private readonly ProgressBar progressBar = new() { IsIndeterminate = true, IsVisible = false, Height = 4 };
    private bool loading;
    private double deformationScale;
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
        var resultsBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 4) };
        AddButton(resultsBar, "Open FRD", async () => await ChooseResults());
        AddButton(resultsBar, "S (Mises)", () => Contour("S (Mises)"));
        AddButton(resultsBar, "U (magnitude)", () => Contour("U (magnitude)"));
        AddButton(resultsBar, "Undeformed", () => { deformationScale = 0; Change(v => { v.Deformation(model?.Displacement, 0); v.Fit(); }); });
        var scaleBox = new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = 1, Width = 95, Increment = 1 };
        resultsBar.Children.Add(new TextBlock { Text = "Deformation scale", VerticalAlignment = VerticalAlignment.Center }); resultsBar.Children.Add(scaleBox);
        AddButton(resultsBar, "Apply scale", () => { deformationScale = (double)(scaleBox.Value ?? 0); Change(v => { v.Deformation(model?.Displacement, deformationScale); v.Fit(); }); });
        AddButton(resultsBar, "Animate front", () => Animate(1));
        DockPanel.SetDock(resultsBar, Dock.Top); root.Children.Add(resultsBar);
        DockPanel.SetDock(progressBar, Dock.Top); root.Children.Add(progressBar);
        var help = new TextBlock { Text = "Left drag: rotate   •   Middle drag: pan   •   Wheel: zoom   •   Click: element / face / node", Margin = new Thickness(12, 6) };
        DockPanel.SetDock(help, Dock.Bottom); root.Children.Add(help);
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("285,5,*") };
        var sidebar = new DockPanel();
        var heading = new TextBlock { Text = "MODEL", Margin = new Thickness(12), FontWeight = global::Avalonia.Media.FontWeight.Bold };
        DockPanel.SetDock(heading, Dock.Top); sidebar.Children.Add(heading); sidebar.Children.Add(tree);
        grid.Children.Add(sidebar); var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1); grid.Children.Add(splitter); Grid.SetColumn(viewport.Control, 2);
        ToolTip.SetTip(viewport.Control, "VTK viewport tooltip");
        viewport.Control.ContextMenu = new ContextMenu { ItemsSource = new[] { Item("Fit model", () => Change(v => v.Fit())), Item("Front view", () => Change(v => v.StandardView(1))) } };
        grid.Children.Add(viewport.Control);
        root.Children.Add(grid); Content = root;
        viewport.Ready += async () => { if (Program.Arguments.Length > 0 && !Program.Arguments[0].StartsWith("--")) await Open(Program.Arguments[0]); };
        viewport.Picked += Picked;
    }
    private static MenuItem Item(string title, Action action) { var item = new MenuItem { Header = title }; item.Click += (_, _) => action(); return item; }
    private static void AddButton(StackPanel panel, string text, Action action) { var b = new Button { Content = text }; b.Click += (_, _) => action(); panel.Children.Add(b); }
    private void Change(Action<Viewport> action)
    {
        try { viewport.Execute(view => { try { action(view); view.Render(); } catch (Exception e) { status.Text=e.Message;Console.Error.WriteLine(e); } }); }
        catch (Exception e) { status.Text = e.Message; Console.Error.WriteLine(e); }
    }
    private async Task Open(string path)
    {
        if (loading) return;
        loading = true; progressBar.IsVisible = true;
        try
        {
            var progress = new Progress<string>(s => status.Text = s);
            var loaded = await ViewerModel.OpenAsync(path, progress); model = loaded;
            Change(v => loaded.Show(v));
            tree.ItemsSource = loaded.TreeGroups().Select(g => new TreeViewItem { Header = $"{g.Name} ({g.Items.Count()})",
                IsExpanded = g.Name is "Materials" or "Composite layups" or "Ply / layer display",
                ItemsSource = g.Items.Select(n => new TreeViewItem { Header = n }).ToArray() }).ToArray();
            status.Text = $"{Path.GetFileName(path)}  |  {loaded.NodeIds.Length:N0} nodes • {loaded.ElementIds.Length:N0} elements  |  {loaded.PlySource}";
            Console.WriteLine($"OPEN path={path} nodes={loaded.NodeIds.Length} elements={loaded.ElementIds.Length} plies={loaded.Plies.Count}");
            int frdFlag=Array.IndexOf(Program.Arguments,"--frd");
            string frd=frdFlag>=0&&frdFlag+1<Program.Arguments.Length?Program.Arguments[frdFlag+1]:Path.ChangeExtension(path,".frd");
            if(File.Exists(frd)) await loaded.LoadResultsAsync(frd,progress);
            if (Environment.GetEnvironmentVariable("PPM_L2B_EVIDENCE") == "1" && Program.Arguments.Contains("--evidence")) Evidence();
        }
        catch (Exception e) { status.Text = "Open failed: " + e.Message; Console.Error.WriteLine(e); }
        finally { loading = false; progressBar.IsVisible = false; }
    }
    private async Task ChooseFile()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Open model", AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("PrePoMax / CalculiX") { Patterns = ["*.inp", "*.pmx"] }] });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) await Open(path);
    }
    private async Task ChooseResults()
    {
        if (model == null || loading) return;
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {Title="Open FRD results", FileTypeFilter=[new FilePickerFileType("CalculiX results") { Patterns=["*.frd"] }]});
        if(files.Count==0 || files[0].TryGetLocalPath() is not { } path) return;
        loading=true;progressBar.IsVisible=true;
        try { await model.LoadResultsAsync(path,new Progress<string>(s=>status.Text=s)); Contour("S (Mises)"); }
        catch(Exception e){status.Text=e.Message;Console.Error.WriteLine(e);}
        finally{loading=false;progressBar.IsVisible=false;}
    }
    private void Contour(string field) { if(model?.Contours.ContainsKey(field)==true)Change(v=>model.ShowContour(v,field,deformationScale)); }
    private void Animate(int standard)
    {
        Change(v=>v.AnimateStandardBegin(standard));var start=System.Diagnostics.Stopwatch.StartNew();
        var timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(16) };
        timer.Tick+=(_,_)=>{double t=Math.Min(1,start.Elapsed.TotalMilliseconds/500);Change(v=>v.AnimateStandardStep(t));if(t==1)timer.Stop();};timer.Start();
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
            Change(v => { v.StandardView(1); v.Fit(); v.WritePng(Path.Combine(output, "embedded-initial.png")); });
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
                    height = viewport.Bounds.Height, scale=RenderScaling, lastPick, runtime = Environment.Version.ToString(),
                    origin=viewport.Control.TranslatePoint(new Point(0,0),this),
                    pumpCount=viewport is VtkMaxView native ? native.PumpCount : 0,
                    focus=viewport.Control.IsFocused
                }, new JsonSerializerOptions { IncludeFields = true }));
                if (File.Exists(Path.Combine(output, "capture")))
                {
                    File.Delete(Path.Combine(output, "capture")); viewport.Execute(v => v.WritePng(Path.Combine(output, "embedded-current.png")));
                }
                string commandPath=Path.Combine(output,"command.json");
                if(File.Exists(commandPath))
                {
                    using var command=JsonDocument.Parse(File.ReadAllText(commandPath));File.Delete(commandPath);
                    var c=command.RootElement;string op=c.GetProperty("op").GetString()!;
                    if(op=="front")Change(v=>v.StandardView(1));
                    if(op=="contour")Contour(c.GetProperty("field").GetString()!);
                    if(op=="tooltip")ToolTip.SetIsOpen(viewport.Control,c.GetProperty("open").GetBoolean());
                    if(op=="menu") {if(c.GetProperty("open").GetBoolean())viewport.Control.ContextMenu!.Open(viewport.Control);else viewport.Control.ContextMenu!.Close();}
                    if(op=="close"){monitor.Stop();Close();}
                }
            };
            monitor.Start();
            Console.WriteLine("READY_FOR_X11_INPUT");
            File.WriteAllText(Path.Combine(output, "ready"), "ready");
        }, TimeSpan.FromSeconds(2));
    }
}
