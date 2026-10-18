"""Export the exact same DCB and M2a PMX bytes through baseline/current Windows binaries."""
from pathlib import Path
import subprocess as sp,json,hashlib,os
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L3a';records={}
for name,model in [('DCB',repo.parent/'logs/L2b/dcb/dcb.pmx'),('M2a',repo.parent/'logs/M2b_acceptance/M2b_layup.pmx')]:
    rows={}
    for label,runtime in [('baseline',repo.parent/'logs/L2c/windows-runtime'),('current',logs/'windows-runtime')]:
        out=logs/'baseline-exports'/name/label;out.mkdir(parents=True,exist_ok=True)
        with (out/'run.log').open('wb') as f:
            f.write(b'Current source rev: '+sp.check_output(['git','rev-parse','HEAD'],cwd=repo,creationflags=0x08000000));f.flush()
            p=sp.run([str(runtime/'PrePoMax.com'),'-f',str(model),'--script',str(repo/'tests/L3a/ExportBaseline.py'),'-g','No','-x','Yes','-w',str(out),'--report',str(out/'run.json'),'--noSettingsSave','--timeout','120'],stdout=f,stderr=sp.STDOUT,creationflags=0x08000000,timeout=150,env={**os.environ,'OMP_NUM_THREADS':'1','DOTNET_PROCESSOR_COUNT':'4'})
        assert p.returncode==0 and 'TEST PASSED' in (out/'run.log').read_text(encoding='utf-8',errors='replace')
        data=(out/'model.inp').read_bytes();rows[label]={'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    assert rows['baseline']==rows['current'],rows
    records[name]=rows
(logs/'baseline-export-comparison.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records,indent=2))
