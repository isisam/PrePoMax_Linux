// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
namespace PrePoMax.Avalonia;

internal static class WslRenderer
{
    [DllImport("libc",EntryPoint="setenv",SetLastError=true)]
    private static extern int SetNativeEnvironment(string name,string value,int overwrite);
    // WSL's D3D12 driver can unload while Skia's background GL worker is still
    // executing in it during process exit. See docs/L3b_SHUTDOWN.md and traces.
    // This is a WSL-specific mitigation, not a claim about native GPU acceptance.
    public static void Configure()
    {
        if(!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("PPM_WSL_GPU")=="1")return;
        string release;
        try{release=File.ReadAllText("/proc/sys/kernel/osrelease");}catch(IOException){return;}
        if(!release.Contains("microsoft",StringComparison.OrdinalIgnoreCase))return;
        // Explicit renderer settings remain authoritative.
        if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GALLIUM_DRIVER")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LIBGL_ALWAYS_SOFTWARE")))return;
        // .NET's Unix environment setters update a managed cache. Mesa reads
        // libc getenv, so configure both before loading the graphics libraries.
        foreach(var pair in new[]{("LIBGL_ALWAYS_SOFTWARE","true"),("GALLIUM_DRIVER","llvmpipe")})
        {
            if(SetNativeEnvironment(pair.Item1,pair.Item2,1)!=0)throw new IOException("Unable to configure the WSL renderer: "+Marshal.GetLastPInvokeError());
            Environment.SetEnvironmentVariable(pair.Item1,pair.Item2);
        }
        Console.WriteLine("WSL renderer: llvmpipe. Set PPM_WSL_GPU=1 to opt into the hardware driver.");
    }
}
