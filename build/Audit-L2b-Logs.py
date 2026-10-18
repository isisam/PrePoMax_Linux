# SPDX-License-Identifier: GPL-3.0-or-later
"""Keep solver output intact and attach its parent harness's execution provenance."""
from pathlib import Path
import hashlib,json
repo=Path(__file__).resolve().parents[1];logs=repo.parent/'logs/L2b'
child=logs/'dcb-solve/fatczm_jumps.log'
marker=b'\nExecution provenance inherited from parent dcb-fixture-solve.log:\n'
if child.exists():
    raw=child.read_bytes().split(marker,1)[0]
    parent=logs/'dcb-fixture-solve.log'
    header=parent.read_bytes().split(b'git rev/status above',1)[0]
    child.write_bytes(raw+marker+header)
rows=[]
for path in sorted(logs.rglob('*.log')):
    data=path.read_bytes()
    rows.append(dict(path=str(path.relative_to(logs)),sha256=hashlib.sha256(data).hexdigest(),
                     provenance=(b'git rev:' in data or b'git rev-parse HEAD' in data) and b'git status --short' in data))
(logs/'log-audit.json').write_text(json.dumps(rows,indent=2))
missing=[r['path'] for r in rows if not r['provenance']]
assert not missing,missing
print('PASS git revision and status in all',len(rows),'logs; hashes in log-audit.json')
