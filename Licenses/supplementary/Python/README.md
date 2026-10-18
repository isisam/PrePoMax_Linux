# Python dependency licence index (RC1b)

All notices below accompany the shipped CPython 3.12.10 Windows x64 runtime. Preserve this directory on redistribution.

| Component | Shipped version | Payload | Notice |
| --- | --- | --- | --- |
| CPython | 3.12.10 | python312.dll, all 22 .pyd, standard library | CPython-3.12.10-LICENSE.txt; CPython-3.12.10-stdlib-notices.rst |
| Tcl | 8.6.15 (CPython binary revision 8.6.15.2) | tcl86t.dll | Tcl-8.6.15-license.terms |
| Tk | 8.6.15 (CPython binary revision 8.6.15.2) | tk86t.dll | Tk-8.6.15-license.terms |
| LibTomMath | vendored in Tcl core-8-6-15 | inside tcl86t.dll | libtommath-LICENSE.txt |
| OpenSSL | 3.0.16 | libcrypto-3.dll, libssl-3.dll; _ssl/_hashlib | OpenSSL-3.0.16-LICENSE.txt; OpenSSL-3.0.16-NOTICE.md |
| libffi | 3.4.4 | libffi-8.dll; _ctypes | libffi-3.4.4-LICENSE.txt |
| SQLite | 3.49.1 | sqlite3.dll; _sqlite3 | SQLite-3.49.1-public-domain.txt |
| bzip2 | 1.0.8 | statically linked in _bz2.pyd | bzip2-1.0.8-LICENSE.txt |
| XZ/liblzma | 5.2.5 | statically linked in _lzma.pyd (liblzma only) | xz-5.2.5-COPYING.txt |
| zlib | 1.3.1 | python312.dll and zlib1.dll | zlib-1.3.1-LICENSE.txt |
| Expat | 2.7.1 | pyexpat.pyd, _elementtree.pyd | expat-2.7.1-COPYING.txt |
| libmpdec | 2.5.1 | _decimal.pyd | mpdecimal-2.5.1-LICENSE.txt |
| HACL* | snapshot in CPython v3.12.10 | Python hash built-ins | HACL-CPython-3.12.10-LICENSE.txt |
| KaRaMeL | support headers in CPython v3.12.10 | HACL* hash implementation support | KaRaMeL-CPython-3.12.10-NOTICES.txt; Apache-2.0.txt |
| BLAKE2 | snapshot in CPython v3.12.10 | Python BLAKE2 built-in | BLAKE2-CPython-3.12.10-NOTICES.txt |
| Microsoft VC14 CRT | 14.44.35211.0 | 10 runtime DLLs in Python/ | ../../Microsoft-runtime-NOTICE.txt |

`binary-audit.json` accounts for every one of the 40 Python DLL/pyd files, including hashes, versions and imports. No pip or third-party site-packages are present. `sources.json` records versioned upstream URLs and downloaded hashes. The CPython standard-library notices retain the other incorporated-code acknowledgements. Only liblzma from XZ is shipped; its public-domain terms are in COPYING, and the command-line GPL tools are not included. HACL/BLAKE2 licences are reproduced from the exact CPython source headers; SQLite uses the public-domain dedication in its 3.49.1 header.

Versions were checked against runtime queries and CPython v3.12.10 build pins (PCbuild/python.props, tcltk.props). All Tcl/Tk/libffi PE sections additionally match the pinned Python binary dependencies byte for byte; Tcl/Tk signed-file envelopes differ, while libffi matches as a whole file. All Python .pyd files report 3.12.10.

The complete BLAKE2 public-domain dedication is supplied in CC0-1.0.txt.
