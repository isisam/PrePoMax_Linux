# Source build / 原始碼建置

Windows source requires the upstream Visual Studio/.NET Framework 4.8 development environment and ActiViz 5.8 x64 references. The actual shared-core Windows driver is `build/Build-Windows.ps1` (Visual Studio MSBuild, x64 Release, net48). It detects installed tooling; repository preparation does not install or invoke it. See UPSTREAM_BUILD.md for original VTK reference setup. Runtime/third-party source and licensing information remain under Licenses/ and ThirdParty/.

Build from this repository root: `powershell -NoProfile -File build/Build-Windows.ps1 -Configuration Release`. Resolve the documented upstream dependencies first. A Windows build is not a complete student release package: attach the matching solver and runtimes with notices and corresponding source, then run the documented examples. Local packaging/evidence scripts with private host paths were intentionally excluded; historical build reports are not public documentation.

The Linux source port is on linux-preview. Its native VTK adapter and Avalonia shell remain preview code, separate from the Windows binaries. Follow that branch's native/ and portable project instructions, using a fresh build directory and existing prerequisites. No cross-platform build or UI acceptance is claimed by this preparation.
