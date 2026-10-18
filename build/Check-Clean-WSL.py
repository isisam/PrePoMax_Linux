# SPDX-License-Identifier: GPL-3.0-or-later
from pathlib import Path
import concurrent.futures as cf,json,subprocess as sp,time
repo=Path(__file__).resolve().parents[1];logs=repo.parent/'logs/L2b';flags=0x08000000
names=['PPM-L2b-U22','PPM-L2b-U24','PPM-L2b-D12']
def check(name):
    with (logs/(name+'-package.log')).open('wb') as log:
        log.write(b'git rev: '+sp.check_output(['git','rev-parse','HEAD'],cwd=repo,creationflags=flags)+b'git status --short:\n'+sp.check_output(['git','status','--short'],cwd=repo,creationflags=flags));log.flush()
        t=time.monotonic()
        cmd=['wsl','-d',name,'--','bash','<workspace>/prepomax/work_l2b/build/Check-Clean-Package.sh','<workspace>/prepomax/logs/L2b_pkg']
        p=sp.run(cmd,stdout=log,stderr=sp.STDOUT,creationflags=flags,timeout=900)
        log.write(('\nexit='+str(p.returncode)+'\n').encode())
        return dict(distro=name,exit=p.returncode,seconds=round(time.monotonic()-t,2))
with cf.ThreadPoolExecutor(max_workers=3) as pool:results=list(pool.map(check,names))
(logs/'clean-package-summary.json').write_text(json.dumps(results,indent=2));print(json.dumps(results,indent=2))
raise SystemExit(int(any(r['exit'] for r in results)))
