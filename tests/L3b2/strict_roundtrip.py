from pathlib import Path
import hashlib,json,os,subprocess as sp,sys
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L3b2/forms/strict-p3'
summary=json.loads((logs/'roundtrip.json').read_text()) if (logs/'roundtrip.json').exists() else {}
for name in sys.argv[1:] or ('DCB','M2a','old-DCB'):
    folder=logs/('ui-'+name);target=folder/'windows-reload';target.mkdir(exist_ok=True)
    assert not (target/'run.log').exists(),'Preserve attempt before retry'
    cmd=[str(logs/'windows-runtime/PrePoMax.com'),'-f',str(folder/'edited.pmx'),'--script',str(repo/'tests/L3b2/VerifyStrictP3.py'),'-g','No','-x','Yes','-w',str(target),'--report',str(target/'run.json'),'--noSettingsSave','--timeout','180']
    with (target/'run.log').open('wb') as log:r=sp.run(cmd,cwd=repo,stdout=log,stderr=sp.STDOUT,creationflags=0x08000000,timeout=210,env=dict(os.environ,OMP_NUM_THREADS='1',DOTNET_PROCESSOR_COUNT='4'))
    assert r.returncode==0 and 'TEST PASSED' in (target/'run.log').read_text(encoding='utf-8',errors='replace'),(target/'run.log').read_text(encoding='utf-8',errors='replace')[-4000:]
    a=(folder/'linux.inp').read_bytes();b=(target/'model.inp').read_bytes()
    summary[name]=dict(linux_bytes=len(a),windows_bytes=len(b),linux_sha256=hashlib.sha256(a).hexdigest(),windows_sha256=hashlib.sha256(b).hexdigest(),byte_identical=a==b,scope='partial DCB materials, section and sets; raw steps preserved' if name=='DCB' else 'old display fixture' if name=='old-DCB' else 'D2 strict P3')
    (logs/'roundtrip.json').write_text(json.dumps(summary,indent=2));assert a==b,summary[name]
    print('PASS byte-identical '+name,flush=True)
