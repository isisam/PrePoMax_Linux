using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using PrePoMax.Viewer;
using Vtkmax.Interop;
using System.Text.Json;
using Avalonia.Logging;
using Avalonia.OpenGL;

namespace PrePoMax.Avalonia;
internal static class Program
{
    public static string[] Arguments = [];
    [STAThread] public static void Main(string[] args)
    {
        Arguments = args;
        WslRenderer.Configure();
        Console.Write(BuildProvenance.Header);
        if(args.Length==3 && args[0]=="--tree-check")
        {try{TreeContractChecks.Run(args[1],args[2]);}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
        if(args.Length>=2 && args[0]=="--contract-check")
        {
            try { AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();ViewportContractChecks.Run(args[1],args.Length>2?args[2]:null,args.Length>3?args[3]:null); }
            catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;
        }
        if (args.Length > 0 && args[0] is "--probe" or "--render")
        {
            try
            {
                using var view=new Viewport(backend:3);
                if(args[0]=="--probe")
                {
                    view.Load([1,2,3,4],[0,0,0,1,0,0,1,1,0,0,1,0],[99],[9],[0,4],[0,1,2,3]);
                    view.StandardView(1);view.Render();view.WritePng(args[1]);Console.WriteLine(view.GlInfo);Console.WriteLine("PASS EGL headless render");
                }
                else
                {
                    var model=ViewerModel.Open(args[1]);model.LoadResults(args[2]);Directory.CreateDirectory(args[3]);model.Show(view);view.StandardView(1);view.Display(true,false);
                    foreach(string field in model.Contours.Keys)
                    {model.ShowContour(view,field);view.WritePng(Path.Combine(args[3],field.StartsWith("S")?"S.png":"U.png"));}
                    model.ShowContour(view,"U (magnitude)",1);view.WritePng(Path.Combine(args[3],"U-deformed.png"));
                    File.WriteAllText(Path.Combine(args[3],"results.json"),JsonSerializer.Serialize(new{nodes=model.NodeIds.Length,elements=model.ElementIds.Length,
                        step=model.ResultsStep,increment=model.ResultsIncrement,time=model.ResultsTime,
                        fields=model.Contours.ToDictionary(p=>p.Key,p=>new{min=p.Value.Min(),max=p.Value.Max()}),displacementCount=model.Displacement!.Length,renderer=view.GlInfo},new JsonSerializerOptions{WriteIndented=true}));
                    Console.WriteLine("PASS PMX core + FRD S/U/deformed render");
                }
            }
            catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
            return;
        }
        Logger.Sink=new ConsoleLogSink();
        AppBuilder.Configure<App>().UsePlatformDetect().With(new X11PlatformOptions
        { UseDBusMenu = false, OverlayPopups=Environment.GetEnvironmentVariable("VTKMAX_OVERLAY_POPUPS") is { } popups ? popups=="1" : Environment.GetEnvironmentVariable("VTKMAX_EMBED")!="x11",GlxRendererBlacklist = [],
          GlProfiles = [new GlVersion(GlProfileType.OpenGL, 4, 0),new GlVersion(GlProfileType.OpenGL, 3, 2)],
          RenderingMode = [X11RenderingMode.Glx, X11RenderingMode.Software] }).StartWithClassicDesktopLifetime(args);
    }
}
internal sealed class ConsoleLogSink : ILogSink
{
    public bool IsEnabled(LogEventLevel level,string area) => level>=LogEventLevel.Warning;
    public void Log(LogEventLevel level,string area,object? source,string messageTemplate) => Console.Error.WriteLine($"{level} {area}: {messageTemplate}");
    public void Log(LogEventLevel level,string area,object? source,string messageTemplate,params object?[] propertyValues) => Console.Error.WriteLine($"{level} {area}: {messageTemplate} "+string.Join(" ",propertyValues.Select(p=>p?.ToString())));
}
public sealed class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
