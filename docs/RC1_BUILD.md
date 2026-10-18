# Windows RC1 build and redistribution recipe

Research assistant: Lota. Release: PrePoMax-RCM 0.1.0-rc1.

RC1d selects Traditional Chinese on first launch and Reset when Windows UI
culture is zh-TW, zh-HK or zh-Hant (including zh-Hant subtags), and English
otherwise. Existing explicitly saved English/zh-TW choices always win.
Missing language fields in older settings use the same culture fallback.
Numeric culture and the RC1c default work-directory fix are unchanged.

The Windows integration starts at rcm/m2c (413e11f6) and merges rcm/l10n
(84d8eff7), retaining the GUI3 event/serialization fixes and localization.
No l1g/l2 feature branch is merged. The inherited portable core remains part
of the Windows application's implementation.

Build Release x64 with VS 2022 Build Tools, .NET Framework 4.8 development
tools and the required existing dependencies. Use build/Build-Windows.ps1;
MSBuild uses /m:1 /nr:false and CoreTargetFrameworks=net48. Run console
children hidden, use below-normal priority and cap process CPU concurrency.
The split core may also be checked on Windows/net8; do builds sequentially
because the SDK project asset files are shared.

NCalc 1.3.8 is built from ThirdParty/NCalc.RCM, with unchanged evaluation
logic. Generated GrammarFileName diagnostic properties return NCalc.g.
CaeGlobals is rebuilt against this unsigned assembly; the original signed
NuGet DLL is not patched. The separate ANTLR runtime retains its identity.

Assemble the runtime from the upstream Windows payload plus the Release x64
output. Use tools/rc1/Package.py with --runtime, --source, --native and
--output directories. It excludes development caches/settings/history,
preserves the ccx_fat v1.1b executable exactly, keeps upstream Licenses and
copies its corresponding-source archive. It redacts fixed-size historical
diagnostic build/profile prefixes in unsigned native/managed binaries.
Public names, numerical data and file sizes are preserved. Run all runtime
regressions after this transformation.

Copy the redistributable x64 VC14 CRT DLL set from the licensed VS 2022
VC/Redist/MSVC/<version>/x64/Microsoft.VC143.CRT directory into the application,
lib and Python directories. Keep upstream Netgen's local VC100 runtime.
For ActiViz/VTK, retain msvcr90/msvcp90/msvcm90 and add a private assembly
directory Microsoft.VC90.CRT beside the executable and in lib. Its manifest
uses the amd64 Microsoft.VC90.CRT identity version 9.0.21022.8 with public key
token 1fc8b3b9a1e18e3b, matching VTK's embedded dependencies, and lists the
three CRT DLLs. The upstream compatible DLL file versions are 9.0.30729.6161.
An already installed Windows publisher policy may select a newer system
WinSxS CRT. This host behavior does not substitute for testing a clean OS.

CaptureGuide.py is an in-process application script. Launch its application
only through tests/M2bUi/run.py's private_desktop_run helper (CreateDesktop,
CreateProcess lpDesktop, never SwitchDesktop). Screenshots are native control
renders, with the output panel cleared through its UI action and only
relative example information displayed. Native viewport rendering is not
claimed by these GDI control screenshots.

The release contains DCB (60 fatigue jumps), ENF (300 jumps) and the M2a
continuous [45/90/-45/0]s free-edge coupon. MakeExample.py builds portable
snapshots; RunExample.py executes and checks them. Construction history and
workstation job paths are removed from the saved examples. Fatigue steps
remain user keywords to preserve STATIC,DIRECT. The continuous coupon's
60500.75 MPa apparent modulus is distinct from the periodic CLT value
64387.645 MPa. No calibrated life claim is made.

Run M1 (7/7), complete M2a CLT, M2b/editor, GUI3 (22 cases plus four journal
replays), Windows L1a/L1b contracts/roundtrips, L10N forms/contracts, and the
300-jump FATCZM check. Test the final archive by extracting a fresh copy
under a folder containing spaces and Traditional Chinese, opening every
PMX through PrePoMax.com and running the bundled solver. Keep solver logs,
structured application reports, actual values and binary hashes outside
the student package.

RC1e adds `tests/RC1e/run.py --runtime <fresh-runtime> --output <evidence>
--sources <sources.json>`. The JSON maps DCB, ENF and Layup to actual FRD files
from the bundled examples. Run this on the final archive's fresh copy after
the smoke solver runs. The helper uses a private, never-switched desktop and
checks native contour images against empty-scene controls, legend extrema,
enum identity, preserved non-default values and all 13 Settings page choices
through en -> zh-TW -> en. Do not call the toolbar initializers to repair the
test state after language changes.

Remove task-generated history/settings/trace files, test executables,
__pycache__ and pyc files before generating SHA256SUMS. Include VERSION,
CHANGELOG, SOURCE, student documentation and licence notices. Generate the
git bundle for rcm/rc1 beside the zip. Retain original-source attribution;
do not ship internal review/advisor correspondence. Hash the archive and
bundle separately, and close all task-owned processes.
