// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml.Styling;
using AvaloniaEdit;

namespace PrePoMax.Avalonia;

public sealed class KeywordViewerWindow : Window
{
    public TextEditor Deck {get;}=Editor();
    public TextEditor Keyword {get;}=Editor();
    public TextBox Search {get;}=new(){Watermark="Search the deck",MinWidth=250};
    public CheckBox Hide {get;}=new(){Content="Hide large mesh data",IsChecked=true};
    public Button FindNext {get;}=new(){Content="Find next"};
    public Button Done {get;}=new(){Content="Close"};
    public TextBlock SearchStatus {get;}=new();
    public KeywordDocument Document {get;private set;}=null!;
    public TreeView KeywordTree {get;}=new();
    private readonly Controller controller;
    private readonly TextBlock note=new(){TextWrapping=TextWrapping.Wrap};
    private static TextEditor Editor()=>new(){IsReadOnly=true,ShowLineNumbers=true,FontFamily=FontFamily.Parse("monospace"),FontSize=13,HorizontalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,VerticalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto};
    public KeywordViewerWindow(Controller c)
    {
        controller=c;Title="CalculiX keywords — read-only (L3)";Width=1320;Height=900;MinWidth=900;MinHeight=600;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Styles.Add(new StyleInclude(new Uri("avares://PrePoMax.Avalonia/")){Source=new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml")});
        var root=new Grid{RowDefinitions=new RowDefinitions("Auto,Auto,*"),Margin=new Thickness(12)};
        note.Text="L3: editing is disabled. The keyword and deck views are read-only.";root.Children.Add(note);
        var tools=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,Margin=new Thickness(0,10)};
        tools.Children.Add(Search);tools.Children.Add(FindNext);tools.Children.Add(Hide);tools.Children.Add(SearchStatus);tools.Children.Add(Done);
        Grid.SetRow(tools,1);root.Children.Add(tools);
        var body=new Grid{ColumnDefinitions=new ColumnDefinitions("330,5,*")};
        body.Children.Add(KeywordTree);var split=new GridSplitter{ResizeDirection=GridResizeDirection.Columns};Grid.SetColumn(split,1);body.Children.Add(split);
        var editors=new Grid{RowDefinitions=new RowDefinitions("Auto,220,5,Auto,*")};
        editors.Children.Add(new TextBlock{Text="Selected keyword (read-only)"});Grid.SetRow(Keyword,1);editors.Children.Add(Keyword);
        var horizontal=new GridSplitter{ResizeDirection=GridResizeDirection.Rows};Grid.SetRow(horizontal,2);editors.Children.Add(horizontal);
        var deckLabel=new TextBlock{Text="CalculiX deck (read-only)"};Grid.SetRow(deckLabel,3);editors.Children.Add(deckLabel);Grid.SetRow(Deck,4);editors.Children.Add(Deck);
        Grid.SetColumn(editors,2);body.Children.Add(editors);Grid.SetRow(body,2);root.Children.Add(body);Content=root;
        KeywordTree.SelectionChanged+=(_,_)=>{if(KeywordTree.SelectedItem is TreeViewItem{Tag:KeywordNode node}){Keyword.Text=node.Data;Deck.Select(node.Offset,node.Data.Length);Deck.ScrollToLine(Deck.Document.GetLineByOffset(node.Offset).LineNumber);}};
        FindNext.Click+=(_,_)=>Find();Search.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){Find();e.Handled=true;}};
        Done.Click+=(_,_)=>Close();Hide.IsCheckedChanged+=(_,_)=>Reload(controller);
        AddHandler(InputElement.KeyDownEvent,(_,e)=>{if(e.Key==Key.F&&e.KeyModifiers==KeyModifiers.Control){Search.Focus();e.Handled=true;}},global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Reload(controller);
    }
    public void Reload(Controller c)
    {
        Document=KeywordDocument.Create(c,Hide.IsChecked==true);Deck.Text=Document.Text;Keyword.Text="";
        TreeViewItem Build(KeywordNode node)=>new(){Header=new TextBlock{Text=node.Caption,FontWeight=node.User||node.Modified?FontWeight.Bold:FontWeight.Normal},Tag=node,ItemsSource=node.Children.Select(Build).ToArray()};
        var root=new TreeViewItem{Header="CalculiX inp file",IsExpanded=true,ItemsSource=Document.Nodes.Select(Build).ToArray()};
        KeywordTree.ItemsSource=new[]{root};KeywordTree.SelectedItem=root.Items.Cast<TreeViewItem>().FirstOrDefault();
        note.Text="L3: editing is disabled. The keyword and deck views are read-only."+(Document.Warnings.Count==0?"":" "+string.Join("; ",Document.Warnings));
        SearchStatus.Text="";
    }
    private void Find()
    {
        string query=Search.Text??"";if(query.Length==0){SearchStatus.Text="Enter search text";return;}
        int start=Math.Min(Deck.Text.Length,Deck.SelectionStart+Deck.SelectionLength);
        int found=Deck.Text.IndexOf(query,start,StringComparison.OrdinalIgnoreCase);
        if(found<0)found=Deck.Text.IndexOf(query,StringComparison.OrdinalIgnoreCase);
        if(found<0){SearchStatus.Text="No matches";return;}
        Deck.Select(found,query.Length);Deck.ScrollToLine(Deck.Document.GetLineByOffset(found).LineNumber);Deck.Focus();SearchStatus.Text="Match at line "+Deck.Document.GetLineByOffset(found).LineNumber;
    }
}
