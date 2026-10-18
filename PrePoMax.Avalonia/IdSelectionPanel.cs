// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
    public TextBox IdInput { get; } = new() { Watermark = "IDs, separated by spaces or commas (face ID = 10 × element ID + VTK face index)" };
    public TextBlock Status { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public Dictionary<string, Button> Buttons { get; } = [];
    public ItemSetData ItemSetData { get; } = new();
    public event Action? Changed;
    // L3b_2 has one integration hook: feed picks through the same Controller node path.
    public void AcceptMouseSelection(SelectionNode node) => Execute(() => controller.AddSelectionNode(node, true, true));

    public IdSelectionPanel(Controller c, IIdSelectionSession s)
    {
        controller = c; session = s; previous = c.Selection; previousBy = c.SelectBy;
        c.Selection = s.CreationData?.DeepCopy(true, false) ?? new Selection(); c.SelectItem = s.SelectItem; c.SelectBy = vtkSelectBy.Id;
        if (c.Selection.Nodes.Count == 0 && s.Ids.Length > 0) c.AddSelectionNode(new SelectionNodeIds(vtkSelectOperation.None, false, s.Ids), false, false);
        var root = new StackPanel { Spacing = 5, Margin = new Thickness(0, 8) };
        root.Children.Add(new TextBlock { Text = "ID selection", FontWeight = FontWeight.Bold }); root.Children.Add(IdInput);
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
        void Add(string name, Action action)
        { var button = new Button { Content = name, Margin = new Thickness(2) }; button.Click += (_, _) => Execute(action); Buttons[name] = button; tools.Children.Add(button); }
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
    public void Dispose() { controller.Selection = previous; controller.SelectBy = previousBy; controller.SelectItem = previous.SelectItem; controller.Clear3DSelection(); }
}
