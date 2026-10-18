# RC1c first-run work directory

`SolverSettings.Reset()` previously set `_workDirectory` to null, while
`UsePmxFolderAsWorkDirectory` defaults to false. The RC package deliberately
does not ship `settings.bin`. Consequently a new installation had no usable
settings work directory; loading a model did not select its folder, and
controller operations guarded by `ShowWorkDirectoryError()` could display
`The work directory does not exist.`

`SettingsContainer.CheckWorkingDirectory()` only warns about non-English
characters. It neither creates the directory nor supplies a fallback.

RC1c creates `%USERPROFILE%\Documents\PrePoMax-RCM\Work` when default solver
settings are initialized or reset. Deserializing older settings migrates only
unset/blank work paths. Explicit paths, including missing custom paths, are
retained, so a user's choice is never silently replaced or created.

The existing preference remains false by default. If the user enables
`Use .pmx folder as work directory`, `SettingsContainer.GetWorkDirectory()`
still prefers the folder of an existing saved `.pmx`. Regeneration and CLI
overrides retain their existing precedence. English and bundled Spooles defaults
are unchanged.

Run `tests/RC1c/FirstRunProbe.py` through a fresh package's `PrePoMax.com`
with `-g No -x Yes --script ... --report ...`. Set `RC1C_FIXTURE` to an
existing packaged PMX and `RC1C_PROBE_OUT` to a writable JSON output path.
The CLI supplies its current-directory override even without `-w`; the probe
temporarily clears that override to exercise the settings directory used by
the GUI, then restores it. The test covers default creation, Reset, migration
through real BinaryFormatter serialization, preservation of custom/missing
paths, PMX-folder opt-in, and override precedence.

The release also runs Layup in the new default directory, and all three
packaged numerical examples from the final archive under a fresh path with
spaces and Chinese characters. Console and solver children are hidden and use
one solver CPU. These checks do not establish clean-OS or GUI visual acceptance.
