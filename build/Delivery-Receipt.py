# SPDX-License-Identifier: GPL-3.0-or-later
from pathlib import Path
import hashlib,json,sys
app=Path(sys.argv[1]);delivery=Path(sys.argv[2])
revision=(app/'SOURCE_REVISION.txt').read_text().splitlines()
inventory=json.loads((app/'license-inventory.json').read_text())
files=[]
for p in sorted(delivery.iterdir()):
    if p.name.endswith(('.tar.gz','.deb')):
        sha=hashlib.sha256()
        with p.open('rb') as f:
            while data:=f.read(1024*1024):sha.update(data)
        files.append(dict(name=p.name,bytes=p.stat().st_size,sha256=sha.hexdigest()))
result=dict(gitRev=revision[0],gitStatusShort=revision[1:],abi='0.2 UNSTABLE',
    installedBytes=sum(p.stat().st_size for p in app.rglob('*') if p.is_file() and not p.is_symlink()),
    privateVtkBytes=(app/'libvtkmax.so.0.2.0').stat().st_size,
    licensedBinaryNames=len({f for r in inventory for f in r['files']}),licenseComponents=len(inventory),packages=files)
(delivery/'delivery-receipt.json').write_text(json.dumps(result,indent=2))
print('PASS delivery receipt; source rev',revision[0],'dirty entries',len(revision)-1)
