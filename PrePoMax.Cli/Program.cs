using System;
using System.IO;
using System.Linq;
using System.Globalization;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeJob;
using CaeResults;
using FileInOut.Output;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            // A separate Linux guardian sets its parent-death signal before starting the solver.
            if (args.Length > 0 && args[0] == "--process-host") return LinuxProcessHost.Run(args.Skip(1).ToArray());
            ChildProcessJob.LinuxHostAssembly = typeof(Program).Assembly.Location;
            if (args.Length == 0 || args[0] == "--help")
            {
                Console.WriteLine("prepomax-cli import <in.inp|in.pmx> [export-inp <out.inp>] [save <out.pmx>]\n" +
                    "prepomax-cli export-inp <in.inp|in.pmx> <out.inp>\n" +
                    "prepomax-cli save <in.inp|in.pmx> <out.pmx>\n" +
                    "prepomax-cli run <model.inp> --solver <ccx> [--cpus 1..8] [--timeout seconds]\n" +
                    "prepomax-cli results <job.frd> --summary\n" +
                    "PMX: trusted files only; model data only. UI history, annotations, jobs and results are not saved.");
                return 0;
            }
            if (args.Length < 2) throw new ArgumentException("Missing input file. Use --help.");
            if (args[0] == "run") return Run(args);
            if (args[0] == "results")
            {
                if (args.Length != 3 || args[2] != "--summary") throw new ArgumentException("Use results <job.frd> --summary.");
                var results = FrdFileReader.Read(Path.GetFullPath(args[1]));
                if (results?.Mesh == null) throw new InvalidDataException("No results mesh.");
                Console.WriteLine($"nodes={results.Mesh.Nodes.Count} elements={results.Mesh.Elements.Count}");
                var increments = results.GetAllExistingIncrementIds();
                int step = increments.Keys.Max(), increment = increments[step].Max();
                int[] nodes = results.Mesh.Nodes.Keys.OrderBy(id => id).ToArray();
                foreach (string field in results.GetAllFieldNames())
                {
                    Console.WriteLine("field=" + field);
                    foreach (string component in results.GetFieldComponentNames(field))
                    {
                        var data = results.GetFieldData(field, component, step, increment);
                        if (data == null) continue;
                        var values = results.GetValues(data, nodes);
                        Console.WriteLine($"  {component} min={values.Min():G9} max={values.Max():G9}");
                    }
                }
                return 0;
            }
            var model = Load(args[1]);
            if (args[0] == "import")
            {
                if (args.Length % 2 != 0) throw new ArgumentException("Actions require output filenames.");
                for (int i = 2; i < args.Length; i += 2) Write(model, args[i], args[i + 1]);
            }
            else
            {
                if (args.Length != 3) throw new ArgumentException("Expected one input and one output filename.");
                Write(model, args[0], args[2]);
            }
            Console.WriteLine($"nodes={model.Mesh.Nodes.Count} elements={model.Mesh.Elements.Count} " +
                $"materials={model.Materials.Count} layups={model.CompositeLayups.Count}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); return 1; }
    }
    private static FeModel Load(string path)
    {
        path = Path.GetFullPath(path);
        if (Path.GetExtension(path).Equals(".pmx", StringComparison.OrdinalIgnoreCase)) return PmxModelFile.Load(path);
        if (!Path.GetExtension(path).Equals(".inp", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Expected .inp or .pmx input.");
        var model = new FeModel("Model-1", null, true);
        model.Properties.ModelSpace = ModelSpaceEnum.ThreeD;
        var errors = model.ImportModelFromInpFile(path, Console.Error.WriteLine, preserveUnsupportedUserMaterials: true);
        if (errors != null && errors.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        return model;
    }
    private static void Write(FeModel model, string action, string path)
    {
        if (action == "save") PmxModelFile.Save(Path.GetFullPath(path), model);
        else if (action == "export-inp") CalculixFileWriter.Write(Path.GetFullPath(path), model, ConvertPyramidsToEnum.Wedges, false);
        else throw new ArgumentException("Unknown action: " + action);
    }
    private static int Run(string[] args)
    {
        string solver = null;
        int cpus = 1, timeout = 600;
        if (args.Length % 2 != 0) throw new ArgumentException("Run options require values.");
        for (int i = 2; i < args.Length; i += 2)
        {
            if (args[i] == "--solver") solver = Path.GetFullPath(args[i + 1]);
            else if (args[i] == "--cpus") cpus = int.Parse(args[i + 1]);
            else if (args[i] == "--timeout") timeout = int.Parse(args[i + 1]);
            else throw new ArgumentException("Unknown option: " + args[i]);
        }
        if (solver == null || !File.Exists(solver)) throw new ArgumentException("--solver must name an existing executable.");
        if (cpus < 1 || cpus > 8 || timeout <= 0 || timeout > 86400) throw new ArgumentException("Use 1..8 CPUs and timeout 1..86400 seconds.");
        string input = Path.GetFullPath(args[1]);
        if (!File.Exists(input)) throw new FileNotFoundException("Input deck does not exist.", input);
        string name = Path.GetFileNameWithoutExtension(input);
        var job = new AnalysisJob(name, solver, AnalysisJob.GetDefaultArgument(FEMSolverEnum.Calculix, name), Path.GetDirectoryName(input));
        job.NumCPUs = cpus;
        ConsoleCancelEventHandler cancel = (sender, e) => { e.Cancel = true; job.Kill("Cancelled by user."); };
        Console.CancelKeyPress += cancel;
        try { job.Submit(1, 1, useBackgroundWorker: false, timeoutMilliseconds: timeout * 1000); }
        finally { Console.CancelKeyPress -= cancel; }
        Console.WriteLine(job.GetAllOutputData());
        Console.WriteLine("status=" + job.JobStatus);
        return job.JobStatus == JobStatus.OK ? 0 : 2;
    }
}
