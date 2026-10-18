"""Check tar/deb on new L3a WSL imports; never unregister or use a personal distro."""
from pathlib import Path
import json,subprocess as sp,time
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L3a';flags=0x08000000
def linux(path):
    p=path.resolve().as_posix();return '/mnt/'+p[0].lower()+p[2:]
records=[]
for name in ('PPM-L3a-U22','PPM-L3a-U24','PPM-L3a-D12'):
    with (logs/(name+'-package.log')).open('wb') as log:
        log.write(b'git rev: '+sp.check_output(['git','rev-parse','HEAD'],cwd=repo,creationflags=flags))
        log.write(b'git status --short:\n'+sp.check_output(['git','status','--short'],cwd=repo,creationflags=flags));log.flush()
        start=time.monotonic()
        p=sp.run(['wsl','-d',name,'--','env','PPM_PACKAGE_VERSION=0.3.0','PPM_PACKAGE_EVIDENCE='+linux(logs/'clean'/name),'bash',linux(repo/'build/Check-L2c-Clean-Package.sh'),linux(logs/'packages')],stdout=log,stderr=sp.STDOUT,creationflags=flags,timeout=900)
        records.append(dict(distro=name,exit=p.returncode,seconds=round(time.monotonic()-start,2)))
        print(records[-1],flush=True)
        (logs/'clean-package-summary.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
        if p.returncode:raise RuntimeError(name+' package check failed')
