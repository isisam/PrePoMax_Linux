"""Two independent new WSL acceptance distros at a time; hidden Windows processes only."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
import json,subprocess as sp,time,sys
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L3b'
if '--final' in sys.argv:logs=logs/'final'
logs.mkdir(parents=True,exist_ok=True)

def linux(path):
    p=path.resolve().as_posix();return '/mnt/'+p[0].lower()+p[2:]

def check(name):
    assert name in ('PPM-L3b-U22','PPM-L3b-U24','PPM-L3b-D12')
    start=time.monotonic()
    with (logs/(name+'-package.log')).open('wb') as log:
        for cmd in (['git','rev-parse','HEAD'],['git','status','--short']):log.write(sp.check_output(cmd,cwd=repo,creationflags=0x08000000))
        log.flush()
        p=sp.run(['wsl','-d',name,'--','env','PPM_L3B_DISTRO='+name,'PPM_PACKAGE_EVIDENCE='+linux(logs/'clean'/name),
            'bash',linux(repo/'build/Check-L3b-Clean.sh')],stdout=log,stderr=sp.STDOUT,creationflags=0x08000000,timeout=1800)
    result=dict(distro=name,exit=p.returncode,seconds=round(time.monotonic()-start,2));print(result,flush=True);return result

if __name__=='__main__':
    results=[]
    with ThreadPoolExecutor(max_workers=2) as pool:
        for future in as_completed([pool.submit(check,n) for n in ('PPM-L3b-U22','PPM-L3b-U24','PPM-L3b-D12')]):
            results.append(future.result());(logs/'clean-package-summary.json').write_text(json.dumps(results,indent=2))
    raise SystemExit(int(any(r['exit'] for r in results)))
