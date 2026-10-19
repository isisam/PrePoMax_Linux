// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
using Avalonia.Input;
using System.Text.Json;
using Avalonia.Interactivity;

namespace PrePoMax.Avalonia;

public sealed partial class MainWindow
{
    private KeywordViewerWindow? keywordViewer;
    private readonly MenuItem keywordMenu=new(){Header="CalculiX keywords (read-only)"};
    private object? KeywordEvidenceState()=>keywordViewer is {} viewer?new {
        readOnly=viewer.Deck.IsReadOnly&&viewer.Keyword.IsReadOnly,
        deckLength=viewer.Deck.Text.Length,selectionStart=viewer.Deck.SelectionStart,selectionLength=viewer.Deck.SelectionLength,
        query=viewer.Search.Text,status=viewer.SearchStatus.Text,
        selectedCaption=(viewer.KeywordTree.SelectedItem as TreeViewItem)?.Tag is KeywordNode n?n.Caption:null,
        nodes=viewer.Document.Flatten().Count(),users=viewer.Document.Flatten().Count(n=>n.User),warnings=viewer.Document.Warnings
    }:null;
    private void KeywordEvidenceCommand(string op,JsonElement command)
    {
        if(op=="keyword-open"){keywordMenu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));return;}
        var viewer=keywordViewer??throw new InvalidOperationException("Keyword viewer is closed");
        if(op=="keyword-search"){viewer.Search.Text=command.GetProperty("query").GetString();viewer.FindNext.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
        if(op=="keyword-hide")viewer.Hide.IsChecked=command.GetProperty("hide").GetBoolean();
        if(op=="keyword-focus")viewer.Deck.Focus();
        if(op=="keyword-select-user")
        {
            IEnumerable<TreeViewItem> Walk(TreeViewItem item){yield return item;foreach(var child in item.Items.Cast<TreeViewItem>())foreach(var node in Walk(child))yield return node;}
            var query=command.GetProperty("query").GetString()!;
            var item=viewer.KeywordTree.Items.Cast<TreeViewItem>().SelectMany(Walk).First(i=>i.Tag is KeywordNode{User:true} n&&n.Data.Contains(query,StringComparison.OrdinalIgnoreCase));
            viewer.KeywordTree.SelectedItem=item;
        }
        if(op=="keyword-close")viewer.Done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(op=="keyword-dump")
        {
            object Node(KeywordNode n)=>new{caption=n.Caption,data=n.Data,user=n.User,children=n.Children.Select(Node).ToArray()};
            File.WriteAllText(command.GetProperty("path").GetString()!,JsonSerializer.Serialize(new {
                readOnly=viewer.Deck.IsReadOnly&&viewer.Keyword.IsReadOnly,deck=viewer.Deck.Text,nodes=viewer.Document.Nodes.Select(Node),
                warnings=viewer.Document.Warnings,suppressed=viewer.Document.SuppressedUserKeywords
            },new JsonSerializerOptions{WriteIndented=true}));
        }
    }
    private void OpenKeywordViewer()
    {
        if(model==null||loading)return;
        if(keywordViewer!=null){keywordViewer.Activate();return;}
        try
        {
            var dialog=keywordViewer=new KeywordViewerWindow(mainView.Controller);
            dialog.AddHandler(InputElement.KeyDownEvent,HistoryKeyDown,global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
            dialog.Closed+=(_,_)=>keywordViewer=null;
            _=dialog.ShowDialog(this);
        }
        catch(Exception error){Log(error.ToString());SetStatus("Keyword viewer failed: "+error.Message);}
    }
}
