// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Threading;
using CaeGlobals;
using CaeMesh;
using System.Drawing;
using vtkControl;
namespace PrePoMax.Avalonia;

/// <summary>Avalonia host callbacks. Editor/form operations remain the L1b NullMainView placeholders until L3.</summary>
internal sealed class ShellMainView(MainWindow window,VtkMaxView viewport) : NullMainView,IMainView
{
    // Controller parsing can issue legacy draw callbacks before the complete mesh is ready.
    // The shell atomically adapts Controller.Model after parsing, then enables direct display callbacks.
    public bool Loading {get;set;}
    public new IViewport3D Viewport=>viewport;
    private static void UI(Action action) {if(Dispatcher.UIThread.CheckAccess())action();else Dispatcher.UIThread.Post(action);}
    public override void WriteDataToOutput(string data)=>UI(()=>window.Log(data));
    public override void WriteErrorToOutput(string data)=>UI(()=>window.Log("ERROR: "+data));
    public override void ShowTimedError(string message,int milliseconds)=>WriteErrorToOutput(message);
    public override void SetTitle(string title)=>UI(()=>window.Title=title);
    public override void SetStateReady(string currentText)=>UI(()=>window.SetStatus(currentText));
    public override bool SetStateWorking(string text,bool showCancelButton=false){SetStateReady(text);return true;}
    public override bool IsStateOpening()=>Loading;
    public override void UpdateUnitSystem(UnitSystem unitSystem)=>UI(()=>window.UpdateUnits(unitSystem));
    public override void RegenerateTree(bool remeshing=false) {if(!Loading)UI(()=>window.RefreshTree());}
    public override void AddTreeNode(ViewGeometryModelResults view,NamedClass item,string parentName)=>RegenerateTree();
    public override void UpdateTreeNode(ViewGeometryModelResults view,string oldItemName,NamedClass item,string parentName,bool updateSelection=true)=>RegenerateTree();
    public override void UpdateTreeNodeLayout(ViewGeometryModelResults view,string itemName,NamedClass item,string parentName)=>RegenerateTree();
    public override void RemoveTreeNode<T>(ViewGeometryModelResults view,string nodeName,string parentName)=>RegenerateTree();
    public override void SelectionChanged(int[] ids=null!)=>UI(()=>window.SetStatus("Selection: "+string.Join(", ",ids??[])));
    public override void SetSelectBy(vtkSelectBy selectBy)=>UI(()=>viewport.SelectBy=selectBy);
    public override void SetSelectItem(vtkSelectItem selectItem)=>UI(()=>viewport.SelectItem=selectItem);
    public override void SetQuerySelection(bool querySelection)=>UI(()=>viewport.QuerySelectionInProgress=querySelection);
    public override bool GetQuerySelection()=>viewport.QuerySelectionInProgress;
    public override void SaveAutomationScreenshot(string fileName,bool transparentBackground,int magnification)=>UI(()=>viewport.RenderToPNG(fileName,transparentBackground,magnification));
    public override void RequestAutomationExit()=>UI(window.Close);
    public override void Clear3D(){if(!Loading)UI(viewport.Clear);}
    public override void Clear3DSelection(){if(!Loading)UI(()=>viewport.ClearSelection());}
    public override void SetFrontBackView(bool animate,bool front){if(!Loading)UI(()=>viewport.SetFrontBackView(animate,front));}
    public override void SetZoomToFit(bool animate){if(!Loading)UI(()=>viewport.SetZoomToFit(animate));}
    public override IViewportActor Add3DCells(vtkMaxActorData data)=>Loading?new NullViewportActor():viewport.AddCells(data);
    public override void Add3DCells(vtkMaxActorData[] data){if(!Loading)UI(()=>viewport.AddCells(data));}
    public override bool ContainsActor(string name)=>!Loading&&viewport.ContainsActor(name);
    public override void HideActors(string[] names,bool update){if(!Loading)UI(()=>viewport.HideActors(names,update));}
    public override void ShowActors(string[] names,bool update){if(!Loading)UI(()=>viewport.ShowActors(names,update));}
    public override void UpdateActor(string oldName,string newName,Color color){if(!Loading)UI(()=>viewport.UpdateActor(oldName,newName,color));}
    public override void SetCurrentEdgesVisibilities(vtkEdgesVisibility visibility){if(!Loading)UI(()=>viewport.EdgesVisibility=visibility);}
    public override void SetScalarBarVisibility(bool visibility){if(!Loading)UI(()=>viewport.SetScalarBarVisibility(visibility));}
    public override void UpdateActorSurfaceScalarField(string name,float[] values,NodesExchangeData extremeNodes,ResultFieldExchangeData resultField,bool update)
    {if(!Loading)UI(()=>viewport.UpdateActorScalarField(name,values,extremeNodes,resultField,update));}
    public override void InvokeIfRequired(Action action) {if(Dispatcher.UIThread.CheckAccess())action();else Dispatcher.UIThread.Invoke(action);}
    public override T InvokeIfRequired<T>(Func<T> func)=>Dispatcher.UIThread.CheckAccess()?func():Dispatcher.UIThread.Invoke(func);
    public override IAsyncResult BeginInvoke(Delegate action)=>Dispatcher.UIThread.InvokeAsync(()=>action.DynamicInvoke()).GetTask();
}
