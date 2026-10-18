"""Capture real ActiViz frustum results on the existing never-visible private desktop."""
from pathlib import Path
import importlib.util, json, os, shutil, subprocess as sp
repo=Path(__file__).resolve().parents[2]
logs=Path(os.environ.get('PPM_SELECT_LOGS',repo.parent/'logs/L3b2/select'))
logs.mkdir(parents=True,exist_ok=True)
def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path);obj=importlib.util.module_from_spec(spec);spec.loader.exec_module(obj);return obj
baseline=module('select_baseline',repo/'tests/L3b/baseline.py')
baseline.logs=logs;baseline.runtime=logs/'windows-runtime';baseline.assemble()
runtime=baseline.runtime
shutil.copy2(repo/'tests/L3b2/bin/Release/net48/SelectionWindows.exe',runtime/'SelectionWindows.exe')
shutil.copy2(repo/'PrePoMax/bin/x64/Release/PrePoMax.exe.config',runtime/'SelectionWindows.exe.config')
hidden=module('select_hidden',repo/'tests/M2bUi/run.py')
results={}
for name,fixture in [('DCB',repo.parent/'logs/L2b/dcb/dcb.pmx'),('M2a',repo.parent/'logs/M2b_acceptance/M2b_layup.pmx')]:
    with (logs/f'windows-selection-{name}.log').open('wb') as log:
        code=hidden.private_desktop_run([str(runtime/'SelectionWindows.exe'),str(fixture),str(logs/f'windows-selection-{name}.json')],runtime,log,180)
    results[name]=dict(exit=code,private_desktop=True)
    (logs/'windows-selection-run.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
    if code: raise RuntimeError((logs/f'windows-selection-{name}.log').read_text(encoding='utf-8',errors='replace'))
print('PASS real Windows selection snapshots')
