# L3b shutdown investigation

Research assistant: Lota. This is a graphics teardown diagnosis and a WSL mitigation, not native GPU acceptance.

## Reproduction and evidence

The initial L3a failure remains at `../logs/L3a/initial-clean/evidence/PPM-L3a-U22/deb/gui.log`: a real segmentation fault after the DCB/FRD ready message. The original L2c package harness unconditionally killed the process group and ignored `wait` failures. L3a corrected that harness; this investigation retains strict exit-code and forced-kill checks.

L3b reproduced a real exit **-11** after the tar model-tree UI run in the new Debian 12 distro, and again with the unchanged L3a tar in the new Ubuntu 22.04 distro. Logs and original failed package bytes remain under `logs/L3b/`; successful retries do not replace them.

Two useful captured stacks are:

* `clean/PPM-L3b-U22/l3a-gdb-reference/11/gdb.log`: thread 16 receives SIGSEGV inside `swrast_dri.so`, called by `libSkiaSharp.so`; simultaneously thread 1 is inside the NVIDIA WSL user-mode driver during `exit()` / loader finalization. Gdb eventually reports exit 0, so its exit code alone must not erase the captured fault.
* `shutdown/debian-tree-gdb/5/gui.log`: thread 16 faults in `/usr/lib/wsl/lib/libd3d12core.so`, called through `libd3d12.so` and Mesa `swrast_dri.so`; thread 1 is concurrently in `dlclose()` called by `libd3d12.so` during `exit()`. Continuing reaches a second SIGSEGV and abnormal termination.

This locates the observed failure in **graphics-library unload racing an active Skia/Mesa/D3D12 rendering worker**. The stacks do not implicate model serialization, selection membership or a managed form exception. The proprietary driver's internal source line and responsibility between framework/driver teardown are not established; no claim is made that every historical crash has the same cause. Some gdb runs fail with `Couldn't read debug register: No such process` under WSL; those runs are inconclusive, not passes. See the summaries for every run, including faults and debugger failures.

The matching Avalonia 11.3.22 sources show that [desktop lifetime shutdown](https://github.com/AvaloniaUI/Avalonia/blob/11.3.22/src/Avalonia.Controls/ApplicationLifetimes/ClassicDesktopStyleApplicationLifetime.cs) cancels the dispatcher, while [OpenGL control cleanup](https://github.com/AvaloniaUI/Avalonia/blob/11.3.22/src/Avalonia.OpenGL/Controls/OpenGlControlBase.cs) invokes asynchronous resource disposal. This supports investigation of the teardown boundary; it does not by itself prove an Avalonia defect.

## Mitigation

Before graphical libraries initialize, `WslRenderer.Configure()` detects a Linux kernel release containing `microsoft`. With no explicit driver settings, it selects Mesa llvmpipe using `LIBGL_ALWAYS_SOFTWARE=true` and `GALLIUM_DRIVER=llvmpipe`, the combination documented by [Mesa](https://docs.mesa3d.org/envvars.html#gallium-driver). The console reports the choice. `PPM_WSL_GPU=1` opts into the prior driver-default path; explicit Mesa/LibGL environment settings remain authoritative. Native Linux is unchanged.

Both libc `setenv` and the managed environment cache are updated. The [.NET 10 Unix implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Environment.Variables.Unix.cs) maintains a managed dictionary for its setter; updating only that dictionary would not configure Mesa's native `getenv` consumers. This process-only setting installs nothing and changes no host driver.

The unchanged Debian package with inherited software-renderer variables completed 16 consecutive strict close probes. Final package and stress results are listed in L3b_REPORT.md; the mitigation is accepted only for that tested WSL software path. A complete framework/driver teardown fix and native GPU/VM acceptance remain pending.

The separate DBus diagnostic about unversioned `libX11` occurs at startup in both passing and failing runs. Its exception is caught before the model opens. It has not been demonstrated to cause this shutdown fault and is not represented as fixed.

## Reproduce

`build/Check-L3b-Shutdown.sh APP OUTPUT_LABEL COUNT plain` runs without gdb and records actual exit status, forced termination and mapped graphics libraries. Omitting `plain` records gdb stops and full native stacks. `build/Check-L3b-Tree-Gdb.sh` drives the same real tree menu and pointer operations under gdb. Diagnostic runs intentionally preserve failures; use the JSON records, not just the diagnostic script's exit status, to assess them.
