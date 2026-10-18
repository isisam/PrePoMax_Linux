# SPDX-License-Identifier: GPL-3.0-or-later
"""Check evidence provenance without inventing execution revisions for older logs."""
from pathlib import Path
import hashlib,json,sys
root=Path(sys.argv[1]);records=[]
for path in sorted(root.rglob('*.log')):
    if 'windows-runtime' in path.parts or 'headless-linux' in path.parts:continue
    content=path.read_bytes();head=content[:20000]
    records.append(dict(path=str(path.relative_to(root)),bytes=len(content),sha256=hashlib.sha256(content).hexdigest(),
        provenance=b'git rev:' in head and b'git status --short:' in head))
(root/'log-audit.json').write_text(json.dumps(records,indent=2))
missing=[r['path'] for r in records if not r['provenance']]
print('Log provenance:',len(records)-len(missing),'/',len(records))
if missing:print('\n'.join(missing))
raise SystemExit(bool(missing))
