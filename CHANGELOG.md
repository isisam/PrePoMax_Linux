# PrePoMax-RCM 0.1.0-rc1f (2026-10-18)

Research assistant: Lota.

- Replace ccx_fat with v1.1b: same source and numerical flags, rebuilt without build-machine paths; single-thread results unchanged.
- Supply ccx_fat_win64_v1.1b_source.zip and matching licence/build notes.
- Correct both quick-start guide version labels to RC1f / 0.1.0-rc1f; no other guide content changes.

# RC1e runtime language switching

Based on RC1d (`f61cede4`) on `rcm/rc1`.

- Result-type and deformation-scale toolbar choices retain enum identities;
  display text is no longer parsed to determine the selected result operation.
- Color-by choices retain the corresponding menu item identity, including None.
- Runtime language changes refresh the three translated lists on the UI thread,
  preserve selections, field variable, scale factor and complex angle, and avoid
  intermediate selection events/redraws. The language subscription is removed
  when the main form is disposed.
- Other result-toolbar lists contain result names, field names, step/increment
  identifiers, or symbol/model names and retain those data identities. Settings
  pages use dynamic property descriptors with enum/bool identities and refresh
  their metadata when the language changes.

Acceptance: `tests/RC1e/run.py` runs the product's Settings Apply path against
DCB, ENF and Layup FRDs on a private, never-switched Windows desktop. It checks
en -> zh-TW -> en selection identity, live stored combo text, non-default factor
and angle retention, field/legend extrema and rendered contour geometry against
an axes/legend-only control image. Settings choices are checked on the same 13
page view instances across both switches. Existing M1, M2a, M2b/GUI3, L1a, L1b,
L10N and FATCZM 300-jump gates remain required.

The release retains RC1d first-run language and RC1c work-directory defaults.
No solver, numerical model or example input changes are included.


# PrePoMax-RCM 0.1.0-rc1d (2026-10-18)

Research assistant: Lota.

- First launch and Reset select 繁體中文 for Windows UI culture zh-TW, zh-HK or zh-Hant (including zh-Hant subtags); other UI cultures select English. Older settings without the optional language field use this fallback too.
- Explicitly saved English or zh-TW always takes precedence. Tools > Settings > General > Language continues to switch both ways and persist the choice.
- Preserve Windows UI culture during startup instead of replacing it with invariant culture. Numeric culture remains invariant for solver and API data.
- Retain RC1c's usable default work directory, old-null migration, custom paths, PMX-folder preference and command-line override precedence.
- Update both ten-page quick starts with Chinese-Windows startup behavior and instructions for switching to English; retain Lota authorship.
- Add real-core language unit checks and a hidden fresh-start form-text probe. Regression receipts and final archive fresh-copy example results are recorded in RC1d_REPORT.md.
- Retain RC1b/RC1c archives. Native solvers, Python, examples, translation catalogue and licence notices remain unchanged from RC1c.


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
