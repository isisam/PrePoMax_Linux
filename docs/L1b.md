# L1b: Controller core and full PMX

`PrePoMax.Core` and `PrePoMax.ViewContracts` target `net48;net8.0`. The existing Windows application remains net48. All moved implementation types keep their namespaces and class names.

## Boundaries

- Core owns Controller, Commands, Annotations, Settings, Python execution/journals/gateways, MCP/RPC, command-line reporting, collections and mesher runners.
- `IMainView` contains the host calls actually used by Controller, Python gateways and RPC. FrmMain implements it. Its additional gateway/RPC calls and overloads explain the difference from the plan's approximate 169-member inventory.
- `IViewport3D` exposes the portable vtkControl surface, including its two selection/rotation events. vtkControl implements it. Native actor caching crosses the boundary through `IViewportActor`; native camera/transform overloads have portable camera/matrix adapters.
- ViewContracts owns the VTK exchange objects/enums and the entire saved side-panel state graph. Color/Point/Rectangle are portable drawing primitives. Native VTK objects and WinForms messages stay in Windows.
- Windows adapters own window geometry, interactive prompts, property descriptors, the Python stop window and the native 3MF visualization export. The stop watchdog lives under Forms and retains its original namespace.
- NullMainView/NullViewport3D support the non-rendering host. Rendering is inert; native triangulation/interpolation/cropping/smoothing/image operations refuse explicitly where a backend is required. This is not the Linux GUI or mesher implementation.

## Serialization

All production dumps/clones use `LegacyFormatter.Create()` and `PmxSerializationBinder`. Core types write the legacy `PrePoMax` identity; saved panel/viewport data write `UserControls` or `vtkControl`. The resolver understands old names, moved assembly aliases, nested generic arguments and array types, while retaining L1a's Framework-compatible System.Type holder.

Moved serializable declarations carry TypeForwardedFrom metadata. The Windows assemblies also forward their moved public types, preserving existing type lookup and the M1 raw-BinaryFormatter settings clone. Array-field metadata can still contain a new assembly alias: use the shared binder. An unmodified upstream application's ability to read every new full file is not established by these tests.

The old CommandsCollection undo/redo callback was serialized into PMX. New saves exclude that runtime event; the legacy callback/method records are consumed when reading old files, and Controller reconnects callbacks after opening. Model/history field names remain unchanged.

The task explicitly targets .NET 8. BinaryFormatter replacement and .NET 10 remain separate work; this does not claim .NET 10 compatibility.

## Build

From this checkout, using the existing Windows prerequisites:

```powershell
.\build\Build-Windows.ps1
dotnet build PrePoMax.Core.sln -c Release -p:CoreTargetFrameworks=net8.0 -m:1
dotnet publish PrePoMax.Headless -f net8.0 -c Release -p:CoreTargetFrameworks=net8.0 -r linux-x64 --self-contained true -o ..\logs\L1b_headless_linux -m:1
```

Do Windows/net8 builds sequentially: they share NuGet asset files. Set `DOTNET_PROCESSOR_COUNT=4` and retain the single MSBuild worker on this machine.

## Full headless host

```text
prepomax-headless inspect full.pmx
prepomax-headless export full.pmx output.inp
prepomax-headless replay full.pmx output.inp input-directory
prepomax-headless roundtrip full.pmx output.pmx
prepomax-headless contracts
prepomax-headless diagnose full.pmx
```

The host loads the real Controller/history, not L1a's model projection. Replay fails on collected command exceptions. The input directory remaps existing import/dependent files by basename, including Windows backslash paths. External inputs and native backends must actually be available.

Replay explicitly preserves the persisted model Hash through `RegenerateHistoryCommands(..., preserveModelIdentity: true)`. Ordinary Windows regeneration retains its previous default (`false`). The writer uses Windows CRLF on Linux. For exact header comparisons, use the same culture/calendar date: the existing heading still contains DateTime.Now. Acceptance runs WSL with `TZ=Asia/Taipei`; it does not normalize exported files after writing.

Linux Python defaults to the existing Ubuntu CPython 3.12 installation. `PREPOMAX_PYTHON_DLL` and `PREPOMAX_PYTHON_HOME` override its library/prefix. No Python package installation is performed by the host.

## Reproduce acceptance

Assemble an isolated release runtime from the official payload plus this Windows build, with the supplied ccx_fat, as in L1a. Do not overlay the owner's existing run folders.

```powershell
python tests/L1b/verify_windows.py --exe C:\path\PrePoMax.com --work C:\path\regressions
python tests/L1b/verify_full_pmx.py --exe C:\path\PrePoMax.com --headless-linux C:\path\linux-publish\prepomax-headless --work C:\path\full-acceptance
```

The full fixture is saved by FrmMain's Windows application. It contains Clear, model properties, an external mesh import, a material, a composite layup and an embedded Python command that adds a parameter. The test checks real Linux replay, a Linux full save reopened by Windows, all six history records and byte-identical exports. `contracts` additionally checks all 276 serializable Command-derived type identities, generic/array aliases, moved Type records and absence of direct UI references.

Existing `tests/L1a/verify_cli.py`, `verify_processes.py` and RuntimeChecks remain available. M1/M2a scientific assertions are unchanged. M2b's runner header and reflective command assembly lookup were adjusted for the split.

`build/ContractExtractor` is the development helper for declaration extraction, portable stub scaffolding and Windows public type-forward generation. Its generated interfaces still require the explicit native-signature adapters described above.
