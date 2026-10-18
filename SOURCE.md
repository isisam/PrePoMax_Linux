# Licences and corresponding source

PrePoMax-RCM is distributed under GNU GPL version 3; see LICENSE and the retained Licenses folder. The full corresponding tracked source and history for rcm/rc1 are provided in PrePoMax-RCM_rc1f_source.bundle beside the release zip. The source bundle is part of this release delivery; keep it with the zip when redistributing.

To recover it with Git: `git clone -b rcm/rc1 PrePoMax-RCM_rc1f_source.bundle source`.
Use VS 2022 Build Tools with the .NET Framework 4.8 developer tools and the existing documented build/Build-Windows.ps1 recipe. Runtime assembly and diagnostic-path redaction are documented by tools/rc1/Package.py. Third-party source locations and licences remain in Licenses/README.md. NCalc 1.3.8 source used by RC1 is in ThirdParty/NCalc.RCM.

CalculiX 2.21/FATCZM use GPL version 2. The complete v1.1b corresponding-source archive, including patched solver, dependencies and rebuild recipes, is supplied in Source/ccx_fat/ccx_fat_win64_v1.1b_source.zip. The corresponding licence and runtime exception texts are in Licenses/ccx_fat. v1.1b is v1.1 rebuilt from the same source without build-machine paths.

Third-party components retain their own terms. Microsoft runtime files are redistributable runtime components, not GPL-covered PrePoMax source. The RC package supplies them locally and performs no installation or registry modification.

In this RC package, the solver banner's standalone source\ and LICENSES\ locations correspond to Source\ccx_fat\ and Licenses\ccx_fat\LGPL-2.1_libquadmath.txt, respectively.
