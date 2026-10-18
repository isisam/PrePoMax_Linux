// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls;
namespace PrePoMax.Avalonia;
public sealed partial class MainWindow
{
    private readonly NumericUpDown threads=new(){Minimum=1,Maximum=4,Value=1,Increment=1,Width=110,FormatString="0"};
    private CancellationTokenSource? solverCancellation;
    private string? analysisDirectory;
    private async Task RunAnalysis(string? requestedDirectory=null)
    {
        if(model==null||loading||solverCancellation!=null)return;
        loading=true;mainView.Loading=true;progressBar.IsVisible=true;solverCancellation=new();
        try
        {
            string solver=LinuxAnalysis.LocateSolver();
            analysisDirectory=requestedDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PrePoMax-RCM","analyses",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(analysisDirectory);string input=Path.Combine(analysisDirectory,"model.inp");
            await Task.Run(()=>mainView.Controller.ExportToCalculix(input,binaryResultsFileAllowed:false));
            IProgress<string> progress=new Progress<string>(line=>{Log(line);status.Text=line;});
            int exit=await LinuxAnalysis.Run(input,solver,(int)(threads.Value??1),progress,solverCancellation.Token);
            string frd=Path.ChangeExtension(input,".frd");
            if(File.Exists(frd))
            {await model.LoadResultsAsync(frd,progress);Contour("S (Mises)");Log(exit==0?"Analysis completed; FRD opened.":"Partial FRD opened; solver failed, exit "+exit);}
            if(exit!=0)throw new InvalidOperationException("ccx_fat failed with exit "+exit+". Partial results do not establish a completed analysis.");
            status.Text="Analysis completed — results opened";
        }
        catch(OperationCanceledException){Log("Analysis cancelled.");status.Text="Analysis cancelled";}
        catch(Exception e){Log(e.ToString());status.Text="Analysis failed: "+e.Message;}
        finally
        {
            if(analysisDirectory!=null)File.WriteAllText(Path.Combine(analysisDirectory,"shell-analysis.json"),System.Text.Json.JsonSerializer.Serialize(new{status=status.Text,directory=analysisDirectory,increment=model?.ResultsIncrement}));
            solverCancellation?.Dispose();solverCancellation=null;loading=false;mainView.Loading=false;progressBar.IsVisible=false;
        }
    }
}
