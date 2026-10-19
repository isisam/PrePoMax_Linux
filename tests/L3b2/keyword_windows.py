"""Snapshot the actual Windows keyword editor on a never-visible private desktop."""
from pathlib import Path
import importlib.util,os,shutil,sys
repo=Path(__file__).resolve().parents[2]
runtime,fixture,output=map(Path,sys.argv[1:4])
assert not output.exists(),'Preserve prior attempts'
os.environ.update(DOTNET_PROCESSOR_COUNT='4',OMP_NUM_THREADS='1')
helper=runtime/(output.stem+'.exe')
shutil.copy2(repo/'tests/L3b2/bin/Release/net48/SelectionWindows.exe',helper)
shutil.copy2(repo/'PrePoMax/bin/x64/Release/PrePoMax.exe.config',Path(str(helper)+'.config'))
spec=importlib.util.spec_from_file_location('hidden_keywords',repo/'tests/M2bUi/run.py')
hidden=importlib.util.module_from_spec(spec);spec.loader.exec_module(hidden)
with output.with_suffix('.log').open('wb') as log:
    code=hidden.private_desktop_run([str(helper),'--keywords',str(fixture),str(output)],runtime,log,180)
assert code==0,'Windows keyword form failed; see '+str(output.with_suffix('.log'))
print('PASS Windows keyword tree/deck snapshot (hidden/full)')
