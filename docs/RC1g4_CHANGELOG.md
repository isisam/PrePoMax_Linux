# RC1g4 candidate — M2e layout repair (2026-10-19)

- RT-G2-01: measure the wrapping ply toolbar's preferred height instead of reserving
  a fixed 62-pixel row. Reverse/反轉 remains inside its parent at minimum size.
- RT-G2-02: use DPI autoscaling from a 96-DPI baseline; reserve a docked, automatically
  sized bottom action panel. Properties/grid/validation scroll when space is short.
  Automated checks cover all seven ply buttons and all three dialog buttons, their
  parents and ancestors, EN/zh-TW, minimum/default, and 1x/1.5x scaling plus scaled font.
- Preserve RC1g3's refreshed quick-start PDFs, screenshots and guide rebuild source.
  Those documents identify their RC1g3 guide revision; RC1g4 changes application layout.
- RT-G2-03: retain the **RC1f rule: zero project/private build paths**. The documented
  inherited vendor gmsh diagnostic paths are an upstream exception, not a claim of
  literal zero paths in every dependency. No vendor rebuild/sanitization is attempted.
- Native Windows 150% / 144-DPI acceptance remains **OPEN** until the owner completes
  `DPI150_manual_check.md`. Automated 1.5x scaling and 192-DPI preview images do not
  establish that gate. Keep RC1f as the accepted student/teacher release meanwhile.

Build/reproduction: `tools/m2e/run.py`, `layout.py`, `layout_check.py`, `seal.py`,
`final_checks.py`, `verify.py` and `delivery.py` in the accompanying source bundle.
Use existing VS 2022 Build Tools/.NET Framework 4.8 dependencies, disable upstream
post-build cleanup, and run GUI assertions only on a private desktop never displayed.
No installation, system DPI change or file deletion is required by this workflow.
