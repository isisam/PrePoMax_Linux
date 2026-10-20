# Build the public source

Use existing .NET 10 SDK (the Avalonia app targets net10.0; shared projects target net8.0), CMake/Ninja, C/C++ tools and the public VTK 9.4.2 development tree. Linux scripts build on an ext4 filesystem and accept output paths. Main contains native/vtkmax, PrePoMax.Core.sln and PrePoMax.Avalonia. Start with `dotnet build PrePoMax.Core.sln -c Release -p:CoreTargetFrameworks=net8.0`; for the viewer follow build/Build-Linux-Viewer.sh and its Build-Compat-Native.sh prerequisites. The original L3 final-package scripts describe the historical owner's build environment and are not portable one-command installers. Replace environment-specific output/dependency locations with your own. Runtime package/source versions are listed in the release's third-party notices.

Linux Gmsh meshing intentionally remains a refusal stub. See PUBLIC_GMSH.md. The Windows feature branches preserve layup editor and 3D fibre display source but their C++/CLI meshing project references the excluded SDK; do not copy private libraries back to make a build pass. Windows binary releases are withheld until a public backend/stub has been integrated and tested. Prior RC1g5 150% DPI manual acceptance is still pending.

The v0.4.0-teaching.4 release rebuilds the cleaned source with build/Build-Teaching.sh. Use .NET SDK 10.0.402 (global.json), CMake/Ninja, GCC/G++, X11/EGL/OpenGL development headers, Python 3, rsync, gzip and dpkg-deb on Ubuntu 22.04. Restore uses public NuGet packages, including Antlr 3.1.3.42154; no Windows installation is needed.

From a clean tagged checkout on ext4:

```bash
export SOURCE_REVISION=$(git rev-parse HEAD)
export SOURCE_DATE_EPOCH=$(git show -s --format=%ct HEAD)
bash build/Build-Teaching.sh /opt/ppm-teaching all /new/release-directory
```

Use a fresh build root and delivery directory. The script never deletes previous output. The managed/native/package phases may also be invoked separately. Native and VTK builds use -ffile-prefix-map, -fdebug-prefix-map and -fmacro-prefix-map; managed builds use PathMap, Deterministic, ContinuousIntegrationBuild and DebugType=None. Source and binary archives normalize owner, ordering, timestamps and gzip headers. These controls address build paths; they do not claim bit-for-bit reproducibility across different compiler versions.

The application omits the research solver. Select an independently installed stock CalculiX (Ubuntu calculix-ccx, or the companion CalculiX_Win release on Windows). Retained third-party diagnostic/PDB paths must be documented against byte-identical public package inputs. Rebuilding and WSL checks do not establish native Linux/VM/GPU acceptance.

Check the restored expression runtime and the public meshing refusal against the published app:

```bash
dotnet run --project tests/Teaching/ExpressionCheck.csproj -c Release -p:TeachingApp=/opt/ppm-teaching/app
```

Antlr remains a historical .NET Framework package; NU1701 is expected. The release records runtime expression checks instead of hiding this compatibility warning.
