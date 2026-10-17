using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace PrePoMax.Avalonia;
internal static class Program
{
    public static string[] Arguments = [];
    [STAThread] public static void Main(string[] args)
    {
        Arguments = args;
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
