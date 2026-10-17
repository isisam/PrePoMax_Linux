# SPDX-License-Identifier: GPL-3.0-or-later
"""Create only the three dedicated L2b acceptance distros; never unregister any.
Downloads/imports are repeatable. Run on Windows, all subprocesses hidden.
"""
from pathlib import Path
import concurrent.futures as cf, hashlib, json, subprocess as sp, sys, urllib.request
repo=Path(__file__).resolve().parents[1]; root=Path(r'<workspace>\wsl'); root.mkdir(exist_ok=True)
logs=repo.parent/'logs/L2b'; logs.mkdir(parents=True,exist_ok=True)
flags=0x08000000
def git(*args): return sp.check_output(['git','-C',str(repo),*args],creationflags=flags)
sources={
 'PPM-L2b-U22':('https://cloud-images.ubuntu.com/wsl/releases/jammy/current/ubuntu-jammy-wsl-amd64-wsl.rootfs.tar.gz','gz'),
 'PPM-L2b-U24':('https://cloud-images.ubuntu.com/wsl/releases/noble/current/ubuntu-noble-wsl-amd64-24.04lts.rootfs.tar.gz','gz'),
 'PPM-L2b-D12':('https://raw.githubusercontent.com/debuerreotype/docker-debian-artifacts/dist-amd64/bookworm/rootfs.tar.xz','xz'),
}
def prepare(item):
    name,(url,ext)=item; archive=root/(name+'.rootfs.tar.'+ext)
    with (logs/(name+'-rootfs.log')).open('wb') as log:
        log.write(b'git rev: '+git('rev-parse','HEAD')+b'git status --short:\n'+git('status','--short')); log.flush()
        try:
            if not archive.exists():
                tmp=archive.with_suffix(archive.suffix+'.partial')
                with urllib.request.urlopen(url,timeout=45) as r,tmp.open('wb') as f:
                    while data:=r.read(1024*1024): f.write(data)
                tmp.replace(archive)
            digest=hashlib.file_digest(archive.open('rb'),'sha256').hexdigest()
            log.write(f'URL {url}\nsha256 {digest}\n'.encode());log.flush()
            existing=sp.check_output(['wsl','--list','--quiet'],creationflags=flags).decode('utf-16le').split()
            if name not in existing:
                install=root/name; install.mkdir(exist_ok=True)
                result=sp.run(['wsl','--import',name,str(install),str(archive),'--version','2'],stdout=log,stderr=sp.STDOUT,creationflags=flags,timeout=300)
                if result.returncode: raise RuntimeError('wsl import exit '+str(result.returncode))
            result=sp.run(['wsl','-d',name,'--','cat','/etc/os-release'],stdout=log,stderr=sp.STDOUT,creationflags=flags,timeout=60)
            return dict(name=name,url=url,sha256=digest,status='imported' if result.returncode==0 else 'failed')
        except Exception as e:
            log.write(('SKIP/BLOCKED '+repr(e)+'\n').encode()); return dict(name=name,url=url,status='blocked',error=repr(e))
with cf.ThreadPoolExecutor(max_workers=3) as pool:
    results=list(pool.map(prepare,sources.items()))
(logs/'clean-wsl-rootfs.json').write_text(json.dumps(results,indent=2))
print(json.dumps(results,indent=2))
