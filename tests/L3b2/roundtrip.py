"""Reload each Linux form-edited PMX in the hidden Windows build; compare raw INP bytes."""
from pathlib import Path
import hashlib, json, os, subprocess as sp, sys
repo = Path(__file__).resolve().parents[2]
logs = Path(os.environ.get('PPM_FORMS_LOGS', repo.parent/'logs/L3b2/forms'))
summary = json.loads((logs/'roundtrip.json').read_text()) if (logs/'roundtrip.json').exists() else {}
for name in sys.argv[1:] or ('DCB','M2a'):
    folder = logs/('ui-'+name)
    target = folder/'windows-reload'; target.mkdir(exist_ok=True)
    command = [str(logs/'windows-runtime/PrePoMax.com'), '-f', str(folder/'edited.pmx'), '--script', str(repo/'tests/L3b2/VerifyForms.py'),
        '-g','No','-x','Yes','-w',str(target),'--report',str(target/'run.json'),'--noSettingsSave','--timeout','180']
    with (target/'run.log').open('wb') as log:
        result = sp.run(command, cwd=repo, stdout=log, stderr=sp.STDOUT, creationflags=0x08000000, timeout=210,
            env=dict(os.environ, OMP_NUM_THREADS='1', DOTNET_PROCESSOR_COUNT='4'))
    assert result.returncode==0 and 'TEST PASSED' in (target/'run.log').read_text(encoding='utf-8',errors='replace')
    linux = (folder/'linux.inp').read_bytes(); windows = (target/'model.inp').read_bytes()
    item = summary[name] = dict(linux_bytes=len(linux),windows_bytes=len(windows),linux_sha256=hashlib.sha256(linux).hexdigest(),windows_sha256=hashlib.sha256(windows).hexdigest(),byte_identical=linux==windows)
    # The Windows batch entry point assigns UNIT_LESS to an Undefined model.
    # Keep original files/hashes and account for every differing byte explicitly.
    explained = linux!=windows and linux.replace(b'Unit system: Undefined',b'Unit system: UNIT_LESS',1)==windows
    item['explained_header_only'] = explained
    if explained:
        item['explanation'] = 'Windows batch reports and assigns UNIT_LESS for the source Undefined unit system; only the heading text changes.'
        item['differing_bytes'] = [dict(offset=i,linux=a,windows=b) for i,(a,b) in enumerate(zip(linux,windows)) if a!=b]
    (logs/'roundtrip.json').write_text(json.dumps(summary,indent=2))
    assert item['byte_identical'] or explained, item
print('PASS Windows reload and INP comparison; original hashes and any heading-only difference are recorded')
