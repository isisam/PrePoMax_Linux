# RC1e runtime language switching

Based on RC1d (`f61cede4`) on `rcm/rc1`.

- Result-type and deformation-scale toolbar choices retain enum identities;
  display text is no longer parsed to determine the selected result operation.
- Color-by choices retain the corresponding menu item identity, including None.
- Runtime language changes refresh the three translated lists on the UI thread,
  preserve selections, field variable, scale factor and complex angle, and avoid
  intermediate selection events/redraws. The language subscription is removed
  when the main form is disposed.
- Other result-toolbar lists contain result names, field names, step/increment
  identifiers, or symbol/model names and retain those data identities. Settings
  pages use dynamic property descriptors with enum/bool identities and refresh
  their metadata when the language changes.

Acceptance: `tests/RC1e/run.py` runs the product's Settings Apply path against
DCB, ENF and Layup FRDs on a private, never-switched Windows desktop. It checks
en -> zh-TW -> en selection identity, live stored combo text, non-default factor
and angle retention, field/legend extrema and rendered contour geometry against
an axes/legend-only control image. Settings choices are checked on the same 13
page view instances across both switches. Existing M1, M2a, M2b/GUI3, L1a, L1b,
L10N and FATCZM 300-jump gates remain required.

The release retains RC1d first-run language and RC1c work-directory defaults.
No solver, numerical model or example input changes are included.
