"""Hidden Windows reload with raw byte comparison, without normalization."""
from pathlib import Path
import hashlib,json,os,subprocess as sp,sys
repo=Path(__file__).resolve().parents[2];logs=Path(os.environ.get('PPM_SELECT_LOGS',repo.parent/'logs/L3b2/select'))
runtime=Path(os.environ.get('PPM_SELECT_RUNTIME',logs/'windows-runtime'))
summary=json.loads((logs/'selection-roundtrip.json').read_text(encoding='utf-8')) if (logs/'selection-roundtrip.json').exists() else {}
for name in sys.argv[1:] or ['DCB','M2a']:
    folder=logs/('ui-'+name);target=folder/'windows-reload';target.mkdir(exist_ok=True)
    assert not (target/'run.log').exists(),'Preserve previous attempt'
    command=[str(runtime/'PrePoMax.com'),'-f',str(folder/'edited.pmx'),'--script',str(repo/'tests/L3b2/VerifySelection.py'),'-g','No','-x','Yes','-w',str(target),'--report',str(target/'run.json'),'--noSettingsSave','--timeout','180']
    with (target/'run.log').open('wb') as stream:
        p=sp.run(command,cwd=repo,stdout=stream,stderr=sp.STDOUT,creationflags=0x08000000,timeout=210,env=dict(os.environ,OMP_NUM_THREADS='1',DOTNET_PROCESSOR_COUNT='4'))
    assert p.returncode==0 and 'TEST PASSED' in (target/'run.log').read_text(encoding='utf-8',errors='replace'),(target/'run.log').read_text(encoding='utf-8',errors='replace')[-4000:]
    a=(folder/'linux.inp').read_bytes();b=(target/'model.inp').read_bytes()
    summary[name]=dict(byte_identical=a==b,bytes=len(a),linux_sha256=hashlib.sha256(a).hexdigest(),windows_sha256=hashlib.sha256(b).hexdigest())
    (logs/'selection-roundtrip.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    assert a==b,summary[name]
    print('PASS selection PMX reload/export '+name,flush=True)
