"""Reload each Linux form-edited PMX in the hidden Windows build; compare raw INP bytes."""
from pathlib import Path
import hashlib, json, os, subprocess as sp
repo = Path(__file__).resolve().parents[2]
logs = repo.parent/'logs/L3b2/forms'
summary = {}
for name in ('DCB','M2a'):
    folder = logs/('ui-'+name)
    target = folder/'windows-reload'; target.mkdir(exist_ok=True)
    command = [str(logs/'windows-runtime/PrePoMax.com'), '-f', str(folder/'edited.pmx'), '--script', str(repo/'tests/L3a/ExportBaseline.py'),
        '-g','No','-x','Yes','-w',str(target),'--report',str(target/'run.json'),'--noSettingsSave','--timeout','180']
    with (target/'run.log').open('wb') as log:
        result = sp.run(command, cwd=repo, stdout=log, stderr=sp.STDOUT, creationflags=0x08000000, timeout=210,
            env=dict(os.environ, OMP_NUM_THREADS='1', DOTNET_PROCESSOR_COUNT='4'))
    assert result.returncode==0 and 'TEST PASSED' in (target/'run.log').read_text(encoding='utf-8',errors='replace')
    linux = (folder/'linux.inp').read_bytes(); windows = (target/'model.inp').read_bytes()
    item = summary[name] = dict(linux_bytes=len(linux),windows_bytes=len(windows),linux_sha256=hashlib.sha256(linux).hexdigest(),windows_sha256=hashlib.sha256(windows).hexdigest(),byte_identical=linux==windows)
    (logs/'roundtrip.json').write_text(json.dumps(summary,indent=2))
    assert item['byte_identical'], item
print('PASS Windows reload of Linux form-edited DCB/M2a; INP byte-identical')
