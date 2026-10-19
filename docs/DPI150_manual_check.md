# RC1g4 — native Windows 150% / 144-DPI owner check

Status: **OPEN — owner manual check required.** Automated `Scale(SizeF(1.5,1.5))`
with a scaled font runs at device DPI 96. It does not prove native 144-DPI layout.
The build agent makes no system changes and opens no windows on the owner's desktop.

1. On an owner-controlled Windows test session already using **150% display scale**,
   confirm Settings → System → Display → Scale = 150%. If changing scale is needed,
   do it yourself when convenient; sign out/in if Windows requests it. Start a fresh
   application process after choosing the test display. Record Windows version,
   display resolution, scale, application version and date. Do not use bitmap zoom
   or compatibility DPI overrides as a substitute for native application scaling.
2. Extract `PrePoMax-RCM_win64_RC1g4.zip` into a new folder (Chinese characters and
   spaces are encouraged). Verify its adjacent SHA256SUMS. Launch PrePoMax and open
   `Examples/Layup/Layup.pmx`. Open the Composite Layup editor for its existing layup.
   If a native DPI measurement is available, record **144 DPI**; otherwise retain a
   screenshot of Windows display scale and describe the measurement limitation.
3. In **English**, inspect the default editor size and then resize it to its supported
   minimum (960×760 logical baseline; normally about 1440×1140 outer pixels at 150%).
   Check every ply action: Add, Insert, Delete, Move up, Move down, Duplicate, Reverse.
   No button edge or label may be clipped. Toolbar wrapping must reserve complete
   rows. OK and Cancel must stay fully visible at the bottom while content scrolls.
   Also open a new layup draft and verify **OK - New** fits. Cancel this draft.
4. Select each existing ply: the selected plate is red and the corresponding elements
   highlight. Toggle symmetry in a draft: both partner plates highlight. Scroll to
   the last plate and across a narrow preview; return to the first. Reverse twice:
   the original order returns. Resize between default and minimum without losing
   access to buttons, grid, properties or validation. Cancel all changes.
5. Switch to **繁體中文** and repeat steps 3–4, including **反轉**, **確定**, **取消**
   and the new-dialog action. Switch back to English and confirm labels/layout restore.
   If using multiple monitors, repeat on the 150% display with a newly started process;
   record any move-between-monitor issue separately.
6. Save four full-editor screenshots (EN/zh-TW × default/minimum), plus the new-dialog
   footer if needed. Record PASS/FAIL for toolbar, footer, scrolling, selection and
   symmetry. A hidden/cropped/unreachable button is a FAIL. Send the evidence with
   the package hash to the release owner; only this native check can close RT-G2-02.

Owner receipt: date ______ Windows ______ resolution ______ scale/DPI ______
ZIP SHA-256 ______ EN default/min ______ zh-TW default/min ______
selection/symmetry/scroll ______ result PASS / FAIL ______ evidence location ______
