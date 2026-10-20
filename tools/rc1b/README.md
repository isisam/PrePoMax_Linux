# RC1b guide rebuild

Use existing Python with reportlab and the Windows Microsoft JhengHei font; no install is required by the runtime. Set RC1B_PACKAGE to the package directory, then run `python Guides.py`. The script reads the retained real-control screenshots in Documentation/screenshots and emits both ten-page guides plus pdf_layout.json beside this script. Author attribution is S.Y. Su

RC1b changes documentation and licence notices only. Application, Python, solver and private CRT binary payloads are unchanged from RC1. See docs/VC90_STATIC_CHECK.md and Licenses/supplementary/Python/README.md for the acceptance limit and audited dependency versions.
