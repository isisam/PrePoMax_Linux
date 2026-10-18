// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Text.Json;

namespace PrePoMax.Avalonia;
public sealed partial class MainWindow
{
    private object TreeEvidenceState()=>new
    {
        tab=treeTabs.SelectedIndex,
        selected=(new[]{geometryTree,tree,resultsTree}[treeTabs.SelectedIndex].SelectedItem as TreeViewItem)?.Tag is TreeEntry e?e.Key:null,
        commandCount=mainView.Controller.GetActiveCommandCount(),errors=mainView.Controller.GetCommandCollectionErrors(),
        entries=treeItems.Values.Select(t=>(TreeEntry)t.Tag!).Select(e=>new{e.Key,e.Caption,e.Kind,view=e.View.ToString(),e.Step,
            entity=e.Entity?.GetType().Name,e.Entity?.Active,e.Entity?.Visible,e.Entity?.Valid,
            actions=new[]{"Duplicate","Delete","Hide","Show","Activate","Deactivate"}.Where(a=>ModelTreeSnapshot.CanExecute(e,a)).ToArray()}).ToArray()
    };
    private void TreeEvidenceCommand(string op,JsonElement command)
    {
        if(op=="tree-tab"){treeTabs.SelectedIndex=command.GetProperty("index").GetInt32();return;}
        if(!command.TryGetProperty("key",out var key) || !treeItems.TryGetValue(key.GetString()!,out var item))throw new ArgumentException("Tree key missing");
        var entry=(TreeEntry)item.Tag!;
        if(op=="tree-select")
        {new[]{geometryTree,tree,resultsTree}[(int)entry.View].SelectedItem=item;return;}
        if(op=="tree-action")
        {
            var action=command.GetProperty("action").GetString();
            var menuItem=item.ContextMenu!.Items.Cast<MenuItem>().Single(m=>Equals(m.Header,action));
            if(!menuItem.IsEnabled)throw new InvalidOperationException("Disabled action "+action);
            menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
    }
}
