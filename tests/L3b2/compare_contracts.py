"""Compare complete hidden Windows PropertyGrid snapshots with the Linux grid schema."""
from pathlib import Path
import json, sys

windows, linux, output = map(Path, sys.argv[1:4])
a, b = json.loads(windows.read_text(encoding='utf-8-sig')), json.loads(linux.read_text(encoding='utf-8-sig'))
differences = {name: dict(windows=a.get(name), linux=b.get(name))
               for name in sorted(a.keys() | b.keys()) if a.get(name) != b.get(name)}
result = dict(passed=not differences, total=len(a.keys() | b.keys()),
              identical=len(a.keys() | b.keys())-len(differences), differences=differences)
output.write_text(json.dumps(result, indent=2), encoding='utf-8')
print(f"{result['identical']}/{result['total']} identical form contracts")
raise SystemExit(bool(differences))
