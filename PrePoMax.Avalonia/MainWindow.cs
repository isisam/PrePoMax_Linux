// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PrePoMax.Viewer;
using Vtkmax.Interop;
using System.Text.Json;
using Avalonia.Input;
using CaeGlobals;
using CaeModel;
using CaeResults;
using vtkControl;

namespace PrePoMax.Avalonia;
public sealed partial class MainWindow : Window, IMainView
{
    private readonly VtkMaxView viewport = new();
    private readonly TreeView tree = new() { Margin = new Thickness(8) };
    private readonly TextBlock status = new() { Text = "Open a PMX, INP or FRD file", Margin = new Thickness(12, 8) };
    private readonly TextBlock units = new() { Text = "Units: undefined", Margin = new Thickness(12,8) };
    private readonly TextBox messages = new() { IsReadOnly=true, AcceptsReturn=true, TextWrapping=global::Avalonia.Media.TextWrapping.Wrap, FontFamily=global::Avalonia.Media.FontFamily.Parse("monospace") };
    private readonly ProgressBar progressBar = new() { IsIndeterminate = true, IsVisible = false, Height = 4 };
    private readonly Menu menu=new();
    private readonly ShellMainView mainView;
    private ViewerModel? model;
    private bool loading,closing;
    private double deformationScale;
    private string? modelPath;
    private PickResult lastPick = new() { NodeId = -1, ElementId = -1, FaceId = -1 };
    public MainWindow()
    {
        Title = "PrePoMax RCM - Linux model tree"; Width = 1280; Height = 900; MinWidth = 1000; MinHeight = 600;
        mainView=new ShellMainView(this,viewport){Loading=true};
        SettingsContainer.IsSaveDisabled=true;
        _=new Controller(this);mainView.Loading=false;
        KeyDown += (_, e) => { if (e.Key == Key.Q && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { e.Handled = true; Close(); } };
        Closed += (_, _) => {closing=true;solverCancellation?.Cancel();};
        viewport.OperationFailed+=e=>Log("Viewport error: "+e.Message);
        var root = new DockPanel();
        BuildMenu();DockPanel.SetDock(menu, Dock.Top); root.Children.Add(menu);
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 6) };
        AddButton(toolbar, "Open", async () => await ChooseFile());AddButton(toolbar,"Save PMX",async()=>await SavePmx());
        AddButton(toolbar, "Fit", () => viewport.SetZoomToFit(false));AddButton(toolbar, "Front", () => viewport.SetFrontBackView(false,true));
        AddButton(toolbar, "Rotate", () => viewport.Rotate(15, 5,0)); AddButton(toolbar, "+", () => viewport.SetZoomFactor(1.2));
        AddButton(toolbar, "-", () => viewport.SetZoomFactor(1 / 1.2));AddButton(toolbar, "Edges", ToggleEdges);
        AddButton(toolbar, "Clear selection", () => viewport.ClearSelection());
        var pickMode=new ComboBox {ItemsSource=new[]{"Pick element","Pick node"},SelectedIndex=0,Width=140};
        pickMode.SelectionChanged+=(_,_)=>viewport.SelectItem=pickMode.SelectedIndex==1?vtkSelectItem.Node:vtkSelectItem.Element;toolbar.Children.Add(pickMode);
        DockPanel.SetDock(toolbar, Dock.Top); root.Children.Add(toolbar);
        var resultsBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 4) };
        AddButton(resultsBar, "S (Mises)", () => Contour("S (Mises)"));AddButton(resultsBar, "U (magnitude)", () => Contour("U (magnitude)"));
        AddButton(resultsBar,"Undeformed",()=>{deformationScale=0;if(model!=null)ViewportModelBridge.Deform(viewport,model,0);});
        var scaleBox = new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = 1, Width = 125, Increment = 1, FormatString="0.0" };
        resultsBar.Children.Add(new TextBlock { Text = "Deformation scale", VerticalAlignment = VerticalAlignment.Center }); resultsBar.Children.Add(scaleBox);
        AddButton(resultsBar,"Apply",()=>{deformationScale=(double)(scaleBox.Value??0);if(model!=null)ViewportModelBridge.Deform(viewport,model,deformationScale);});
        AddButton(resultsBar, "Animate front", () => viewport.SetFrontBackView(true,true));
        resultsBar.Children.Add(threads);AddButton(resultsBar,"Run analysis",async()=>await RunAnalysis());AddButton(resultsBar,"Cancel",()=>solverCancellation?.Cancel());
        DockPanel.SetDock(resultsBar,Dock.Top);root.Children.Add(resultsBar);
        DockPanel.SetDock(progressBar,Dock.Top);root.Children.Add(progressBar);
        var statusPanel=new DockPanel();DockPanel.SetDock(units,Dock.Right);statusPanel.Children.Add(units);statusPanel.Children.Add(status);
        DockPanel.SetDock(statusPanel,Dock.Bottom);root.Children.Add(statusPanel);
        var layout = new Grid { RowDefinitions=new RowDefinitions("*,5,170"),ColumnDefinitions = new ColumnDefinitions("330,5,*") };
        var sidebar = new DockPanel();sidebar.Children.Add(BuildModelTrees());layout.Children.Add(sidebar);
        var vertical=new GridSplitter{ResizeDirection=GridResizeDirection.Columns};Grid.SetColumn(vertical,1);layout.Children.Add(vertical);
        Grid.SetColumn(viewport,2);layout.Children.Add(viewport);
        var horizontal=new GridSplitter{ResizeDirection=GridResizeDirection.Rows};Grid.SetRow(horizontal,1);Grid.SetColumnSpan(horizontal,3);layout.Children.Add(horizontal);
        var logPanel=new DockPanel();var label=new TextBlock{Text="MESSAGES / SOLVER LOG",Margin=new Thickness(8,3)};
        DockPanel.SetDock(label,Dock.Top);logPanel.Children.Add(label);logPanel.Children.Add(messages);Grid.SetRow(logPanel,2);Grid.SetColumnSpan(logPanel,3);layout.Children.Add(logPanel);
        ToolTip.SetTip(viewport,"VTK viewport tooltip");viewport.ContextMenu=new ContextMenu{ItemsSource=new[]{Item("Fit",()=>viewport.SetZoomToFit(false)),Item("Front",()=>viewport.SetFrontBackView(false,true))}};
        root.Children.Add(layout);Content=root;
        viewport.Ready+=async()=>{if(Program.Arguments.Length>0&&!Program.Arguments[0].StartsWith("--"))await Open(Program.Arguments[0]);};
        viewport.Picked+=Picked;
        Log("Ready. Open a model to begin.");
    }
    private void BuildMenu()
    {
        MenuItem L(string title,Action action)=>Item(ShellLocale.Text(title),action);
        menu.ItemsSource=new[]{
            new MenuItem{Header=ShellLocale.Text("File"),ItemsSource=new[]{L("Open",async()=>await ChooseFile()),L("Save",async()=>await SavePmx()),Item("Export INP",async()=>await ExportInp()),L("Export",async()=>await SavePng()),L("Exit",Close)}},
            new MenuItem{Header=ShellLocale.Text("View"),ItemsSource=new[]{L("Front",()=>viewport.SetFrontBackView(false,true)),L("Back",()=>viewport.SetFrontBackView(false,false)),L("Top",()=>viewport.SetTopBottomView(false,true)),L("Bottom",()=>viewport.SetTopBottomView(false,false)),L("Left",()=>viewport.SetLeftRightView(false,true)),L("Right",()=>viewport.SetLeftRightView(false,false)),Item("Isometric",()=>viewport.SetIsometricView(false,true)),L("Zoom to fit",()=>viewport.SetZoomToFit(false)),Item("Show / hide edges",ToggleEdges)}},
            new MenuItem{Header=ShellLocale.Text("Language"),ItemsSource=new[]{Item("English",()=>{ShellLocale.Chinese=false;BuildMenu();}),Item("\u7e41\u9ad4\u4e2d\u6587",()=>{ShellLocale.Chinese=true;BuildMenu();})}}
        };
    }
    private static MenuItem Item(string title, Action action) { var item = new MenuItem { Header = title }; item.Click += (_, _) => action(); return item; }
    private static void AddButton(StackPanel panel,string text,Action action){var b=new Button{Content=text};b.Click+=(_,_)=>action();panel.Children.Add(b);}
    private void ToggleEdges()=>viewport.EdgesVisibility=viewport.EdgesVisibility==vtkEdgesVisibility.NoEdges?vtkEdgesVisibility.ElementEdges:vtkEdgesVisibility.NoEdges;
    private void Change(Action<Viewport> action)=>viewport.Execute(v=>{try{action(v);v.Render();}catch(Exception e){Log(e.ToString());}});
    internal void Log(string text)
    {
        if(closing)return;
        Console.WriteLine(text);messages.Text=(messages.Text+text+Environment.NewLine);
        if(messages.Text.Length>250000)messages.Text=messages.Text[^200000..];
        messages.CaretIndex=messages.Text.Length;
    }
    internal void SetStatus(string text)=>status.Text=text;
    internal void UpdateUnits(UnitSystem unitSystem)=>units.Text="Units: "+unitSystem.UnitSystemType;
    internal void RefreshTree()
    {
        RefreshModelTrees();
    }
    private async Task Open(string path)
    {
        if(loading)return;loading=true;mainView.Loading=true;progressBar.IsVisible=true;
        var previousController=mainView.Controller;
        try
        {
            IProgress<string> progress=new Progress<string>(s=>{status.Text=s;Log(s);});
            string extension=Path.GetExtension(path).ToLowerInvariant();
            if(extension==".frd"&&model!=null)
            {await model.LoadResultsAsync(path,progress);Contour("S (Mises)");}
            else
            {
                var loaded=await Task.Run(()=>{
                    var controller=new Controller(this);controller.RegenerationWorkDirectory=Path.GetDirectoryName(Path.GetFullPath(path));
                    Exception? error=null;var previous=CoreHost.FileReadError;CoreHost.FileReadError=e=>error=e;
                    try
                    {
                        if(extension==".pmx")controller.Open(Path.GetFullPath(path));
                        else if(extension==".inp")
                        {
                            controller.Model.Properties.ModelSpace=ModelSpaceEnum.ThreeD;
                            var errors=controller.Model.ImportModelFromInpFileForViewing(Path.GetFullPath(path),s=>progress.Report(s),out _);
                            if(errors?.Count>0)throw new InvalidDataException(string.Join(Environment.NewLine,errors));
                            controller.ModelChanged=true;
                        }
                        else if(extension==".frd")
                        {var result=FrdFileReader.Read(Path.GetFullPath(path))??throw new InvalidDataException("FRD read failed");controller.Model.Properties.ModelSpace=ModelSpaceEnum.ThreeD;controller.Model.SetMesh(result.Mesh);}
                        else throw new ArgumentException("Expected .pmx, .inp or .frd");
                        if(error!=null)throw new InvalidDataException("PMX read failed",error);
                        return new ViewerModel(controller.Model);
                    }
                    finally{CoreHost.FileReadError=previous;}
                });
                model=loaded;modelPath=path;ViewportModelBridge.Load(viewport,loaded);RefreshTree();
                Log($"OPEN path={path} nodes={loaded.NodeIds.Length} elements={loaded.ElementIds.Length} controllerCommands={mainView.Controller.GetActiveCommandCount()}");
                int flag=Array.IndexOf(Program.Arguments,"--frd");
                string frd=extension==".frd"?path:flag>=0&&flag+1<Program.Arguments.Length?Program.Arguments[flag+1]:Path.ChangeExtension(path,".frd");
                if(File.Exists(frd))try{await loaded.LoadResultsAsync(frd,progress);}catch(Exception e){Log("Mesh loaded; "+e.Message);}
            }
            RefreshTree();
            status.Text=$"{Path.GetFileName(path)} | {model!.NodeIds.Length:N0} nodes | {model.ElementIds.Length:N0} elements";
            if((Environment.GetEnvironmentVariable("PPM_L2C_EVIDENCE")=="1"||Environment.GetEnvironmentVariable("PPM_L2B_EVIDENCE")=="1")&&Program.Arguments.Contains("--evidence"))Evidence();
        }
        catch(Exception e){mainView.Controller=previousController;Log(e.ToString());status.Text="Open failed: "+e.Message;}
        finally{loading=false;mainView.Loading=false;progressBar.IsVisible=false;}
    }
    private async Task ChooseFile()
    {
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{Title="Open model / results",FileTypeFilter=[new FilePickerFileType("PrePoMax / CalculiX"){Patterns=["*.pmx","*.inp","*.frd"]}]});
        if(files.Count>0&&files[0].TryGetLocalPath() is {} path)await Open(path);
    }
    private async Task SavePmx()
    {
        if(model==null||loading)return;
        var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title="Save full PMX",SuggestedFileName=Path.GetFileNameWithoutExtension(modelPath)+".pmx",DefaultExtension="pmx"});
        if(file?.TryGetLocalPath() is {} path)await SavePmxTo(path);
    }
    private async Task SavePmxTo(string path)
    {
        if(model==null||loading)return;loading=true;mainView.Loading=true;progressBar.IsVisible=true;
        try{await Task.Run(()=>mainView.Controller.SaveToPmx(path));Log("SAVE PMX "+path);status.Text="Saved "+path;}
        catch(Exception e){Log(e.ToString());status.Text="Save failed: "+e.Message;}
        finally{loading=false;mainView.Loading=false;progressBar.IsVisible=false;}
    }
    private async Task ExportInp()
    {
        if (model == null || loading) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Export CalculiX input", SuggestedFileName = Path.GetFileNameWithoutExtension(modelPath) + ".inp", DefaultExtension = "inp" });
        if (file?.TryGetLocalPath() is { } path)
        {
            try { ExportInpTo(path); }
            catch (Exception error) { Log(error.ToString()); SetStatus("Export failed: " + error.Message); }
        }
    }
    private void ExportInpTo(string path)
    {
        mainView.Controller.ExportToCalculix(path); Log("EXPORT INP " + path); SetStatus("Exported " + path);
    }
    private void Contour(string field)
    {if(model?.Contours.ContainsKey(field)==true){ViewportModelBridge.Contour(viewport,model,field,deformationScale);status.Text=$"{field} | step {model.ResultsStep}, increment {model.ResultsIncrement} | range {model.Contours[field].Min():G6}..{model.Contours[field].Max():G6}";}else Log("No "+field+" at a common S/U increment.");}
    private async Task SavePng()
    {var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{SuggestedFileName="view.png",DefaultExtension="png"});if(file?.TryGetLocalPath() is {} path)viewport.RenderToPNG(path);}
    private void Picked(PickResult r)
    {lastPick=r;SelectPickedPart(r);status.Text=r.Hit!=0?$"Element {r.ElementId} | Node {r.NodeId} | Face {r.FaceId} | ({r.X:G5}, {r.Y:G5}, {r.Z:G5})":"No element at pointer";Log($"PICK hit={r.Hit} element={r.ElementId} face={r.FaceId} node={r.NodeId}");}
    // Optional deterministic setup for the external X11 interaction harness.
    private void Evidence()
    {
        if(Environment.GetEnvironmentVariable("PPM_L2C_EVIDENCE")!="1"&&Environment.GetEnvironmentVariable("PPM_L2B_EVIDENCE")!="1")return;
        int flag = Array.IndexOf(Program.Arguments, "--evidence");
        if (flag + 1 >= Program.Arguments.Length) return;
        var output = Program.Arguments[flag + 1]; Directory.CreateDirectory(output);
        DispatcherTimer.RunOnce(() =>
        {
            var mesh = model!.Model.Mesh;
            double z = mesh.Nodes.Values.Max(n => n.Z);
            var element = mesh.Elements.Values.OrderBy(e => e.Id).First(e => e.NodeIds.Count(id => Math.Abs(mesh.Nodes[id].Z - z) < 1e-8) >= 3);
            var nodes = element.NodeIds.Select(id => mesh.Nodes[id]).Where(n => Math.Abs(n.Z - z) < 1e-8).ToArray();
            double x = nodes.Average(n => n.X), y = nodes.Average(n => n.Y);
            Change(v => {v.StandardView(1);v.Fit();v.WritePng(Path.Combine(output,"embedded-initial.png"));
                // DCB layers are subpixel at whole-model fit. Resolve the selected face before exact-ID XTest assertions.
                var camera=v.Camera;double distance=Math.Sqrt(camera.Position.Zip(camera.Focal,(a,b)=>(a-b)*(a-b)).Sum());
                v.SetCamera([x+1,y,z+distance],[x+1,y,z],[0,1,0],2);
                Console.WriteLine("READY_FOR_X11_INPUT");File.WriteAllText(Path.Combine(output,"ready"),"ready");});
            var monitor = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            Closed+=(_,_)=>monitor.Stop();
            monitor.Tick += async (_, _) =>
            {
                if (viewport.View is not { } view) { monitor.Stop(); return; }
                var screen = view.Project(x, y, z);
                File.WriteAllText(Path.Combine(output, "state.tmp"), JsonSerializer.Serialize(new
                {
                    expectedElement = element.Id, world = new[] { x, y, z }, screen, width = viewport.Bounds.Width,
                    height = viewport.Bounds.Height, scale=RenderScaling, lastPick, runtime = Environment.Version.ToString(),
                    origin=viewport.Control.TranslatePoint(new Point(0,0),this), windowId=TryGetPlatformHandle()?.Handle.ToInt64()??0,
                    pumpCount=viewport.PumpCount,
                    focus=viewport.HasViewportFocus, status=status.Text, log=messages.Text, actors=viewport.GetVtkMaxActorData().Length,
                    nodes=model.NodeIds.Length,elements=model.ElementIds.Length,controllerNodes=mainView.Controller.Model.Mesh?.Nodes.Count??0,step=model.ResultsStep,increment=model.ResultsIncrement,time=model.ResultsTime,
                    fields=model.Contours.ToDictionary(p=>p.Key,p=>new{min=p.Value.Min(),max=p.Value.Max()}),
                    language=ShellLocale.Chinese?"zh-TW":"en", units=units.Text, tree=TreeEvidenceState(), form=FormsEvidenceState()
                }, new JsonSerializerOptions { IncludeFields = true }));
                File.Move(Path.Combine(output,"state.tmp"),Path.Combine(output,"state.json"),true);
                if (File.Exists(Path.Combine(output, "capture")))
                {
                    File.Delete(Path.Combine(output, "capture")); viewport.Execute(v => v.WritePng(Path.Combine(output, "embedded-current.png")));
                }
                string commandPath=Path.Combine(output,"command.json");
                if(File.Exists(commandPath))
                {
                    using var command=JsonDocument.Parse(File.ReadAllText(commandPath));File.Delete(commandPath);
                    var c=command.RootElement;string op=c.GetProperty("op").GetString()!;
                    if(op.StartsWith("tree-"))TreeEvidenceCommand(op,c);
                    if(op.StartsWith("form-"))
                    {
                        try { FormsEvidenceCommand(op,c); }
                        catch (Exception error) { Log(error.ToString()); File.WriteAllText(Path.Combine(output,"command-error.txt"),error.Message); }
                    }
                    if(op=="front")viewport.SetFrontBackView(false,true);
                    if(op=="contour")Contour(c.GetProperty("field").GetString()!);
                    if(op=="deform"){deformationScale=c.GetProperty("scale").GetDouble();ViewportModelBridge.Deform(viewport,model,deformationScale);}
                    if(op=="png")viewport.RenderToPNG(c.GetProperty("path").GetString()!);
                    if(op=="edges")viewport.EdgesVisibility=c.GetProperty("visible").GetBoolean()?vtkEdgesVisibility.ElementEdges:vtkEdgesVisibility.NoEdges;
                    if(op=="save"){await SavePmxTo(c.GetProperty("path").GetString()!);File.WriteAllText(Path.Combine(output,"save-done"),"done");}
                    if(op=="open")await Open(c.GetProperty("path").GetString()!);
                    if(op=="language"){ShellLocale.Chinese=c.GetProperty("chinese").GetBoolean();BuildMenu();}
                    if(op=="benchmark"){double fps=await viewport.BenchmarkFramesAsync();File.WriteAllText(Path.Combine(output,"fps.json"),JsonSerializer.Serialize(new{fps,frames=30,warmup=5,backend="Avalonia generic",renderer=viewport.View!.GlInfo}));}
                    if(op=="run"){if(c.TryGetProperty("threads",out var count))threads.Value=count.GetInt32();await RunAnalysis(c.GetProperty("directory").GetString());File.WriteAllText(Path.Combine(output,"analysis-done"),"done");}
                    if(op=="tooltip")ToolTip.SetIsOpen(viewport.Control,c.GetProperty("open").GetBoolean());
                    if(op=="menu") {if(c.GetProperty("open").GetBoolean())viewport.Control.ContextMenu!.Open(viewport.Control);else viewport.Control.ContextMenu!.Close();}
                    if(op=="close"){monitor.Stop();Close();}
                }
            };
            monitor.Start();

        }, TimeSpan.FromSeconds(2));
    }
}
