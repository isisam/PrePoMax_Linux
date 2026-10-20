# RC1d quick starts

Set RC1D_PACKAGE to the package root, then run `python -B Guides.py`.
Requires the existing reportlab and Microsoft JhengHei font. Both ten-page
guides retain Isisam authorship. The language/default-work-directory instructions
are on page 2. See tests/RC1d and docs/RC1d_CHANGELOG.md.

Language acceptance against a fresh extracted copy (no settings.bin):

```powershell
python -B tests/RC1d/run.py --runtime '<extracted package root>' --output '<evidence folder>'
```

The runner launches every child hidden, caps concurrency, compiles the unit
checks against the delivered core, reads startup form texts before switching,
checks both settings choices, and retains RC1c work-directory assertions.
It uses existing VS 2022 Build Tools and makes no system changes.
