# L2a Linux viewer prototype

`native/vtkmax` is an independent BSD-3 C++ wrapper around VTK 9.1. `Vtkmax.Interop`
is the net8.0 P/Invoke binding. `PrePoMax.Viewer` adapts the existing L1a FeModel;
`PrePoMax.Avalonia` is an Avalonia 11.3.22 shell running on **.NET 10**. The shared
core and binding remain net8.0-compatible. This is the first half of L2, not the
complete 174-member IViewport3D implementation or a replacement application.

## Build and run

On Ubuntu 24.04, install `build-essential cmake ninja-build libvtk9-dev xvfb
dotnet-sdk-8.0 dotnet-sdk-10.0`. GUI operation needs X11/GLX/Mesa and Avalonia's
X11/font libraries; the acceptance host also installed `xdotool x11-utils
imagemagick` for external input and screenshots. No Windows install is needed.

```bash
bash build/Build-Linux-Viewer.sh "$HOME/ppm-l2a"
"$HOME/ppm-l2a/app/run.sh" /path/M2a_golden.inp
export LD_LIBRARY_PATH="$HOME/ppm-l2a/native"
xvfb-run -a dotnet tests/L2a/bin/Release/net8.0/ViewerChecks.dll /tmp/viewer-evidence \
  /path/M2a_golden.inp /path/dcb_fatczm_static.inp
python3 tests/L2a/wslg_checks.py \
  PrePoMax.Avalonia/bin/Release/net10.0/PrePoMax.Avalonia.dll \
  /path/M2a_golden.inp /tmp/wslg-evidence "$HOME/ppm-l2a/native"
```

Run Linux and Windows builds sequentially: their NuGet `obj` directories are shared
when the checkout lives on `/mnt/c`. The Windows regression entry point is
`tests/L2a/windows_regression.py`; it builds with the inherited Build-Windows.ps1,
assembles a separate runtime under logs, and runs the existing M1, M2a and L1a CLI
checks. All Windows children are hidden.
`tests/L2a/windows_extended.py` additionally reruns L1a clone/process checks and the
owner's existing FATCZM 300-jump fixture under logs. It requires the L1a/M1 handoff
files already present on the acceptance machine.

## C ABI and inventory mapping

The public contract is `native/vtkmax/vtkmax.h`: ABI version 1, opaque owned view,
fixed 64-bit application IDs, 32-bit counts/types, UTF-8 paths/titles, copied input
arrays, thread-local errors and explicit destruction. All calls, including dispose,
belong to the creating UI thread. Managed calls enforce this. Pick results are
56 bytes. Calls cannot validate a C caller's actual allocation lengths; callers
must provide the documented lengths. Native exceptions are caught at status-return
boundaries; VTK/OpenGL fatal errors (such as no X server) are not recoverable C++
exceptions.

Selection followed `linux/survey_B/vtk_usage.json` and `vtk_class_methods.txt`:

| Survey usage | vtkmax entry points / implementation |
|---|---|
| vtkRenderWindow.SetParentId, Render, GetSize | create, attach_x11, resize, render |
| vtkUnstructuredGrid, vtkPoints, vtkIdList | load_grid, atomic grid replacement |
| vtkDataSetMapper, vtkProperty | display surface / surface+edges / wireframe |
| vtkLookupTable, vtkScalarBarActor | scalars on nodes or cells; scalar bar |
| vtkCellPicker, renderer WorldToDisplay, vtkCell | pick original node/element ID and local face; project |
| vtkExtractCells, selection actors | highlight element ID set or node ID set; clear with empty set |
| vtkCamera, ResetCamera, Azimuth, Elevation | fit, rotate, zoom, seven standard views |
| vtkWindowToImageFilter, vtkPNGWriter | write_png at current physical pixel size |
| vtkRenderWindowInteractor / trackball style | nonblocking process_events, last_pick sequence |

Supported VTK cell codes: triangle 5, quad 9, tet 10, hex 12, wedge 13, quadratic
triangle 22, quad 23, tet 24, hex 25, wedge 26. Thus C3D8/C3D20/C3D6/C3D4 and
linear/quadratic shells are supported; FeElement.GetVtkNodeIds supplies VTK ordering.
Pyramids, beams, mass elements and unsupported codes fail explicitly. The adapter
uses whole arrays, not a managed/native call per point or element.

Picking uses bottom-left physical pixel coordinates. `element_id` is the original
application ID. `face_id` is **zero-based VTK local volume face** (shells return 0),
not a CalculiX S-number, globally unique face ID or the old PrePoMax face encoding.
Use `(element_id, face_id)` as the face identity and translate via connectivity
when integrating with the future ViewContracts surface. The nearest node within
10 pixels of the picked cell is returned, with front depth breaking projected
ties; otherwise node_id is -1. Miss IDs are all -1. Highlight actors never intercept
picks. Input IDs and counts are validated before replacing the displayed grid.

## Model semantics

Open imports INP through the L1a parser's new `FeModel.ImportModelFromInpFileForViewing`
entry point, retaining unsupported legacy USER MATERIAL cards as raw, unvalidated
text in ViewerModel.RawUserKeywords. It does not solve them. This explicit read-only
mode skips positional user-keyword indexing, which builds the export tree using
legacy serialization clones. Its FeModel is for display, not lossless re-export.
The original editing/CLI imports still index and preserve keywords as before.
The model tree lists actual FeModel parts, materials, sections and composite layups.
INP has no typed CompositeLayup keyword: the M2a coupon's eight `LU_*` element sets
provide the display plies. A FeModel supplied directly uses CompositeLayupExpansion.
If no ply metadata exist, structured centroid layers along the least varying
nonconstant axis provide **display-only** groups (Y for the legacy DCB). This is a
visual heuristic, not mesh alignment validation or a material/layup reconstruction.
The source is shown in the status bar and recorded in offscreen receipts.

The .NET 10 INP path needed two import-time part clones to stop using BinaryFormatter:
BasePart now uses the existing BoundingBox copy constructor and an explicit nested
array copy of PartMassProperties. Serialized identities/fields are unchanged.
Full PMX and Controller serialization replacement remain separate work. This shell
opens INP only and does not offer PMX save/load or full editing. Viewer tests compare
its mesh/parts/materials/sections against the existing editable import. BoundingBox.Tag's
legacy arbitrary-object clone remains outside this INP path.

## Embedding: X11 now, Wayland later

NativeControlHost supplies an X11 parent. vtkXOpenGLRenderWindow.SetParentId creates
its own child with the proper GLX visual/context, and the host returns that child as
an XID handle. vtkmax owns the child/display/context; Avalonia owns the parent and
layout. Host destruction stops input polling before releasing vtkmax. All VTK calls
run on the Avalonia UI thread. A 20 ms dispatcher timer pumps VTK X11 events without
calling its blocking Start loop. VTK's own trackball handles rotation/pan/wheel;
a small style subclass records click picks, which the managed host polls by sequence.
Host dimensions convert logical Avalonia units with RenderScaling into pixels.

X11 native children have airspace limits: Avalonia overlays/transparency/transforms
are not suitable over the view. Menus are popup windows; toolbar/status/tree stay
outside its rectangle. Prototype acceptance covers WSLg XWayland at scale 1;
fractional/mixed DPI and popup occlusion need later coverage.

WSLg currently provides XWayland, so this prototype uses DISPLAY and X11 even when
WAYLAND_DISPLAY exists. Direct Wayland native surfaces are not implemented.
Keep XWayland as the initial desktop path. A future Wayland path should compare
Avalonia OpenGlControlBase + vtkGenericOpenGLRenderWindow, sharing the host's current
context and forwarding input, or render through a texture/compose surface. That
requires explicit context ownership, framebuffer setup, synchronization and DPI
tests; simply passing a wl_surface as an XID cannot work. The generic OpenGL option
is a design alternative here, not a tested second backend.

References: [Avalonia native interop](https://docs.avaloniaui.net/docs/app-development/native-interop),
[VTK X11 render window](https://vtk.org/doc/nightly/html/classvtkXOpenGLRenderWindow.html).

## Native Linux packaging

The prototype publishes a self-contained linux-x64 .NET 10 app with libvtkmax.so
beside its executable. It runs natively on Linux; WSL is only the development and
acceptance environment. The current runtime depends on Ubuntu's VTK 9.1 shared
libraries (package `libvtk9.1t64`, plus transitive GLX/font/image libraries).
`ldd libvtkmax.so` lists the exact runtime dependencies. The build does not install
anything on Windows or require Wine, WSL or a VM at runtime.

For a distribution-specific package, declare distro runtime dependencies and ship
the wrapper/header/license; CMake install uses lib/ and include/ and `$ORIGIN` rpath.
For a portable official release, pin and build VTK 9.x with only the CMake-listed
modules, disable Python/Java/Qt/wrapping/tests/examples, bundle its recursive shared
library dependency closure and licenses in a private lib directory, and set each
bundled library's RUNPATH to `$ORIGIN`. Keep driver-facing GL/EGL/X11 system
libraries external. Add a launcher or layout using `$ORIGIN/lib`, SBOM and clean
Linux-machine install tests. Do not label the present Ubuntu-linked archive a
portable VTK bundle; a note and a distro-linked runnable prototype are delivered.

The vtkmax source has its own BSD-3 license. The shell/core remain under the
repository license; VTK/Avalonia/transitive redistribution notices must accompany
an official binary release. Windows still uses the existing ActiViz renderer.
