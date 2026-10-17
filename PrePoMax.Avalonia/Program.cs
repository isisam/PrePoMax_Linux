using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using PrePoMax.Viewer;
using Vtkmax.Interop;
using System.Text.Json;

namespace PrePoMax.Avalonia;
internal static class Program
{
    public static string[] Arguments = [];
    [STAThread] public static void Main(string[] args)
    {
        Arguments = args;
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
                    var model=ViewerModel.Open(args[1]);model.LoadResults(args[2]);Directory.CreateDirectory(args[3]);model.Show(view);view.StandardView(1);
                    foreach(string field in model.Contours.Keys)
                    {model.ShowContour(view,field);view.WritePng(Path.Combine(args[3],field.StartsWith("S")?"S.png":"U.png"));}
                    model.ShowContour(view,"U (magnitude)",1);view.WritePng(Path.Combine(args[3],"U-deformed.png"));
                    File.WriteAllText(Path.Combine(args[3],"results.json"),JsonSerializer.Serialize(new{nodes=model.NodeIds.Length,elements=model.ElementIds.Length,
                        fields=model.Contours.ToDictionary(p=>p.Key,p=>new{min=p.Value.Min(),max=p.Value.Max()}),displacementCount=model.Displacement!.Length,renderer=view.GlInfo},new JsonSerializerOptions{WriteIndented=true}));
                    Console.WriteLine("PASS PMX core + FRD S/U/deformed render");
                }
            }
            catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
            return;
        }
        AppBuilder.Configure<App>().UsePlatformDetect().With(new X11PlatformOptions
        { UseDBusMenu = false }).LogToTrace().StartWithClassicDesktopLifetime(args);
    }
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
