using Avalonia.Controls;
using Vtkmax.Interop;
namespace PrePoMax.Avalonia;
internal interface IViewportHost
{
    Control Control { get; }
    global::Avalonia.Rect Bounds => Control.Bounds;
    Viewport? View { get; }
    event Action? Ready;
    event Action<PickResult>? Picked;
    void Execute(Action<Viewport> action);
}
