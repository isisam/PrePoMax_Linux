// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
namespace PrePoMax.Avalonia;

internal static class LinuxAnalysis
{
    public static string LocateSolver()
    {
        string? configured=Environment.GetEnvironmentVariable("PPM_CCX_FAT");
        if(!string.IsNullOrEmpty(configured))return Check(configured);
        var paths=new[]{Path.Combine(AppContext.BaseDirectory,"Solver","ccx_fat"),Path.Combine(AppContext.BaseDirectory,"ccx_fat")}
            .Concat((Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator).Select(p=>Path.Combine(p,"ccx_fat")));
        foreach(var path in paths)if(File.Exists(path))return Check(path);
        throw new FileNotFoundException("Linux ccx_fat was not found. Place the release binary in Solver/ccx_fat, or set PPM_CCX_FAT to its absolute path.");
    }
    private static string Check(string path)
    {
        path=Path.GetFullPath(path);using var file=File.OpenRead(path);byte[] magic=new byte[4];
        if(file.Read(magic)!=4||!magic.SequenceEqual(new byte[]{127,69,76,70}))throw new InvalidDataException("Expected the Linux ELF ccx_fat release binary: "+path);
        return path;
    }
    public static async Task<int> Run(string input,string solver,int threads,IProgress<string> log,CancellationToken cancellation)
    {
        if(!OperatingSystem.IsLinux())throw new PlatformNotSupportedException("This analysis action requires Linux.");
        if(threads<1||threads>4)throw new ArgumentOutOfRangeException(nameof(threads),"Use 1–4 solver threads.");
        var start=new ProcessStartInfo("/usr/bin/nice"){WorkingDirectory=Path.GetDirectoryName(input)!,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        foreach(var arg in new[]{"-n","19",Check(solver),"-i",Path.GetFileNameWithoutExtension(input)})start.ArgumentList.Add(arg);
        foreach(var key in new[]{"OMP_NUM_THREADS","CCX_NPROC_EQUATION_SOLVER","OPENBLAS_NUM_THREADS"})start.Environment[key]=threads.ToString(System.Globalization.CultureInfo.InvariantCulture);
        using var process=Process.Start(start)??throw new IOException("Could not launch ccx_fat.");
        using var fileLog=new StreamWriter(Path.Combine(start.WorkingDirectory,"solver.log")){AutoFlush=true};
        fileLog.Write(BuildProvenance.Header);fileLog.WriteLine("solver sha256: "+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(solver))));
        fileLog.WriteLine($"command: nice -n 19 {solver} -i {Path.GetFileNameWithoutExtension(input)}; threads={threads}");
        var writeLock=new object();
        using var registration=cancellation.Register(()=>{try{if(!process.HasExited)process.Kill(entireProcessTree:true);}catch(InvalidOperationException){}});
        log.Report($"SOLVER pid={process.Id} nice=19 threads={threads} path={solver}");
        async Task Pump(StreamReader reader){while(await reader.ReadLineAsync() is {} line){lock(writeLock)fileLog.WriteLine(line);log.Report(line);}}
        await Task.WhenAll(Pump(process.StandardOutput),Pump(process.StandardError),process.WaitForExitAsync(cancellation));
        fileLog.WriteLine("exit="+process.ExitCode);log.Report("SOLVER exit="+process.ExitCode);return process.ExitCode;
    }
}
