# SPDX-License-Identifier: GPL-3.0-or-later
"""Keep exact, already-downloaded original license inputs with the source tree."""
from pathlib import Path
import ast,hashlib,json,shutil
repo=Path(__file__).resolve().parents[1];tree=ast.parse((repo/'build/Generate-Linux-Notices.py').read_text())
urls=sorted({n.value for n in ast.walk(tree) if isinstance(n,ast.Constant) and isinstance(n.value,str) and n.value.startswith('https://raw.githubusercontent.com/')})
dst=repo/'Licenses/linux-cache';dst.mkdir(exist_ok=True);index={}
for url in urls:
    name=hashlib.sha256(url.encode()).hexdigest();source=Path.home()/'.cache/ppm-licenses'/name
    if not source.exists():raise RuntimeError('Missing audited license input '+url)
    shutil.copy2(source,dst/name);index[url]={'file':name,'sha256':hashlib.sha256(source.read_bytes()).hexdigest()}
(dst/'index.json').write_text(json.dumps(index,indent=2))
print('Vendored',len(index),'unmodified original license texts')
