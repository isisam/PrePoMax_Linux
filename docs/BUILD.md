# Source build / 原始碼建置

Windows source requires the upstream Visual Studio/.NET Framework 4.8 development environment and ActiViz 5.8 x64 references. The actual shared-core Windows driver is `build/Build-Windows.ps1` (Visual Studio MSBuild, x64 Release, net48). It detects installed tooling; repository preparation does not install or invoke it. See UPSTREAM_BUILD.md for original VTK reference setup. Runtime/third-party source and licensing information remain under Licenses/ and ThirdParty/.

Build from this repository root: `powershell -NoProfile -File build/Build-Windows.ps1 -Configuration Release`. Resolve the documented upstream dependencies first. A Windows build is not a complete student release package: attach the matching solver and runtimes with notices and corresponding source, then run the documented examples. Local packaging/evidence scripts with private host paths were intentionally excluded; historical build reports are not public documentation.

The Linux source port is on linux-preview. Its native VTK adapter and Avalonia shell remain preview code, separate from the Windows binaries. Follow that branch's native/ and portable project instructions, using a fresh build directory and existing prerequisites. No cross-platform build or UI acceptance is claimed by this preparation.

## Antlr3 runtime dependency

The original RCM tree carried Antlr3.Runtime assembly version 3.1.3.22795 as a binary with third-party developer paths. It was excluded from our prepared source history. Before building, obtain a trusted RC1f or RC1g5 release ZIP and run `powershell -NoProfile -File build/Restore-ReleaseDependency.ps1 -ReleaseZip /path/to/release.zip`. This helper checks the extracted DLL SHA-256 `2038b9d2998dd52db9fcd3988abfee3de0e5977b7dcbab6e9223a71ea792c216` before writing it to the ignored ThirdParty/NCalc.RCM path. It does not install packages. The release runtime assembly identity (unsigned, PublicKeyToken=null) is retained locally; no substitute NuGet version is silently selected. This restoration/build was not executed during preparation.

Metadata verification during preparation: source and release runtime both report `Antlr3.Runtime, Version=3.1.3.22795, Culture=neutral, PublicKeyToken=null`. The historical source README called it 3.1.2; the measured assembly identity is recorded here. The release ZIP runtime differs by 13 bytes from the original source-tree DLL, so the helper pins the released DLL hash instead of assuming byte identity. No runtime substitution or restore was executed in the source checkout.
