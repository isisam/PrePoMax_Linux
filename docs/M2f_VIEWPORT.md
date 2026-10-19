# RC1g5 — material directions in the model viewport

Open a Composite Layup and select a ply row. **Show fibre directions / 顯示纖維方向** is on by default. The main 3D model viewport draws a short red fibre line at every selected element's corner-node centroid, a shorter green transverse line and a blue positive stacking-normal hint. Symmetric partners appear together. Rotate and zoom the main viewport normally; glyph coordinates remain attached to the mesh.

The angle uses the selected rectangular CSYS and the solver's cyclic basis: stacking X uses Y/Z, stacking Y uses Z/X, stacking Z uses X/Y. Positive angles follow the right-hand rule about the positive stacking axis. The preview reads the validated expanded ply data used by the input writer. Invalid drafts clear stale glyphs.

Move **Glyph size / 方向標記尺寸** to scale the lines relative to the local element's in-plane span. The rest of the model is temporarily translucent. Turn the checkbox off, cancel, close or dispose the editor to restore the original opacity, including previously configured part and edge transparency.

After closing the editor, right-click a saved layup in the Model tree and select **Show fibre directions / 顯示纖維方向**. This toggles all expanded plies in the actual 3D viewport. The adjacent **Glyph size / 方向標記尺寸** submenu contains the size slider. Adjusting it displays the saved layup and updates the glyphs. A model redraw or view/model replacement clears transient actors.

This feature does not modify the mesh, material, layup, solver input, command journal or saved model. The editor schematic and all M2e layout fixes remain. Exploded ply geometry is not included in RC1g5; the optional layer-separation mode remains future work.

The native Windows 150% / 144-DPI acceptance item from RC1g4 remains open. The automated 1.5x layout stress test does not replace it. Existing project/private-path redaction policy and the disclosed unchanged vendor diagnostic-path exception remain in effect.

Source, reproducible checks and packaging: `tools/m2f/`. Screenshots are captured in-process from the main VTK renderer, on a private desktop never switched into view. No installers or system setting changes are required.
