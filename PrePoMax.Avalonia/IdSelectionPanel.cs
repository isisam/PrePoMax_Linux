// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using CaeGlobals;
using vtkControl;

namespace PrePoMax.Avalonia;

/// <summary>FrmSelectItemSet's ID path. All membership operations use Controller selection nodes.</summary>
public sealed class IdSelectionPanel : UserControl, IDisposable
{
    private readonly Controller controller;
    private readonly IIdSelectionSession session;
    private readonly Selection previous;
    private readonly vtkSelectBy previousBy;
    private readonly GeometrySelectModeEnum previousGeometryMode;
    private readonly double previousAngle;
    public ComboBox Mode { get; } = new() { MinWidth=165 };
    public ComboBox GeometryMode { get; } = new() { ItemsSource=new[]{"Location","ID"},MinWidth=130 };
    public TextBox Angle { get; } = new() { Text="30 deg",Width=115 };
    private static readonly Dictionary<string,vtkSelectBy> modes=new()
    {
        ["ID"]=vtkSelectBy.Id,["Node"]=vtkSelectBy.Node,["Element"]=vtkSelectBy.Element,["Part"]=vtkSelectBy.Part,
        ["Surface"]=vtkSelectBy.Surface,["Edge"]=vtkSelectBy.Edge,["Edge angle"]=vtkSelectBy.EdgeAngle,["Surface angle"]=vtkSelectBy.SurfaceAngle,
        ["Geometry"]=vtkSelectBy.Geometry,["Geometry part"]=vtkSelectBy.GeometryPart,
        ["Geometry edge angle"]=vtkSelectBy.GeometryEdgeAngle,["Geometry surface angle"]=vtkSelectBy.GeometrySurfaceAngle
    };
    public TextBox IdInput { get; } = new() { Watermark = "IDs, separated by spaces or commas (face ID = 10 × element ID + VTK face index)" };
    public TextBlock Status { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public Dictionary<string, Button> Buttons { get; } = [];
    public ItemSetData ItemSetData { get; } = new();
    public event Action? Changed;
    // L3b_2 has one integration hook: feed picks through the same Controller node path.
    public void AcceptMouseSelection(SelectionNode node) => Execute(() => controller.AddSelectionNode(node, true, true));
    public void SelectPointOrArea(double[] point,double[] direction,double[][] planes,bool completelyInside,vtkSelectOperation operation,string[] names)
        => Execute(()=>controller.SelectPointOrArea(point,direction,planes,completelyInside,operation,names));

    public IdSelectionPanel(Controller c, IIdSelectionSession s)
    {
        controller = c; session = s; previous = c.Selection; previousBy = c.SelectBy;
        previousGeometryMode=c.GeometrySelectMode;previousAngle=c.SelectAngle;
        c.Selection = s.CreationData?.DeepCopy(true, false) ?? new Selection(); c.SelectItem = s.SelectItem; c.SelectBy = vtkSelectBy.Id;
        if (c.Selection.Nodes.Count == 0 && s.Ids.Length > 0) c.AddSelectionNode(new SelectionNodeIds(vtkSelectOperation.None, false, s.Ids), false, false);
        bool geometryHistory=c.Selection.Nodes.Count>0&&c.Selection.IsGeometryBased();
        if(geometryHistory){c.SelectBy=vtkSelectBy.Geometry;c.GeometrySelectMode=c.Selection.IsGeometryIdBased(c.GeometrySelectMode==GeometrySelectModeEnum.SelectId)?GeometrySelectModeEnum.SelectId:GeometrySelectModeEnum.SelectLocation;}
        var root = new StackPanel { Spacing = 5, Margin = new Thickness(0, 8) };
        root.Children.Add(new TextBlock { Text = "Selection", FontWeight = FontWeight.Bold });
        Mode.ItemsSource=modes.Keys.ToArray();Mode.SelectedItem=geometryHistory?"Geometry":"ID";
        GeometryMode.SelectedIndex=c.GeometrySelectMode==GeometrySelectModeEnum.SelectId?1:0;
        var options=new WrapPanel();options.Children.Add(Mode);options.Children.Add(Angle);options.Children.Add(GeometryMode);root.Children.Add(options);
        root.Children.Add(new TextBlock {Text="Drag a box in the viewport. Shift: add; Ctrl: subtract; Shift+Ctrl: intersect.",TextWrapping=TextWrapping.Wrap});
        root.Children.Add(IdInput);
        var tools = new WrapPanel();
        Add("Add IDs", () => c.AddSelectionNode(new SelectionNodeIds(vtkSelectOperation.Add, false, Parse()), false, true));
        Add("Remove IDs", () => c.AddSelectionNode(new SelectionNodeIds(vtkSelectOperation.Subtract, false, Parse()), false, true));
        Add("Select all", () => c.AddSelectionNode(new SelectionNodeIds(vtkSelectOperation.None, true), false, true));
        Add("Invert", () => c.AddSelectionNode(new SelectionNodeInvert(), false, true));
        Add("Clear", c.ClearSelectionHistoryAndCallSelectionChanged);
        // Same selection-history operation as Controller.RemoveLastSelectionNode;
        // preview is rendered by the shell's backing-grid highlighter, since the
        // legacy temporary-actor callback is not a valid Avalonia connectivity path.
        Add("Undo last", () => c.Selection.RemoveLast(true));
        Add("OK selection", () => ItemSetData.ItemIds = c.GetSelectionIds());
        root.Children.Add(tools); root.Children.Add(Status); Content = root; UpdateStatus();
        Mode.SelectionChanged+=(_,_)=>Execute(()=>
        {
            var by=modes[(string)Mode.SelectedItem!];
            if(by.IsGeometryBased()!=c.SelectBy.IsGeometryBased())c.ClearSelectionHistoryAndCallSelectionChanged();
            c.SelectBy=by;SetAngle();UpdateControls();
        });
        GeometryMode.SelectionChanged+=(_,_)=>Execute(()=>
        {
            var mode=GeometryMode.SelectedIndex==1?GeometrySelectModeEnum.SelectId:GeometrySelectModeEnum.SelectLocation;
            if(mode!=c.GeometrySelectMode){c.GeometrySelectMode=mode;c.ClearSelectionHistoryAndCallSelectionChanged();}
        });
        Angle.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){Execute(SetAngle);e.Handled=true;}};
        Angle.LostFocus+=(_,_)=>Execute(SetAngle);
        UpdateControls();
        void Add(string name, Action action)
        { var button = new Button { Content = name, Margin = new Thickness(2) }; button.Click += (_, _) => Execute(action); Buttons[name] = button; tools.Children.Add(button); }
    }
    private void SetAngle()
    {
        if(controller.SelectBy is not(vtkSelectBy.EdgeAngle or vtkSelectBy.SurfaceAngle or vtkSelectBy.GeometryEdgeAngle or vtkSelectBy.GeometrySurfaceAngle))return;
        var converter=new StringAngleDegConverter();double angle=(double)converter.ConvertFromString(Angle.Text??"")!;
        if(!double.IsFinite(angle))throw new ArgumentException("The selection angle must be finite.");
        angle=Math.Clamp(angle,0,180);controller.SetSelectAngle(angle);Angle.Text=converter.ConvertToString(angle);
    }
    private void UpdateControls()
    {
        bool geometry=controller.SelectBy.IsGeometryBased(),ids=controller.SelectBy==vtkSelectBy.Id;
        GeometryMode.IsEnabled=geometry;Angle.IsEnabled=controller.SelectBy is vtkSelectBy.EdgeAngle or vtkSelectBy.SurfaceAngle or vtkSelectBy.GeometryEdgeAngle or vtkSelectBy.GeometrySurfaceAngle;
        IdInput.IsEnabled=ids;Buttons["Add IDs"].IsEnabled=Buttons["Remove IDs"].IsEnabled=ids;
        Buttons["Select all"].IsEnabled=Buttons["Invert"].IsEnabled=!geometry;
    }
    private int[] Parse() => (IdInput.Text ?? "").Split([' ', ',', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
    private void Execute(Action action)
    {
        var before = controller.Selection.DeepCopy(true, false);
        try
        {
            action(); var ids = controller.GetSelectionIds(); session.SelectionChanged(ids, controller.Selection);
            ItemSetData.ItemIds = ids; UpdateStatus(); Changed?.Invoke();
        }
        catch (Exception ex) { controller.Selection = before; Status.Text = DescriptorPropertyGrid.Message(ex); }
    }
    private void UpdateStatus() => Status.Text = $"{controller.GetSelectionIds().Length} items: " + string.Join(", ", controller.GetSelectionIds().Take(40));
    public void Dispose() { controller.Selection = previous; controller.SelectBy = previousBy; controller.SelectItem = previous.SelectItem; controller.GeometrySelectMode=previousGeometryMode;controller.SelectAngle=previousAngle;controller.Clear3DSelection(); }
}
