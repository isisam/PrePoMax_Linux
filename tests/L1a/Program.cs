using System;
using System.IO;
using System.Linq;
using CaeGlobals;
using CaeMesh;

static class RuntimeChecks
{
    static void Check(bool passed, string name)
    {
        if (!passed) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
    static int Main(string[] args)
    {
        try
        {
            // Deterministic process fixtures used by verify_processes.py; no FE work is performed.
            if (args.Length > 0 && args[0].StartsWith("L1a_fake_", StringComparison.Ordinal))
            {
                if (args[0] == "L1a_fake_failure") return 7;
                File.WriteAllText(args[0] + ".pid", Environment.ProcessId.ToString());
                if (args[0] != "L1a_fake_descendant")
                {
                    var info = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath)
                        { UseShellExecute = false, CreateNoWindow = true };
                    info.ArgumentList.Add("L1a_fake_descendant");
                    System.Diagnostics.Process.Start(info);
                }
                System.Threading.Thread.Sleep(30000);
                return 0;
            }
            Check(typeof(string).DeepClone() == typeof(string), "BCL System.Type clone");
            Check(new[] { typeof(LinearHexaElement), typeof(StringLengthConverter) }.DeepClone()
                .SequenceEqual(new[] { typeof(LinearHexaElement), typeof(StringLengthConverter) }), "Type[] clone");
            var equation = new EquationContainer(typeof(StringDoubleConverter), 42);
            Check(equation.DeepClone().Value == 42, "EquationContainer converter Type clone");
            var part = new MeshPart("part", 1, new[] { 1 }, new[] { 1 }, new[] { typeof(LinearHexaElement) });
            Check(part.DeepClone().ElementTypes[0] == typeof(LinearHexaElement), "BasePart element Type[] clone");
            var count = new ElementTypeCount(typeof(LinearHexaElement), "C3D8", 7).DeepClone();
            Check(count.ElementType == typeof(LinearHexaElement) && count.Count == 7, "ElementTypeCount Type clone");
            Check(Math.Abs(MyNCalc.SolveEquation("=2*(3+4)+Sqrt(16)") - 18) < 1e-12, "NuGet NCalc runtime");
            Check(UnitsNet.Length.FromMillimeters(1500).Meters == 1.5, "NuGet UnitsNet runtime");
            var queue = new Priority_Queue.SimplePriorityQueue<string>();
            queue.Enqueue("b", 2); queue.Enqueue("a", 1);
            Check(queue.Dequeue() == "a" && queue.Dequeue() == "b", "NuGet OptimizedPriorityQueue runtime");
            var assemblies = new[] { typeof(Tools).Assembly, typeof(FeMesh).Assembly, typeof(CaeModel.FeModel).Assembly,
                typeof(CaeResults.FeResults).Assembly };
            Check(assemblies.All(a => a.GetReferencedAssemblies().All(n => n.Name != "System.Windows.Forms" &&
                n.Name != "WinFormsFacade" && n.Name != "StrayNamespaces" && n.Name != "vtkControl")), "net8 core has no UI/facade assembly dependencies");
            try { GmshCommon.Gmsh.Initialize(); throw new Exception("Stub returned successfully"); }
            catch (NotSupportedException) { Console.WriteLine("PASS: Gmsh stub explicitly refuses meshing"); }
            if (args.Length > 0 && args[0] == "--native")
            {
                Lib3MF.Lib3MFWrapper.GetLibraryVersion(out uint major, out uint minor, out uint micro);
                Check(major >= 2, $"lib3mf resolver/version {major}.{minor}.{micro}");
                var lp = typeof(FeMesh).Assembly.GetType("CaeMesh.LpSolve", true);
                var handle = (IntPtr)lp.GetMethod("make_lp").Invoke(null, new object[] { 0, 1 });
                Check(handle != IntPtr.Zero, "lpsolve resolver/make_lp");
                lp.GetMethod("delete_lp").Invoke(null, new object[] { handle });
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
