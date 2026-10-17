using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class NativeLibraryResolver
{
    [ModuleInitializer]
    internal static void Initialize() => NativeLibrary.SetDllImportResolver(
        typeof(NativeLibraryResolver).Assembly, Resolve);

    private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string[] candidates;
        if (name == "lib3mf.dll")
            candidates = OperatingSystem.IsWindows() ? new[] { name } :
                OperatingSystem.IsMacOS() ? new[] { "lib3mf.dylib" } : new[] { "lib3mf.so", "lib3mf.so.2" };
        else if (name == "lpsolve55.dll")
            candidates = OperatingSystem.IsWindows() ? new[] { name } :
                OperatingSystem.IsMacOS() ? new[] { "liblpsolve55.dylib" } : new[] { "liblpsolve55.so", "liblpsolve55.so.0" };
        else return IntPtr.Zero;
        foreach (string candidate in candidates)
        {
            foreach (string directory in new[] { AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "lib") })
                if (NativeLibrary.TryLoad(Path.Combine(directory, candidate), out var local)) return local;
            if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var system)) return system;
        }
        return IntPtr.Zero; // The runtime supplies its normal detailed DllNotFoundException.
    }
}
