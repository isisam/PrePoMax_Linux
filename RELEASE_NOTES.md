# PrePoMax Linux 0.4.0 teaching.5

Author and package maintainer for this fork's additions: **S.Y. Su**.
Original PrePoMax: **Matej Borovinšek** and upstream contributors. Original licenses and credits are retained.

This attribution update rebuilds the Linux teaching tar.gz, amd64 deb and corresponding source with the canonical maintainer, own Avalonia assembly metadata, AppStream metadata and rebuilt tutorial PDF. Existing releases and tags remain intact. Windows ZIP is omitted. No solver is bundled; install stock CalculiX separately. Gmsh meshing is explicitly unsupported.

Known test-count note: the historical `--contract-check` final assertion expects 120 unsupported methods, while the implementation has 112; its exit 1 remains disclosed. The independent audit checks all 112 refusals. Native Linux/VM/GPU acceptance remains pending. This release does not establish new numerical or materials-calibration acceptance.
