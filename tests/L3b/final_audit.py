"""Verify receipts and package bytes; keep L3 feature completion separate from regressions."""
from pathlib import Path
import hashlib,json,subprocess as sp
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L3b'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
gates=read(logs/'windows-gates/windows_gates/summary.json')
assert set(gates)=={'M1','M2a','M2b'} and all(r['exit']==0 for r in gates.values())
assert all(r['exit']==0 for r in read(logs/'windows-gates/summary.json').values())
assert all(r['exit']==0 for r in read(logs/'baseline-l1a.json').values())
full=read(logs/'full-pmx/acceptance.json');assert len(set(full['exports'].values()))==1
ui=read(logs/'gui3/inprocess_results.json');assert len(ui)==22 and all(r['status']=='PASS' for r in ui)
replay=read(logs/'gui3/replay_results.json');assert len(replay)==4 and all(r['passed'] and r['exit']==0 for r in replay.values())
for name in ('headless_run','contracts_run'):assert read(logs/'l10n'/(name+'.json'))['exitCode']==0
assert (logs/'l10n/language_en.inp').read_bytes()==(logs/'l10n/language_zh-TW.inp').read_bytes()
final=read(logs/'final/clean-package-summary.json');assert len(final)==3 and all(r['exit']==0 for r in final)
strong=read(logs/'final/tree-verified-summary.json');assert len(strong)==3 and all(r['exit']==0 for r in strong)
packages=read(logs/'packages-final/delivery-receipt.json');assert not packages['gitStatusShort']
for p in packages['packages']:
    f=logs/'packages-final'/p['name'];assert f.stat().st_size==p['bytes'] and sha(f)==p['sha256']
cases=[]
for r in final:
    root=logs/'final/clean'/r['distro']
    for kind in ('tar','deb'):
        check=read(root/(kind+'-tree-verified')/'acceptance.json');assert check['passed'] and check['exit']==0 and not check['forced']
        assert 'pointer selection replaces material with picked part' in check['checks']
        text=(root/(kind+'-tree-verified')/'gui.log').read_text(errors='replace');assert 'WSL renderer: llvmpipe.' in text
        cases.append(dict(distro=r['distro'],kind=kind,passed=True))
    stress=read(root/'shutdown-default/summary.json')['records'];assert len(stress)==4
    assert all(s['clean'] and s['exit']==0 and not s['forced'] and not s['segv'] for s in stress)
    assert all(not any('d3d12' in p or 'nvwgf2' in p for p in s['graphics']) for s in stress)
win_roots=('Cae','PrePoMax/','PrePoMax.Core/','PrePoMax.ViewModels/','PrePoMax.PropertyMetadata/','PrePoMax.L10n/','UserControls/','vtkControl/','ThirdParty/')
changed=sp.check_output(['git','diff','--name-only','1e9294c3','HEAD'],cwd=repo,creationflags=0x08000000).decode().splitlines()
assert not any(p.startswith(win_roots) or p=='PrePoMax.sln' for p in changed)
binary_log=(logs/'final-windows-build.log').read_bytes()
build_text=binary_log.decode('utf-16' if binary_log.startswith(b'\xff\xfe') else 'utf-8',errors='replace')
assert 'L3B_FINAL_WINDOWS_BUILD_EXIT=0' in build_text
coverage={}
for name in ('DCB','M2a'):
    coverage[name]=[r['kind'] for r in read(logs/('tree-'+name+'.json')) if r.get('passed')]
result=dict(packageSource=packages['gitRev'],packages=packages['packages'],windowsRegressions=gates,
    gui3Cases=len(ui),gui3Assertions=sum(len(r['assertions']) for r in ui),gui3ReplayCases=len(replay),
    l1bExportSha256=next(iter(full['exports'].values())),treeCommandCoverage=coverage,cleanPackageCases=cases,
    finalDefaultShutdownProbes=12,windowsProductionChangesAfterMerge=[],
    completion=dict(mergeBaseline=True,treeCommandMenus=True,treeViewportSync=True,editableFormHost=False,
        selectionCreatedRegions=False,uiUndoRedo=False,avaloniaEditKeywordViewer=False,l3EditedModelAcceptance=False,
        wslSoftwareMitigation=True,nativeGpuAcceptance=False))
(logs/'final-audit.json').write_text(json.dumps(result,indent=2))
print('PASS receipts, six actual packaged tree runs, twelve strict default closes, package hashes and unchanged Windows production source')
