#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
logs="$repo/../logs/L3b2/forms"
out=/root/ppm-l3b2/solve-static
mkdir -p "$out" "$logs/solve-static"
python3 - "$logs/ui-DCB/linux.inp" "$out/static.inp" <<'PY'
from pathlib import Path
import re,sys
data=Path(sys.argv[1]).read_bytes()
end=re.search(rb'^\*END STEP[^\r\n]*(?:\r?\n|$)',data,re.I|re.M)
assert end,'No static step in the edited export'
deck=data[:end.end()]
assert re.search(rb'^\*STATIC',deck,re.I|re.M)
assert not re.search(rb'SOLVER=(PARDISO|PASTIX)',deck,re.I),'Explicit solver unsupported; do not silently change it'
Path(sys.argv[2]).write_bytes(deck)
PY
export OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1 MKL_NUM_THREADS=1
cd "$out"
nice -n 19 timeout 900 /root/ppm-l3b/solver/install/ccx_fat -i static > "$logs/solve-static/solver.log" 2>&1
cp static.inp static.sta static.dat "$logs/solve-static/"
python3 - "$logs/solve-static" <<'PY'
from pathlib import Path
import hashlib,json,sys
p=Path(sys.argv[1]);text=(p/'solver.log').read_text(errors='replace')
assert 'Job finished' in text and '*ERROR' not in text,text[-3000:]
(p/'acceptance.json').write_text(json.dumps(dict(passed=True,solver='/root/ppm-l3b/solver/install/ccx_fat',static_only=True,source='ui-DCB/linux.inp through its first END STEP',inp_sha256=hashlib.sha256((p/'static.inp').read_bytes()).hexdigest()),indent=2))
print('PASS edited DCB first static step solves with Linux ccx_fat')
PY
