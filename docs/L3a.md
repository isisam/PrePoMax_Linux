# L3a shared ViewModels checkpoint

Author: S.Y. Su; Base `rcm/l2c` / `d95d9b7a`; branch `rcm/l3a`.

The 178 `View*.cs` files and 18 supporting files formerly compiled into PrePoMax
now compile once in `PrePoMax.ViewModels` (`net48;net8.0`). The exact source paths
are in `PrePoMax.ViewModels/migration.json`; its public type inventory contains
213 types. Names, namespaces and model fields are retained. Windows public type
forwarders, `TypeForwardedFrom` and PMX binder aliases preserve legacy identities.

The descriptor engine is in a small `PrePoMax.PropertyMetadata` foundation, with
public type forwarding from ViewModels. This extra assembly avoids the cycle
Core -> descriptor -> ViewModels -> Core: Core's pre-existing SearchContactPair
uses the descriptor, while the wrappers consume Core settings and data. The
StandardValueAttribute remains in CaeGlobals on both targets for model enum
annotations. All WinForms descriptor controls, services and paint/dropdown
editors live in UserControls. The foundation has no WinForms/VTK references.

Editor attributes contain string type identities. All eleven editor kinds map
through Avalonia's PropertyEditorMap; this is a metadata table, not a completed
form host. Unknown editors are explicitly Unsupported. The Windows classes still
perform editing. Missing-work-directory confirmation stays a host callback,
registered to the original Windows prompt in Program.Main. File-dialog static
filter state and the image magnification limit retain their existing semantics.

Neutral API adaptations: ViewNodeSet's unused parent parameter is object;
CustomPropertyDescriptor.ValueImage and StateItems store opaque objects, cast to
the existing image/UI item types by the Windows adapter. None changes FE model
fields. Existing third-party plugins must rebuild against the moved assemblies;
this is not a binary compatibility claim for arbitrary plugins.

## Verification

`python -B tests/L3a/regressions.py --suite windows` builds Windows, assembles an
isolated runtime, compares the L2c and current metadata, runs M1/M2a/M2b, then the
existing FATCZM 300-jump regression. `--suite portable` publishes and exercises
the Windows CLI, runtime/process checks and the full Windows/Linux PMX exchange.
All child processes are hidden. Linux source/obj/bin are staged separately on
ext4 under `/root/ppm-l3a`, with nice 19 and at most eight build workers.

`MetadataProbe` compares public fields/properties, categories, descriptions,
read-only/browsable flags, converters, editor resolution and constructors; it also
exercises actual per-instance section region dropdown switching and both typed
26-constant FATCZM presets, including DEPVAR validation. Raw snapshots retain
generic assembly names; compare_metadata.py maps only the intentional ViewModels
assembly move and the documented unused Form -> object constructor argument.

Use `build/Check-L3a-Metadata.sh /root/ppm-l3a` after staging/publishing Linux. It
runs the same dynamic metadata cases on Linux and builds the L1b headless peer.
The unchanged GUI3 test source is checked separately by gui3_prerequisites.py:
its rcm/m2c form constructor and journal-baseline API are not in the requested
L2c baseline. An inability to compile/run those tests is not reported as PASS.

The task's final coverage, regressions, packages and remaining L3b work are in
the external `prepomax/L3a_REPORT.md` and the committed `docs/L3a_REPORT.md`.
Full L3 model editing, selection and export acceptance remain separate from this
shared ViewModels checkpoint.
