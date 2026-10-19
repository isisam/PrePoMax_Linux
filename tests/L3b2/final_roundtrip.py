"""Fresh Windows P3 reload, typed assertions, exact bytes; no normalization."""
from pathlib import Path
import hashlib,json,os,subprocess as sp,sys
repo=Path(__file__).resolve().parents[2]
root,runtime=map(Path,sys.argv[1:3]);summary={}
for name in ['DCB','M2a','old-DCB']:
    folder=root/('ui-'+name);target=folder/'windows-reload';target.mkdir()
    assert json.loads((folder/'acceptance.json').read_text())['passed']
    cmd=[str(runtime/'PrePoMax.com'),'-f',str(folder/'edited.pmx'),'--script',str(repo/'tests/L3b2/VerifyStrictP3.py'),'-g','No','-x','Yes','-w',str(target),'--report',str(target/'run.json'),'--noSettingsSave','--timeout','180']
    with (target/'run.log').open('wb') as log:r=sp.run(cmd,cwd=repo,stdout=log,stderr=sp.STDOUT,creationflags=0x08000000,timeout=210,env=dict(os.environ,PPM_FINAL_P3_OLD='1',OMP_NUM_THREADS='1',DOTNET_PROCESSOR_COUNT='4'))
    assert r.returncode==0 and 'TEST PASSED' in (target/'run.log').read_text(encoding='utf-8',errors='replace')
    a=(folder/'linux.inp').read_bytes();b=(target/'model.inp').read_bytes()
    summary[name]=dict(passed=a==b,bytes=len(a),linux_sha256=hashlib.sha256(a).hexdigest(),windows_sha256=hashlib.sha256(b).hexdigest(),windows_exit=r.returncode)
    (root/'roundtrip.json').write_text(json.dumps(summary,indent=2));assert a==b,summary[name]
    print('PASS final P3 raw byte identity '+name,flush=True)
