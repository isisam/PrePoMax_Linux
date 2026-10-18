# PrePoMax-RCM 0.1.0-rc1c (2026-10-18)

- Fix first-run and Reset solver work directory: create %USERPROFILE%/Documents/PrePoMax-RCM/Work instead of storing null.
- Migrate unset work directories from old settings.bin. Preserve explicit paths (including missing custom paths), matrix solver choices and the existing Use .pmx folder as work directory preference (default remains off). The PMX-folder opt-in and command-line overrides retain precedence.
- Rebuild PrePoMax.Core.dll from rcm/rc1. Keep RC1b solver, Python, native runtimes, examples and licence notices unchanged.
- Document the default work directory on pages 2 and 3 of both quick starts. No personal settings.bin is distributed.
- Fresh-copy hidden startup/settings probe and DCB/ENF/Layup numerical smoke receipts are recorded separately in RC1c_REPORT.md. The composite-layup appearance check has incomplete items due to computer-use child-window focus and middle-drag limitations; it is not a visual acceptance PASS.

# PrePoMax-RCM 0.1.0-rc1b (2026-10-18)

- Corrected both quick starts, p. 5: opening pre-cracks require constant 5 = 0.999999 AND constant 19 = 0; constant 19 alone leaves the interface intact. Added one-element D = 1 / zero opening reaction verification.
- Audited all guide constants against the material table and shipped UI metadata. Corrected the exact English Fatigue CZM (FATCZM) control, read-only FATCZM preset explanation, English True values, DISP field wording and Traditional Chinese Steps label.
- Added version-matched notices and a 40-binary Python dependency inventory under Licenses/supplementary/Python; linked both licence indexes.
- Statically verified all 49 VC90 dependency manifests, both exact 9.0.21022.8 private assembly identities, x64 CRT file version 9.0.30729.6161 and all 3586 imported symbols. No CRT binary or manifest change was needed.
- Added the Microsoft Visual C++ 2008 SP1 Redistributable (x64) fallback and explicit clean-PC acceptance item on p. 1 of both guides.
- Rebuilt both ten-page PDFs, renewed SHA256SUMS and packaged RC1b separately. RC1 application/solver binaries and numerical examples are unchanged.

Known item: clean Windows 10/11 startup, viewport rendering, meshing and example execution without preinstalled VC redistributables still require confirmation on a clean PC. Static compatibility and a fresh-copy smoke test do not replace that acceptance.

# PrePoMax-RCM 0.1.0-rc1

Based on upstream PrePoMax 3.0.0 RC2, commit 912fb3d.

- Native CalculiX 2.21 + FATCZM v1.1 solver selected automatically from Solver/ccx_fat.exe; default Spooles; explicit job/solver choices preserved.
- Typed user materials, 26-constant FATCZM preset, validation, DEPVAR and CalculiX import/export.
- Flat solid composite layups: bottom-to-top ply editor, symmetric stacks, layer assignment, orientation export, persistence, selection, command and Python journal support.
- Corrected row highlighting, edit validation refresh, Cancel selection cleanup, remove-command persistence, localized Add action and selected material names.
- Selectable English/Traditional Chinese. First launch/reset defaults to English, preserving upstream behavior; saved explicit choices are retained.
- Retained Windows controller/core split and serialization compatibility; no Linux GUI, mesher or renderer branch included.
- Rebuilt NCalc 1.3.8 from its existing source, changing only generated diagnostic grammar filenames; consumers rebuilt against the unsigned assembly and separate original ANTLR runtime.
- English Settings category navigation now uses the available column width so category names are readable.
- Student DCB, ENF and [45/90/-45/0]s model snapshots, reference values and two ten-page quick starts.
- App-local VC++ runtimes, private VC90 descriptor, licence/source information and checksums; historical build/profile prefixes in unsigned binary diagnostics are redacted without changing their length.

Known limits: flat solid meshes only; no mesh extrusion, automatic interface insertion or curved layups; FATCZM ignores *ORIENTATION. New typed PMX files are not readable by unmodified older versions. Fatigue demonstrations are not calibrated life predictions.
