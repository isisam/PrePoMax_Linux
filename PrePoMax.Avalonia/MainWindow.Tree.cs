// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CaeGlobals;
using CaeMesh;
using System.ComponentModel;
using Vtkmax.Interop;
using vtkControl;

namespace PrePoMax.Avalonia;

public sealed partial class MainWindow
{
    private readonly TreeView geometryTree = new() { Margin = new Thickness(6) };
    private readonly TreeView resultsTree = new() { Margin = new Thickness(6) };
    private readonly TabControl treeTabs = new() { SelectedIndex = 1 };
    private readonly Dictionary<string, TreeViewItem> treeItems = [];
    private bool changingTree;
    private ViewGeometryModelResults TreeViewMode => treeTabs.SelectedIndex == 0 ? ViewGeometryModelResults.Geometry :
        treeTabs.SelectedIndex == 2 ? ViewGeometryModelResults.Results : ViewGeometryModelResults.Model;

    private Control BuildModelTrees()
    {
        treeTabs.ItemsSource = new[] { new TabItem { Header="Geometry", Content=geometryTree,FontSize=14,Padding=new Thickness(8,6) },
            new TabItem { Header="Model", Content=tree,FontSize=14,Padding=new Thickness(8,6) }, new TabItem { Header="Results", Content=resultsTree,FontSize=14,Padding=new Thickness(8,6) } };
        foreach (var control in new[] { geometryTree, tree, resultsTree })
        {
            control.SelectionChanged += (_, _) => { if (!changingTree && control.SelectedItem is TreeViewItem { Tag: TreeEntry entry }) SelectTreeEntity(entry); };
            control.DoubleTapped += (_, e) => { if (control.SelectedItem is TreeViewItem { Tag: TreeEntry { Entity: not null } entry }) { OpenTreeEditor(entry); e.Handled=true; } };
        }
        treeTabs.SelectionChanged += (_, e) => { if (e.Source == treeTabs && !changingTree && model != null) SwitchTreeView(); };
        return treeTabs;
    }

    private void RefreshModelTrees()
    {
        if (mainView.Controller?.Model == null) return;
        var expanded = treeItems.Where(p => p.Value.IsExpanded).Select(p => p.Key).ToHashSet();
        string? selected = (new[] { tree, geometryTree, resultsTree }.Select(t => t.SelectedItem).OfType<TreeViewItem>().FirstOrDefault()?.Tag as TreeEntry)?.Key;
        changingTree=true;
        try
        {
            bool initial = treeItems.Count == 0; treeItems.Clear();
            TreeViewItem Build(TreeEntry entry, int depth)
            {
                var label = new TextBlock { Text=entry.Caption, VerticalAlignment=VerticalAlignment.Center };
                if (entry.Entity is { } entity)
                {
                    if (!entity.Valid) { label.Text+=" [invalid]"; label.Foreground=Brushes.OrangeRed; }
                    if (!entity.Active) label.Text+=" [inactive]";
                    if (!entity.Visible) label.Opacity=.55;
                }
                var row = new StackPanel { Orientation=Orientation.Horizontal, Spacing=7 };
                // Original vector glyphs: no upstream bitmap or font asset is redistributed.
                row.Children.Add(new PathIcon { Width=13, Height=13, Foreground=entry.Entity == null ? Brushes.Goldenrod : Brushes.LightSkyBlue,
                    Data=global::Avalonia.Media.Geometry.Parse(entry.Entity == null ? "M0,3 L5,3 L7,5 L14,5 L14,13 L0,13 Z" : "M2,0 L10,0 L14,4 L14,14 L2,14 Z M9,0 L9,5 L14,5") });
                row.Children.Add(label);
                var item = new TreeViewItem { Header=row, Tag=entry, IsExpanded=initial ? depth<3 : expanded.Contains(entry.Key) };
                treeItems.Add(entry.Key,item); item.ItemsSource=entry.Children.Select(n => Build(n,depth+1)).ToArray();
                if (entry.Entity != null)
                {
                    var edit=Item("Inspect (read-only)",()=>OpenTreeEditor(entry));
                    var actions=new List<MenuItem>{edit};
                    foreach(string action in new[]{"Duplicate","Delete","Hide","Show","Activate","Deactivate"})
                    { var menuItem=Item(action,()=>RunTreeAction(entry,action));menuItem.IsEnabled=ModelTreeSnapshot.CanExecute(entry,action);actions.Add(menuItem); }
                    item.ContextMenu=new ContextMenu{ItemsSource=actions};
                }
                ToolTip.SetTip(item,entry.Entity == null ? entry.Caption : entry.Entity.GetType().Name+" — "+entry.Caption);
                return item;
            }
            foreach(var (control,view) in new[]{(geometryTree,ViewGeometryModelResults.Geometry),(tree,ViewGeometryModelResults.Model),(resultsTree,ViewGeometryModelResults.Results)})
            {
                control.ItemsSource=ModelTreeSnapshot.Build(mainView.Controller,view,model?.Results).Select(e=>Build(e,0)).ToArray();
                if(selected!=null && treeItems.TryGetValue(selected,out var item) && (item.Tag as TreeEntry)?.View==view) control.SelectedItem=item;
            }
            UpdateUnits(mainView.Controller.Model.UnitSystem);
        }
        finally { changingTree=false; }
    }
    private FeMesh? DisplayedMesh => TreeViewMode == ViewGeometryModelResults.Geometry ? mainView.Controller.Model.Geometry :
        TreeViewMode == ViewGeometryModelResults.Results ? model?.Results?.Mesh ?? mainView.Controller.CurrentResult?.Mesh : mainView.Controller.Model.Mesh;

    private void SwitchTreeView()
    {
        if(loading)return;
        mainView.Loading=true;
        try
        {
            if(TreeViewMode==ViewGeometryModelResults.Results && mainView.Controller.CurrentResult==null && model?.Results!=null)
                mainView.Controller.SetResults(model.Results);
            if(TreeViewMode!=ViewGeometryModelResults.Results || mainView.Controller.CurrentResult!=null) mainView.Controller.CurrentView=TreeViewMode;
            if(DisplayedMesh is {} mesh) ViewportModelBridge.LoadMesh(viewport,mesh); else viewport.Clear();
        }
        catch(Exception error){Log(error.ToString());}
        finally{mainView.Loading=false;RefreshModelTrees();}
    }
    private void RunTreeAction(TreeEntry entry,string action)
    {
        if(loading)return;
        mainView.Loading=true;
        try
        {
            ModelTreeSnapshot.Execute(mainView.Controller,entry,action);
            if(entry.Kind=="Part" && DisplayedMesh is {} mesh)ViewportModelBridge.LoadMesh(viewport,mesh,false);
            Log($"TREE {action} {entry.Key} commands={mainView.Controller.GetActiveCommandCount()}");
        }
        catch(Exception error){Log(error.ToString());SetStatus(error.Message);}
        finally{mainView.Loading=false;RefreshModelTrees();}
    }
    private void SelectTreeEntity(TreeEntry entry)
    {
        if(entry.Entity==null || DisplayedMesh is not {} mesh)return;
        if(entry.Kind=="ResultField")
        { if(entry.Caption=="S")Contour("S (Mises)");else if(entry.Caption=="U")Contour("U (magnitude)");return; }
        int[] ids=entry.Entity switch
        {
            BasePart p=>p.Labels,
            FeNodeSet n=>n.Labels,
            FeElementSet e=>e.Labels,
            FeSurface s=>s.FaceIds?.Select(id=>id/10).Distinct().ToArray()??[],
            _=>[]
        };
        if(ids.Length==0){viewport.ClearSelection();return;}
        bool nodes=entry.Entity is FeNodeSet;
        viewport.Execute(v=>{v.Highlight(ids.Select(id=>(long)id).ToArray(),nodes);v.Render();});
        SetStatus(entry.Caption+": "+ids.Length+(nodes?" nodes":" elements"));
    }
    private void SelectPickedPart(PickResult pick)
    {
        if(pick.Hit==0 || DisplayedMesh is not {} mesh || !mesh.Elements.TryGetValue((int)pick.ElementId,out var element))return;
        var part=mesh.Parts.Values.FirstOrDefault(p=>p.PartId==element.PartId);if(part==null)return;
        var match=treeItems.Values.FirstOrDefault(t=>t.Tag is TreeEntry e && e.View==TreeViewMode && e.Kind=="Part" && e.Entity?.Name==part.Name);
        if(match==null)return;
        changingTree=true;
        try{(TreeViewMode==ViewGeometryModelResults.Geometry?geometryTree:TreeViewMode==ViewGeometryModelResults.Results?resultsTree:tree).SelectedItem=match;}
        finally{changingTree=false;}
    }

    // Item 3 replaces inspection with editable shared-ViewModel sessions; unsupported entities remain read-only.
    private void OpenTreeEditor(TreeEntry entry)
    {
        if(entry.Entity==null)return;
        var text=string.Join(Environment.NewLine,TypeDescriptor.GetProperties(entry.Entity).Cast<PropertyDescriptor>()
            .Where(p=>p.IsBrowsable).Select(p=>p.DisplayName+": "+p.GetValue(entry.Entity)));
        if(entry.Entity is CaeModel.CompositeLayup layup)
            text+=Environment.NewLine+Environment.NewLine+"Plies (model units)"+Environment.NewLine+
                string.Join(Environment.NewLine,layup.Plies.Select((p,i)=>$"{i+1}. {p.Name} | {p.MaterialName} | thickness {p.Thickness} | angle {p.AngleDeg} deg | elements {p.ElementsThroughThickness}"));
        var dialog=new Window{Title="Inspect "+entry.Caption,Width=650,Height=550,Content=new TextBox{Text=text,IsReadOnly=true,AcceptsReturn=true,Margin=new Thickness(16)}};
        _=dialog.ShowDialog(this);
    }
}
