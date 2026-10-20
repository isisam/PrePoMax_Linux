# Releases

Use https://github.com/isisam/PrePoMax_Linux/releases . The selected teaching checkpoint is Linux 0.4.0, tag v0.4.0-teaching.4.

The tar.gz and amd64 deb are rebuilt from the cleaned public source with deterministic managed compilation, native file-prefix maps, normalized archive timestamps and a pinned .NET SDK. No research solver is included; configure a separately installed stock CalculiX. Gmsh operations intentionally throw NotSupportedException. Native Linux/VM/GPU acceptance remains pending; WSL checks do not replace it.

PrePoMax-Linux-0.4.0-public-source.tar.gz includes the tagged project source, exact VTK 9.4.2 source archive, licenses and upstream source-access instructions. Preserve it beside redistributed binaries. The release's verification.json and UPSTREAM_PATHS.json record validation and byte-identical upstream binary provenance. Documented upstream compiler paths may remain in unmodified third-party packages; our own build paths must be absent.

Windows RC1f-equivalent binaries are omitted. The original ZIPs contain the excluded modified Gmsh SDK, research solver/source and legacy model history paths. The cleaned Windows branches do not yet have an integrated and validated public backend/stub. They remain source feature lines, not release binaries. Do not upload original RC1f/RC1g5 ZIPs or the old full Linux source archive. Superseded teaching assets remain private audit evidence.
