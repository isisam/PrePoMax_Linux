# Session journal baselines

Isisam: a PMX snapshot is not necessarily reconstructible from its command history.
The GUI2 fixture has 16 elements, but its recorded import describes an earlier
one-element cube. Replaying that import on top of the snapshot produced 17.

Every exported journal now starts with `# PPM-JOURNAL-BASELINE` metadata:

- `empty`: the host creates a new model before recording the script. The entire
  recorded history follows. Loading a fixture with `-f` does not append its mesh.
- `file <base64 UTF-8 path> <SHA256>`: the host verifies and opens that baseline
  before recording any script commands. Relative paths resolve beside the journal.
  GUI session export creates `name.baseline.pmx` beside `name.py`, then exports
  only commands after the opened baseline's command count. Keep both files together.

Use the ordinary GUI **File -> Export -> Python session journal** workflow or
`Controller.ExportSessionJournal`. Run the resulting file directly with
`PrePoMax.com --script name.py -g No -x Yes -w <fresh output directory>`.
Starting it with `-f fixture.pmx` is also supported: the declared baseline wins.
Loading the baseline is a host operation before command recording, so the existing
restriction on `pmx.open`/`pmx.new_model` inside a recorded script remains intact.
Calling `exec` on a journal inside another recorded script is unsupported; the
top-level loader must see the metadata. Do not remove the metadata line.

The controller retains the opened compressed PMX bytes in transient session memory,
so Save/Save As cannot overwrite the baseline. They are not added to the PMX schema.
New/Clear releases them. Undo retains the session baseline; export refuses if Undo
crossed its command boundary, rather than silently exporting an incorrect delta.
Reopening a saved PMX establishes a new named baseline, including all saved edits.
Missing/corrupt sidecars are refused before replacing the current model.

Generic `PythonJournalExporter.Export(commands,...)` describes a complete history
with an empty baseline (or its explicit initial Open command). Use the controller's
session export for a history inherited from a PMX snapshot.

`tests/M2bUi/replay.py` verifies named and empty baselines, each from both an empty
host and the original fixture, against complete node coordinates, element
connectivity and layup properties. Each case must retain 16 elements. The UI test
also checks corrupt-sidecar refusal and real GUI add/replace commands; duplicate,
rename, activate/deactivate and remove use the same controller commands as the tree.
