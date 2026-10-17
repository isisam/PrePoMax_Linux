using System;
using System.IO;
using System.Linq;
using System.Globalization;
using CaeGlobals;
using PrePoMax;
using PrePoMax.Commands;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                Console.WriteLine("prepomax-headless inspect <full.pmx>\nprepomax-headless export <full.pmx> <out.inp>\nprepomax-headless replay <full.pmx> <out.inp> [input-directory]\nprepomax-headless roundtrip <full.pmx> <out.pmx>\nprepomax-headless contracts\nTrusted PMX files only. Rendering and meshing require their own backends.");
                return 0;
            }

            if (args[0] == "contracts")
                return CheckContracts();
            if (args.Length < 2)
                throw new ArgumentException("Missing input PMX.");
            if (args[0] == "diagnose")
            {
                using var file = File.OpenRead(args[1]);
                using var reader = new BinaryReader(file);
                reader.ReadBytes(32);
                int length = reader.ReadInt32();
                using var block = Tools.Decompress(new MemoryStream(reader.ReadBytes(length)));
                using var binary = new BinaryReader(block);
                var data = Tools.LoadDumpFromFile<object[]>(binary);
                Console.WriteLine("PMX records=" + data.Length);
                return 0;
            }

            CoreHost.FileReadError = ex => Console.Error.WriteLine(ex);
            SettingsContainer.IsSaveDisabled = true;
            var view = new NullMainView();
            var controller = new Controller(view);
            controller.RegenerationWorkDirectory = args.Length > 3 ? Path.GetFullPath(args[3]) : Path.GetDirectoryName(Path.GetFullPath(args[1]));
            controller.Open(Path.GetFullPath(args[1]));
            int count = controller.GetActiveCommandCount();
            Console.WriteLine("commands=" + count);
            foreach (var command in controller.GetActiveCommands())
                Console.WriteLine(command.GetType().FullName);
            if (args[0] == "inspect")
                return 0;
            if (args.Length < 3)
                throw new ArgumentException("Missing output path.");
            string output = Path.GetFullPath(args[2]);
            if (args[0] == "roundtrip")
            {
                controller.SaveToPmx(output);
                return 0;
            }

            if (args[0] == "replay")
            {
                if (count == 0)
                    throw new InvalidDataException("No history to replay.");
                controller.BatchRegenerationMode = true;
                controller.RegenerationWorkDirectory = args.Length > 3 ? Path.GetFullPath(args[3]) : Path.GetDirectoryName(Path.GetFullPath(args[1]));
                controller.RegenerateHistoryCommands(false, false, RegenerateTypeEnum.All, preserveModelIdentity: true);
                var errors = controller.GetCommandCollectionErrors();
                if (errors.Count != 0)
                    throw new InvalidDataException("History replay failed:\n" + string.Join("\n", errors));
                Console.WriteLine("replayed=" + count);
            }
            else if (args[0] != "export")
                throw new ArgumentException("Unknown operation: " + args[0]);
            controller.ExportToCalculix(output);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int CheckContracts()
    {
        var forbidden = new[]
        {
            "System.Windows.Forms",
            "Kitware.VTK",
            "Kitware.mummy.Runtime",
            "UserControls",
            "vtkControl",
            "PrePoMax"
        };
        foreach (var assembly in new[]
        {
            typeof(Controller).Assembly,
            typeof(vtkControl.IViewport3D).Assembly
        }

        )
            if (assembly.GetReferencedAssemblies().Any(a => forbidden.Contains(a.Name)))
                throw new Exception("UI dependency: " + assembly.FullName);
        var movedTypes = new[]
        {
            typeof(Controller),
            typeof(Command),
            typeof(UserControls.SidePanelsState),
            typeof(vtkControl.vtkMaxColorSpectrum)
        };
        if (!movedTypes.DeepClone().SequenceEqual(movedTypes))
            throw new Exception("Moved System.Type wire records.");
        int count = 0;
        foreach (var type in typeof(Command).Assembly.GetTypes().Where(t => t.IsSerializable && typeof(Command).IsAssignableFrom(t)))
        {
            PmxSerializationBinder.Instance.BindToName(type, out string assembly, out string name);
            if (assembly == null || !assembly.StartsWith("PrePoMax,"))
                throw new Exception("Wrong wire assembly: " + type);
            if (name != type.FullName || PmxSerializationBinder.Instance.BindToType(assembly, name) != type)
                throw new Exception("Wrong wire type: " + type);
            var array = type.MakeArrayType();
            if (PmxSerializationBinder.Instance.BindToType(type.Assembly.FullName, array.FullName) != array)
                throw new Exception("Moved array assembly alias: " + array);
            var list = typeof(System.Collections.Generic.List<>).MakeGenericType(type);
            PmxSerializationBinder.Instance.BindToName(list, out string listAssembly, out string listName);
            if (PmxSerializationBinder.Instance.BindToType(listAssembly ?? list.Assembly.FullName, listName ?? list.FullName) != list)
                throw new Exception("Generic wire type: " + list);
            count++;
        }

        Console.WriteLine("PASS: UI-free contracts; stable command identities=" + count);
        return 0;
    }
}
