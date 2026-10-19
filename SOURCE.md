# Licences and corresponding source

PrePoMax-RCM is distributed under GNU GPL version 3; see LICENSE and the retained Licenses folder. The full corresponding tracked source and history for rcm/m2c are provided in PrePoMax-RCM_rc1g5_source.bundle beside the release zip. The source bundle is part of this release delivery; keep it with the zip when redistributing.

To recover it with Git: `git clone -b rcm/m2c PrePoMax-RCM_rc1g5_source.bundle source`.
Use VS 2022 Build Tools with the .NET Framework 4.8 developer tools and the existing documented build/Build-Windows.ps1 recipe. Runtime assembly and diagnostic-path redaction are documented by tools/rc1/Package.py. Third-party source locations and licences remain in Licenses/README.md. NCalc 1.3.8 source used by RC1 is in ThirdParty/NCalc.RCM.

CalculiX 2.21/FATCZM use GPL version 2. The complete v1.1b corresponding-source archive, including patched solver, dependencies and rebuild recipes, is supplied in Source/ccx_fat/ccx_fat_win64_v1.1b_source.zip. The corresponding licence and runtime exception texts are in Licenses/ccx_fat. v1.1b is v1.1 rebuilt from the same source without build-machine paths.

Third-party components retain their own terms. Microsoft runtime files are redistributable runtime components, not GPL-covered PrePoMax source. The RC package supplies them locally and performs no installation or registry modification.

In this RC package, the solver banner's standalone source\ and LICENSES\ locations correspond to Source\ccx_fat\ and Licenses\ccx_fat\LGPL-2.1_libquadmath.txt, respectively.

RC1g2 preview build and acceptance: tools/m2d/run.py, seal.py and verify.py. Build with the upstream post-build deletion event disabled, as recorded in the M2d report.

RC1g4 M2e build/acceptance: tools/m2e/. RC1g3 refreshed PDFs, screenshots and Source/RC1g3_packaging are preserved byte-for-byte. See Documentation/RC1g4_CHANGELOG.md and DPI150_manual_check.md. Native 144-DPI acceptance is open.

RC1g5 M2f build/acceptance: tools/m2f/. Transient fibre glyphs use the main VTK model viewport. Earlier guides, examples, solver and corresponding solver source are preserved. See Documentation/M2f_VIEWPORT.md. Native 144-DPI acceptance remains open.
