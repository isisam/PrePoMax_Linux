// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using CaeGlobals;
using PrePoMax.Viewer;
using PrePoMax.Commands;

namespace PrePoMax.Avalonia;

public sealed partial class MainWindow
{
    private readonly MenuItem undoMenu=new(){Header="Undo",InputGesture=new KeyGesture(Key.Z,KeyModifiers.Control)};
    private readonly MenuItem redoMenu=new(){Header="Redo",InputGesture=new KeyGesture(Key.Y,KeyModifiers.Control)};
    private string? undoName,redoName,historyError;
    private bool historyBusy,historyMenuBound;
    private int historySequence;

    private MenuItem BuildEditMenu()
    {
        if(!historyMenuBound)
        {
            undoMenu.Click+=async (_,_)=>await ChangeHistory(false);
            redoMenu.Click+=async (_,_)=>await ChangeHistory(true);
            keywordMenu.Click+=(_,_)=>OpenKeywordViewer();
            historyMenuBound=true;
        }
        SetHistoryActions(undoName,redoName);
        return new MenuItem{Header="Edit",ItemsSource=new[]{undoMenu,redoMenu,keywordMenu}};
    }
    internal void SetHistoryActions(string? undo,string? redo)
    {
        undoName=undo;redoName=redo;
        undoMenu.Header=undo==null?"Undo":"Undo - "+undo;
        redoMenu.Header=redo==null?"Redo":"Redo - "+redo;
        undoMenu.IsEnabled=undo!=null&&!loading&&!historyBusy;
        redoMenu.IsEnabled=redo!=null&&!loading&&!historyBusy;
        keywordMenu.IsEnabled=model!=null&&!loading&&!historyBusy;
    }
    private void HistoryKeyDown(object? sender,KeyEventArgs e)
    {
        if(e.KeyModifiers!=KeyModifiers.Control||e.Key is not (Key.Z or Key.Y))return;
        e.Handled=true;
        _=ChangeHistory(e.Key==Key.Y);
    }
    private async Task<RegenerateTypeEnum?> ChooseRegeneration()
    {
        var dialog=new Window{Title="Regenerate history",Width=480,Height=220,CanResize=false,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var all=new RadioButton{Content="All commands",GroupName="scope",IsChecked=true};
        var pre=new RadioButton{Content="Pre-processing commands only",GroupName="scope"};
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,HorizontalAlignment=HorizontalAlignment.Right};
        var ok=new Button{Content="OK"};var cancel=new Button{Content="Cancel"};
        ok.Click+=(_,_)=>dialog.Close((RegenerateTypeEnum?)(pre.IsChecked==true?RegenerateTypeEnum.PreProcess:RegenerateTypeEnum.All));
        cancel.Click+=(_,_)=>dialog.Close((RegenerateTypeEnum?)null);
        buttons.Children.Add(ok);buttons.Children.Add(cancel);
        dialog.Content=new StackPanel{Margin=new Thickness(18),Spacing=12,Children={new TextBlock{Text="Choose which history commands to regenerate."},all,pre,buttons}};
        return await dialog.ShowDialog<RegenerateTypeEnum?>((Window?)activeEditor??this);
    }
    private async Task ChangeHistory(bool redo)
    {
        if(loading||historyBusy||(redo?redoName:undoName)==null)return;
        historyBusy=true;historyError=null;SetHistoryActions(undoName,redoName);
        var editor=activeEditor;
        string? kind=editor?.Session.Kind,name=editor?.Session.OriginalName,step=activeEditorStep;
        var newEditor=activeEditorNew;
        try
        {
            var type=RegenerateTypeEnum.All;
            if(!redo&&mainView.Controller.IsRegenerateTypeUsedByUndo())
            {
                var selected=await ChooseRegeneration();if(selected==null)return;type=selected.Value;
            }
            loading=true;mainView.Loading=true;progressBar.IsVisible=true;
            if(editor!=null)editor.IsEnabled=false;
            if(keywordViewer!=null)keywordViewer.IsEnabled=false;
            treeTabs.IsEnabled=false;viewport.IsEnabled=false;
            SetStatus(redo?"Redoing…":"Undoing…");
            // Controller regeneration replays commands and may read PMX/mesh files.
            // Suppress incremental drawing and adapt the completed model on the UI thread.
            await Task.Run(()=>{if(redo)mainView.Controller.RedoHistory();else mainView.Controller.UndoHistory(type);});
            var errors=mainView.Controller.GetCommandCollectionErrors();
            if(errors?.Count>0)throw new InvalidOperationException(string.Join(Environment.NewLine,errors));
            historySequence++;
        }
        catch(Exception error){historyError=error.ToString();Log(historyError);SetStatus("History failed: "+error.Message);}
        finally
        {
            if(loading)
            {
                // A form draft owns detached model references. Recreate it from the
                // regenerated entity so an old draft cannot overwrite an undone edit.
                editor?.Close();
                try
                {
                    model=new ViewerModel(mainView.Controller.Model);
                    ViewportModelBridge.Load(viewport,model);RefreshModelTrees();
                    keywordViewer?.Reload(mainView.Controller);
                    if(editor!=null)
                    {
                        var entry=treeItems.Values.Select(i=>i.Tag).OfType<TreeEntry>().FirstOrDefault(e=>e.Kind==kind&&e.Entity?.Name==name&&e.Step==step);
                        if(name==null)newEditor?.Invoke();
                        else if(entry!=null&&CanEdit(entry))ShowEntityEditor(CreateSession(entry),step:step);
                        else Log("The open form's entity was removed by history; form closed.");
                    }
                    if(historyError==null)SetStatus(redo?"Redo complete":"Undo complete");
                }
                catch(Exception error){historyError=error.ToString();Log(historyError);SetStatus("History refresh failed: "+error.Message);}
            }
            if(editor!=null)editor.IsEnabled=true;
            if(keywordViewer!=null)keywordViewer.IsEnabled=true;
            loading=false;mainView.Loading=false;historyBusy=false;progressBar.IsVisible=false;
            treeTabs.IsEnabled=true;viewport.IsEnabled=true;SetHistoryActions(undoName,redoName);
        }
    }
}
