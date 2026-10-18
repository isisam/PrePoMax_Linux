# VC90 static check and clean-PC acceptance

The application EXE/COM manifests do not directly request VC90. Forty-nine native ActiViz/VTK/helper manifests (resource ID 2) request Microsoft.VC90.CRT, type win32, version 9.0.21022.8, amd64, publicKeyToken 1fc8b3b9a1e18e3b. Both Microsoft.VC90.CRT/Microsoft.VC90.CRT.manifest and lib/Microsoft.VC90.CRT/Microsoft.VC90.CRT.manifest declare that exact identity and list msvcr90.dll, msvcp90.dll and msvcm90.dll.

The six private CRT DLL copies are x64, file version 9.0.30729.6161; their corresponding root/lib payloads match. File version is PE version metadata; the SxS identity comes from the external assembly manifest. None of these CRT DLLs embeds a conflicting assembly manifest. All 3586 VC90 import references from 55 importers resolve to exports in the bundled DLLs. There is no application binding redirect to a different VC90 version.

Static result: with no VC90 assembly/publisher policy in WinSxS, the requested identity has a matching private assembly at the documented application-directory search location. No manifest version change is needed; replacing its identity with 9.0.30729.6161 alone would no longer match the native manifests. A machine with existing publisher policy may select its system CRT instead.

This is static compatibility evidence. Clean Windows 10/11 startup, viewport rendering, meshing and example execution without an installed VC redistributable remain a known item to confirm. If startup fails on a clean PC because VC90 is unavailable, installing Microsoft Visual C++ 2008 SP1 Redistributable (x64) supplies it. No installation was performed for this release.

References: [Microsoft assembly search sequence](https://learn.microsoft.com/en-us/windows/win32/sbscs/assembly-searching-sequence), [assembly manifests](https://learn.microsoft.com/en-us/windows/win32/sbscs/assembly-manifests).
