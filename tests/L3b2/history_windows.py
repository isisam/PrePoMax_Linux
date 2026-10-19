"""Replay the Linux UI sequence independently on the hidden Windows host."""
from pathlib import Path
import hashlib,importlib.util,json,os,shutil,sys

repo=Path(__file__).resolve().parents[2]
folder,runtime,fixture=map(Path,sys.argv[1:4])
assert json.loads((folder/'acceptance.json').read_text())['passed']
assert not (folder/'windows-history.log').exists(),'Preserve the previous attempt'
os.environ.update(DOTNET_PROCESSOR_COUNT='4',OMP_NUM_THREADS='1',OPENBLAS_NUM_THREADS='1')
shutil.copy2(repo/'tests/L3b2/bin/Release/net48/SelectionWindows.exe',runtime/'SelectionWindows.exe')
shutil.copy2(repo/'PrePoMax/bin/x64/Release/PrePoMax.exe.config',runtime/'SelectionWindows.exe.config')
spec=importlib.util.spec_from_file_location('hidden_history',repo/'tests/M2bUi/run.py')
hidden=importlib.util.module_from_spec(spec);spec.loader.exec_module(hidden)
with (folder/'windows-history.log').open('wb') as log:
    code=hidden.private_desktop_run([str(runtime/'SelectionWindows.exe'),str(fixture),str(folder/'windows-source.json'),'--history',str(folder)],runtime,log,300)
if code:raise RuntimeError((folder/'windows-history.log').read_text(encoding='utf-8',errors='replace')[-5000:])
result=json.loads((folder/'windows-history-comparison.json').read_text())
assert result['passed'] and all(r['byte_identical'] and r['count_equal'] and not r['errors'] for r in result['results'])
assert len(result['results'])==27,result
receipts=[]
for row in result['results']:
    index=row['index'];a=(folder/f'linux-{index:03}.inp').read_bytes();b=(folder/f'windows-{index:03}.inp').read_bytes()
    assert a==b
    receipts.append(dict(index=index,operation=row['op'],commands=row['commandCount'],bytes=len(a),linux_sha256=hashlib.sha256(a).hexdigest(),windows_sha256=hashlib.sha256(b).hexdigest()))
(folder/'history-byte-receipt.json').write_text(json.dumps(dict(passed=True,states=receipts),indent=2),encoding='utf-8')
print('PASS Windows/Linux history: 27 states, raw INP bytes and command counts, independent commands')
