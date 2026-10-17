# Shared core and headless CLI

The seven core projects use one source tree and SDK projects targeting
`net48;net8.0`. `CoreTargetFrameworks` selects a build slice. Existing assembly,
namespace, serializable type and field identities are retained. WinForms-only
CaeGlobals files and the Extensions UI partial compile only for net48, in their
original assembly. `IMessageSink`, `IPlatformPaths` and `IProcessTreeService`
provide the host boundary. net48 defaults preserve the original prompt policy.

## Build

Windows: VS 2022 C++/CLI tools, Framework 4.8 targeting pack and .NET SDK:

```powershell
.\build\Build-Windows.ps1
```

This restores the remaining WinForms `packages.config` projects and the core
PackageReferences. It builds the full Windows solution, including GmshCommon,
GmshCaller and the console launcher. The process-only SDK path override handles
SDK 10 discovery with VS 2022; no machine settings are changed.

Linux or Windows, .NET 8 SDK or newer:

```sh
dotnet build PrePoMax.Core.sln -c Release -p:CoreTargetFrameworks=net8.0 -m:1
dotnet run --project PrePoMax.Cli -c Release --no-build -- --help
```

On a machine without the .NET 8 runtime, publish a self-contained CLI (NuGet
restores the runtime locally; no runtime installation is required):

```sh
dotnet publish PrePoMax.Cli -c Release -p:CoreTargetFrameworks=net8.0 -r win-x64 --self-contained true -o cli
```

Use `linux-x64` for Linux. Do not copy a Windows publish directory to Linux.
Only the net8 slice is restored on Linux, so C++/CLI and WinForms are not required.
NCalc stays at 1.3.8 through AssetTargetFallback; its NU1701 warning is expected.
The final .NET 10 target and serializer replacement remain separate work.

## CLI

```sh
prepomax-cli import model.inp export-inp written.inp save model.pmx
prepomax-cli import model.pmx export-inp reloaded.inp
prepomax-cli export-inp model.pmx written.inp
prepomax-cli save model.inp model.pmx
prepomax-cli run written.inp --solver /usr/bin/ccx --cpus 1 --timeout 600
prepomax-cli results written.frd --summary
```

Errors exit 1; failed/timed-out/cancelled solver jobs exit 2. Solver execution
uses `CaeJob.AnalysisJob`, its output parser and job status. Output files go
beside the input deck. CPU count defaults to one and is limited to eight.

## PMX scope and serialization

L1a supports model data from current 3.0.0 Windows PMX files: mesh, materials,
sets, layups, steps and keywords. Its save operation writes the same PMX header,
Deflate block, BinaryFormatter object dump and explicit mesh payload as Windows,
with a minimal Controller record and empty result collection. Windows can open
these files. CLI saves omit UI history, annotations, jobs, side panels and results;
they are **model-only documents**, as specified for L1a. Full shared Controller
and history support belongs to L1b. Earlier PMX versions should first be saved
by Windows 3.0.0. The CLI prints this boundary when loading PMX.

BinaryFormatter is deliberately retained. The CLI runtimeconfig explicitly sets
`System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=true`
through `EnableUnsafeBinaryFormatterSerialization`. Other net8 hosts must set
that property themselves. Open trusted PMX files only. This is not a serializer
replacement or a safe reader for untrusted files.

`LegacyFormatter` writes Framework-compatible UnitySerializationHolder records
for System.Type and resolves them on net8, preserving all three model Type fields.
The binder handles BCL assembly identities across runtimes. The CLI maps UI-only
objects and legacy delegate/member holders to inert records while extracting the
model. These projections cannot be serialized or replayed.

The cloud probe's 18-constant DCB UMAT predates M1 validation. The CLI explicitly
preserves unsupported UMAT/DEPVAR pairs as raw keywords for conversion, reporting
that they are unvalidated. This opt-in import overload does not change the strict
Windows default or establish solver compatibility. Current 26-constant FATCZM
materials remain typed and validated.

## Platform services and native libraries

`ChildProcessJob.Start` preserves Win32 job-object tracking on Windows. On Linux
it launches a CLI guardian that arms `prctl(PR_SET_PDEATHSIG, SIGTERM)`, checks the
parent PID for the setup race, starts the solver through `/usr/bin/setsid`, and
kills the process tree/group on parent death. The host must configure
`LinuxHostAssembly` and dispatch `--process-host` to `LinuxProcessHost.Run`.
All production CaeJob and external core-mesher launches use this entry point.
The Linux guardian requires Linux and util-linux `setsid`; it is not a macOS
process supervisor. Linux execution still needs acceptance on a host with SDK 8.

net8 lib3mf/lpsolve imports resolve beside the application, in its `lib` directory,
or through the platform loader (`lib3mf.so[.2]`, `liblpsolve55.so[.0]`, or macOS
dylibs). Native dependencies are not bundled by the core. No native operation is
silently simulated. net48 keeps its existing DLL loading behavior. The net8 Gmsh
stub always refuses native operations; real meshing remains L1g. The VTK-fed 3MF
visual exporter stays net48-only until the UI DTO split.

## Runtime checks

```sh
dotnet publish tests/L1a/RuntimeChecks.csproj -c Release -p:CoreTargetFrameworks=net8.0 -r win-x64 --self-contained true -o checks
checks/RuntimeChecks.exe
# After supplying lib3mf and lpsolve beside the check executable:
checks/RuntimeChecks.exe --native
```

`tests/L1a/verify_cli.py` runs the two Task A fixtures, PMX reloads and an optional
net48-written PMX and real ccx solve. `tests/L1a/verify_processes.py` uses explicitly
labelled process fixtures to check Windows net8 failure, timeout, tree termination
and abrupt CLI death; its results are not FEM evidence.
