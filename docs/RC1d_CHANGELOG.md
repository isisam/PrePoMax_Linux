# PrePoMax-RCM 0.1.0-rc1d (2026-10-18)

Author: Isisam.

- First launch and Reset select 繁體中文 for Windows UI culture zh-TW, zh-HK or zh-Hant (including zh-Hant subtags); other UI cultures select English. Older settings without the optional language field use this fallback too.
- Explicitly saved English or zh-TW always takes precedence. Tools > Settings > General > Language continues to switch both ways and persist the choice.
- Preserve Windows UI culture during startup instead of replacing it with invariant culture. Numeric culture remains invariant for solver and API data.
- Retain RC1c's usable default work directory, old-null migration, custom paths, PMX-folder preference and command-line override precedence.
- Update both ten-page quick starts with Chinese-Windows startup behavior and instructions for switching to English; retain Isisam authorship.
- Add real-core language unit checks and a hidden fresh-start form-text probe. Regression receipts and final archive fresh-copy example results are recorded in RC1d_REPORT.md.
- Retain RC1b/RC1c archives. Native solvers, Python, examples, translation catalogue and licence notices remain unchanged from RC1c.
