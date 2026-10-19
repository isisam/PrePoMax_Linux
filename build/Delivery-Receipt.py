# SPDX-License-Identifier: GPL-3.0-or-later
from pathlib import Path
import hashlib,json,os,sys
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
native=(app/'libvtkmax.so').resolve(strict=True)
native_version=native.name.split('.so.',1)[1]
result=dict(gitRev=revision[0],gitStatusShort=revision[1:],abi='.'.join(native_version.split('.')[:2])+' UNSTABLE',packageVersion=os.environ.get('PPM_PACKAGE_VERSION'),
    installedBytes=sum(p.stat().st_size for p in app.rglob('*') if p.is_file() and not p.is_symlink()),
    privateVtkBytes=native.stat().st_size,
    licensedBinaryNames=len({f for r in inventory for f in r['files']}),licenseComponents=len(inventory),packages=files)
(delivery/'delivery-receipt.json').write_text(json.dumps(result,indent=2))
print('PASS delivery receipt; source rev',revision[0],'dirty entries',len(revision)-1)
