# PrePoMax_Linux — unofficial PrePoMax fork

**Windows RC1g5 development feature lineage.** Main focus: Linux/Avalonia port, L3 checkpoint **0.4.0**, with shared finite-element core and composite layup / 3D fibre-display work. Windows feature lines remain available on windows-rc (RC1f) and develop (RC1g5). Linux 0.4.0 does not include every Windows RC1c–RC1f enhancement; follow the documented checkpoint boundaries.

PrePoMax was created by **Matej Borovinšek** and upstream contributors at the University of Maribor. This is an **unofficial fork**, not an endorsed upstream release. Upstream: https://github.com/ISCPC/PrePoMax . GPL-3.0; original LICENSE, Licenses/ and source notices are preserved.

- [下載與安裝](docs/下載與安裝.md) · [使用教學](docs/使用教學.md) · [FAQ](docs/FAQ.md) · [回報問題](docs/回報問題.md)
- [Install](docs/INSTALL.en.md) · [Tutorial](docs/TUTORIAL.en.md) · [FAQ](docs/FAQ.en.md) · [Issues](docs/ISSUES.en.md)
- [Public Gmsh boundary](docs/PUBLIC_GMSH.md) · [Build](docs/BUILD_PUBLIC.md) · [Source lineage](SOURCE_HISTORY.md)

The Linux checkpoint supports existing-mesh models, property forms, selection-created sets/surfaces, undo/redo and a read-only keyword viewer. Linux Gmsh operations use an explicit compile stub and throw NotSupportedException. Native Linux/VM/GPU release acceptance, full meshing parity and editable raw keywords remain open. Windows RCM layup editing and 3D fibre display share source in this fork; branch names describe feature lineage, not a new binary validation.

The private Gmsh fork/SDK is excluded from every published history object. Original Windows application ZIPs are not release assets here. See PUBLIC_GMSH.md before attempting a Windows full-solution build.
