# PrePoMax zh-TW add-on

`PrePoMax.L10n` targets .NET Framework 4.8 and has no package dependency. The
Windows build ships `lib/PrePoMax.L10n.dll` and `lang/zh-TW.json`. No installation,
registry change, network access or translation service is needed by the app.

Choose **Tools → Settings → General → Language → English / 繁體中文**, then accept
the settings. A missing language field in old settings, or Reset, follows the
Windows UI culture. Changing language does not change numeric or UI thread culture.

The startup hook installs the walker and two optional `CaeGlobals.UiLocalization`
callbacks. The shared helper has no dependency on the translator or WinForms.
Existing dynamic descriptors retain their identity, attributes, values, editors,
ordering and visibility. Other selected property-grid objects get an instance
description provider. Tree/category and fixed type-list text is painted separately
from the English keys used by routing and serialization. Editable values and
entity selectors are preserved.

Form visibility, application idle and a 200 ms UI timer discover and update forms,
dynamic captions and menus. Captions retain their latest English source, so
translation is idempotent and switching back restores source text. Native common
message helpers translate only interactive presentation; batch output and strict
prompt reports keep their original contract. The state bar uses a separate
invariant state field.

## Editing the catalogue

The file is an ordinary UTF-8 JSON string dictionary:

```json
{
  "Mesh": "網格",
  "FrmMaterial.lName": "材料名稱",
  "ViewUserMaterial.NumberOfStateVariables.Description": "狀態變數說明",
  "FrmExample.button|Run": "執行",
  "Thickness [{0}]": "厚度 [{0}]"
}
```

Lookup precedence: `context|source`, `context`, exact source, optional mnemonic /
property-order prefix normalization, then unchanged English. Explicit message
templates retain all captured parameter text, including empty strings. Unknown
messages are not translated by word replacement. An absent, unreadable, null or
malformed catalogue falls back to English.

The catalogue is a **first translation draft**, not an engineering-language
certification. Core terminology and FATCZM/layup explanations are specified by
hand. Other public upstream UI literals were initially translated using Google's
public translation endpoint, with the engineering lexicon protected and restored.
The retained scripts record provenance and check completeness and argument
preservation. The 150-term glossary is the terminology review entry point.

## Reproduction on the owner's Windows workspace

Use the existing Python 3.10, VS 2022 BuildTools, released RC1 payload, M1/M2
fixtures and solver bundle. Commands run from `work_l10n`; the helpers launch
console/solver children with `CREATE_NO_WINDOW` and cap build/solver concurrency.

```powershell
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\extract.py
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\catalogue.py
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\build.py
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\verify.py assemble
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\verify.py checks
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\audit.py
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\verify.py regressions
& 'C:\Program Files\Python310\python.exe' -B tools\l10n\extended.py
```

`catalogue.py` without `--translate` is offline and reuses reviewed entries. New
strings appear in `lang/missing.json`. The optional `--translate` is an authoring
aid for **public literal UI text only**; it must never be used for private data.
It retries bad batches separately and rejects changed placeholders / token debris.

The extractor is a C# literal scanner with invocation/concatenation handling, not
a full semantic compiler. Its source inventory deliberately includes fragments
and hidden metadata. Runtime form samples supplement extraction, and do not
claim to visit every application state, native file dialog, plot annotation or
third-party widget. Numeric inputs, keyword-editor content, units, file patterns,
model names and user descriptions remain data.

For the explicitly authorized screenshot phase, run `screenshots.py` through
`run_l10n/PrePoMax.com` with the M2b PMX fixture, `-g Yes -x Yes -w <logs/L10N>` and
`--timeout 600`. Set `gui_command.txt` to `main`, `material`, `layup`, then `close`.
Each stage reports readiness. Capture the returned native app/window with the
computer-use skill; owned dialog images accompany the main window screenshot.
The driver disposes its child forms and closes its own application after `close`.
It changes only the disposable fixture, with settings autosave disabled.
