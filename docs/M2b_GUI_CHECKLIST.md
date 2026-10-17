# M2b GUI acceptance checklist

Owner: Lota. Run these steps only during an explicitly authorized GUI session.
This checklist was written after headless checks; visual appearance, interactive
picking, VTK highlighting and mouse/keyboard workflows still require acceptance.

## Prepare the fixture and application

1. Use `<workspace>\prepomax\run_m2b\PrePoMax.exe`. Keep this runtime
   separate from `run\` and other installed PrePoMax copies.
2. Open `<workspace>\prepomax\logs\M2b_acceptance\M2b_layup.pmx`.
   It is made by `Test_Model_Composite_Layup_Editor.py`. If missing, regenerate
   with the report's headless command before starting this session.
3. Save As `<workspace>\prepomax\logs\M2b_GUI_work.pmx` so the
   original automated fixture remains available. Record the tested commit and
   Windows display scaling; capture screenshots for failures.
4. Select Model view. Expand **Composite Layups**. Expect one active layup
   named **Persist**, two plies P01/P02, material IM7_8552, thickness 0.5 mm
   each, angles +30/-30 degrees, CS Rect, stacking axis 3, symmetric No,
   tolerance 0.000003. Mesh: 4 x 2 x 2 C3D8I elements, 16 total.

## Open, edit, plot and highlight

5. Double-click Persist. Expect one resizable Composite Layup editor with a
   property grid above the ply table, seven table action buttons, a stack
   plot on the right and a readable validation summary below. No clipping at
   the minimum window size; resize larger and check the plot/table again.
6. Check Name, Region type/Element set COUPON, Coordinate system Rect,
   Stacking axis, Symmetric and Boundary tolerance. The coordinate dropdown
   contains Global and rectangular systems only. A missing referenced system
   remains displayed as the current value and must be reported invalid.
7. Select P01 and P02 in turn. Expect the lower eight and upper eight elements
   respectively highlighted red in the 3D view. Rotate the model to inspect
   the internal layer. The mesh/model must not gain persistent LU_/OR_ sets.
8. Change P01 angle to `45 deg`, commit with Tab. Expect its plot label to
   change to `P01 45°`. Change back to `30 deg`. Check that Material is a real
   dropdown listing model materials.
9. Enter `0.05 cm` for P01 thickness and commit. Expect the displayed/model
   value to equal 0.5 mm and the summary to remain valid. Angle input
   `0.5235987755982988 rad` should equal approximately 30 degrees.
10. Clear P01 Elements and commit: it is optional and remains valid. Enter
    `1` again. It checks mesh bands and must not generate elements.
11. Change P01 angle, then click Cancel. Reopen Persist and verify the original
    +30 degrees remains. Verify picking is no longer active after Cancel.

## Table actions, symmetry and validation before OK

12. With Persist open, select P01 and click Duplicate. Expect a distinct row
    after P01, with copied values and a numbered name. A thickness mismatch
    appears immediately, and OK/OK-New are disabled.
13. Edit the duplicate's name, material or angle; verify P01 is unchanged.
    Move down and Move up, checking row order and plot labels each time.
    Delete the duplicate; expect the original valid two-row stack again.
14. Click Insert with P02 selected: a new row appears before it. Delete it.
    Click Add: a new row appears at the end. Delete it. Reverse twice and
    confirm the original order. Sorting by column headers must not reorder
    plies accidentally.
15. Change both thicknesses to 0.25 mm and set Symmetric Yes. Expect the plot
    bottom-to-top P01/P02/P02/P01, a dashed red midplane and four 0.25 mm
    bands. The present two-layer mesh is invalid (boundary crossing); OK
    remains disabled. Selecting an edited row must never highlight stale
    IDs from the previous valid stack. Restore Symmetric No and both 0.5 mm.
16. Enter negative thickness, NaN, nonnumeric angle, Elements 0 and Elements
    1.5 one at a time. Expect a coloured cell, an explanation and refusal to
    commit/OK. Escape the invalid cell or correct it. Cancel must still work.
17. Set thicknesses to 0.4/0.6 mm. Total is still 1 mm; expect an explicit
    boundary-crossing element count/IDs. Restore 0.5/0.5.
18. Set Elements to 2 on P01. Expect a mesh-band-count error (only one band
    exists per ply). Restore 1. Change tolerance to -1 or 0.5: expect an error;
    restore 0.000003. Do not use tolerance to hide nonconforming layers.
19. Click OK after valid changes, reopen and verify all committed values.
    Check the plot can scroll with many rows and each thin ply stays visible;
    cancel that exploratory edit.

## Tree actions, commands and overlap

20. Right-click Persist -> Duplicate. Expect an independently named tree
    entry. Both active layups overlap and become invalid; this is expected.
21. Deactivate the original Persist. Open the duplicate, change P01 to 15
    degrees and OK. Reopen Persist to verify its P01 is still 30 degrees.
22. Rename the duplicate with the tree Rename action to **EditedCopy**.
    Verify edit opens EditedCopy and the old name is gone. Try renaming it
    to Persist: refuse a case-insensitive duplicate name.
23. Undo the rename, then Redo. Verify the name and editable plies each time.
    Undo/Redo the preceding edit, deactivate and duplicate actions in order;
    no missing internal sets or dangling tree entries. Restore the working
    state with Redo before continuing.
24. Delete EditedCopy using the tree action. Undo restores the full layup;
    Redo removes it again. Activate Persist. Expect it to be valid again.
25. Right-click Composite Layups -> Create. Enter a new name and choose
    Region type Part, then choose the solid part; also inspect Element set.
    Add a full-thickness ply. Expect an overlap error naming Persist. Cancel.
26. Deactivate Persist. Create a full-thickness layup over COUPON, then OK-New.
    Expect the first layup to be saved and a fresh editor ready for the next
    name; the new overlapping draft is invalid until a disjoint region is
    chosen. Cancel it, delete the created test layup and reactivate Persist.

## Interactive retained selection and dependencies

27. Deactivate Persist. Create **Picked**, choose Region type Selection, and
    use the existing selection dialog/ellipsis to select the complete solid
    mesh (16 elements). Define one 1 mm ply using IM7_8552 and optional count
    2. Expect valid preview/highlight and successful OK. Reopen Picked: the
    retained selection must be present and editable.
28. Duplicate Picked, deactivate the original, edit the copy and remove it.
    The original's selected region must remain intact. Delete Picked and
    reactivate Persist. No hidden selection set should remain owned by a
    deleted layup. Repeat selection with a geometry/part selection if CAD is
    available, recording whether regeneration preserves the region.
29. Rename material IM7_8552 to **MatRenamed** using the material tree.
    Reopen Persist: both material cells show MatRenamed and validation passes.
    Undo/Redo the rename and verify the references track both operations.
30. Rename CS Rect to **CSRenamed**, then element set COUPON to **RegionRenamed**.
    Reopen Persist: both references follow the names and it stays valid.
31. Delete CSRenamed. Persist becomes invalid and reports the missing system
    when opened, without silently falling back to Global. Undo repairs it.
32. Delete MatRenamed, then Undo; delete RegionRenamed, then Undo. Each
    deletion invalidates Persist with the missing reference; each Undo
    restores the reference and a valid editor.
33. Change Persist to a Part region and OK. Rename that part and verify the
    editor follows its new name. Delete the part: Persist must be invalid,
    not silently assigned to another part. Undo and check the region again.
34. With Persist active, attempt to add an ordinary solid section on its
    region. Open Persist: the summary must name the overlapping section.
    Remove the section and verify validation recovers.

## Persistence, journal and final evidence

35. Save, close and reopen the working .pmx. Check name/order, all ply values,
    region, CS, axis, symmetry, tolerance and active state match. A Python-
    created layup must open in the same editor as a GUI-created one.
36. Export CalculiX input and inspect LU_/OR_ cards and ply material/angle
    references. Open keyword preview; generated nodes and an existing user
    keyword remain in their intended groups. Cancel the keyword editor.
37. Export the session Python journal using the existing history/journal
    workflow. Layup add/replace/remove/duplicate/rename/activate/deactivate
    must emit composite_layups API calls without refusals. Replay on a clean
    copy of the same input fixture and compare the final layup definitions.
38. Record pass/fail per numbered step, tested commit, executable SHA256,
    screenshots and any GUI exceptions in `logs\M2b_GUI_acceptance.md`.
    Leave GUI acceptance pending until these interactive steps have passed.
