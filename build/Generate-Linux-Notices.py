#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-or-later
"""Fail-closed shipped-binary license inventory, driven by publish deps.json.
Preserve original package copyright/licenses, never invent license texts.
"""
from pathlib import Path
import argparse, hashlib, json, shutil, urllib.request, xml.etree.ElementTree as ET

ap=argparse.ArgumentParser()
ap.add_argument('app',type=Path); ap.add_argument('repo',type=Path)
ap.add_argument('vtk_source',type=Path); ap.add_argument('--cache',type=Path,default=Path.home()/'.cache/ppm-licenses')
a=ap.parse_args(); app,repo=a.app,a.repo
dst=app/'Licenses'; dst.mkdir(exist_ok=True); a.cache.mkdir(parents=True,exist_ok=True)
deps=json.loads((app/'PrePoMax.Avalonia.deps.json').read_text())
target=deps['targets'][deps['runtimeTarget']['name']]
nuget=Path.home()/'.nuget/packages'

def download(url):
    index_file=repo/'Licenses/linux-cache/index.json'
    if index_file.exists():
        index=json.loads(index_file.read_text())
        if url in index:
            entry=index[url];data=(index_file.parent/entry['file']).read_bytes()
            if hashlib.sha256(data).hexdigest()!=entry['sha256']:raise RuntimeError('Vendored license hash mismatch: '+url)
            return data.decode('utf-8-sig')
    cached=a.cache/hashlib.sha256(url.encode()).hexdigest()
    if not cached.exists():
        print('License source:',url,flush=True)
        req=urllib.request.Request(url,headers={'User-Agent':'PrePoMax-license-packager'})
        with urllib.request.urlopen(req,timeout=45) as r: data=r.read()
        if b'<html' in data.lower() or len(data)<80: raise RuntimeError('Not a license text: '+url)
        cached.write_bytes(data)
    return cached.read_text(encoding='utf-8-sig')

# Legacy packages have dead license URLs. These are original project sources.
fallback={
 'ncalc':'https://raw.githubusercontent.com/ncalc/ncalc/master/LICENSE',
 'optimizedpriorityqueue':'https://raw.githubusercontent.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/master/LICENSE.txt',
 'unitsnet':'https://raw.githubusercontent.com/angularsen/UnitsNet/UnitsNet/4.89.1/LICENSE',
 'microcom.runtime':'https://raw.githubusercontent.com/kekekeks/MicroCom/4b8a38f773c109bad558ee3713d9f16d80776e42/LICENSE',
 'tmds.dbus.protocol':'https://raw.githubusercontent.com/tmds/Tmds.DBus/rel/0.21.3/COPYING',
}
owners={}; rows=[]; records=[]
def add(name,version,files,text,source):
    filename=name.replace('/','_')+'-'+version+'.txt'
    (dst/filename).write_text(text,encoding='utf-8')
    rows.append(f'| {name} {version} | {", ".join(sorted(files))} | [{filename}]({filename}) | {source} |')
    for file in files: owners.setdefault(file,[]).append(filename)
    records.append(dict(component=name,version=version,files=sorted(files),license=filename,source=source))

for key,lib in deps['libraries'].items():
    if lib['type']!='package': continue
    name,ver=key.rsplit('/',1); pkg=nuget/name.lower()/ver
    entry=target.get(key,{})
    files=set()
    for kind in ('runtime','native','runtimeTargets'):
        for f in entry.get(kind,{}):
            if (app/Path(f).name).is_file(): files.add(Path(f).name)
    if not files: continue
    ns=next(pkg.glob('*.nuspec')); root=ET.parse(ns).getroot()
    meta={e.tag.split('}')[-1]:(e.text or '') for e in root.iter()}
    licenses=[f for f in pkg.iterdir() if f.is_file() and f.name.lower().startswith(('license','third-party','thirdparty','notice','copying'))]
    source='NuGet '+key+'; '+meta.get('projectUrl','')
    if licenses: text='\n\n'.join(f.read_text(encoding='utf-8-sig') for f in licenses)
    else:
        if name.lower().startswith('avalonia'):
            url='https://raw.githubusercontent.com/AvaloniaUI/Avalonia/11.3.22/licence.md'
        else: url=fallback.get(name.lower())
        if not url: raise RuntimeError('No original license for '+key)
        text=download(url); source+='; '+url
    add(name,ver,files,text,source)

runtime=json.loads((app/'PrePoMax.Avalonia.runtimeconfig.json').read_text())['runtimeOptions']['includedFrameworks'][0]['version']
pkg=nuget/'microsoft.netcore.app.runtime.linux-x64'/runtime
runtime_text='\n\n'.join(p.read_text(encoding='utf-8-sig') for p in pkg.iterdir() if p.name.upper() in ('LICENSE.TXT','THIRD-PARTY-NOTICES.TXT'))
assert runtime_text,'runtime license missing'
runtime_files={p.name for p in (pkg/'runtimes/linux-x64/native').glob('*') if (app/p.name).is_file()}
runtime_files|={p.name for p in (pkg/'runtimes/linux-x64/lib/net10.0').glob('*.dll') if (app/p.name).is_file()}
runtime_files-=owners.keys() # package overrides (e.g. compatibility formatter) have their own version/license
add('.NET-runtime',runtime,runtime_files,runtime_text,'https://github.com/dotnet/runtime/tree/v'+runtime)

gpl=(repo/'LICENSE').read_text(encoding='utf-8-sig')
assemblies=set()
for project in repo.glob('*/*.csproj'):
    names=[e.text for e in ET.parse(project).getroot().iter() if e.tag.split('}')[-1]=='AssemblyName']
    assemblies.add(names[0] if names else project.stem)
project_files={p.name for p in app.glob('*.dll') if p.stem in assemblies}
# QuantumConcepts is an upstream supplied reference, not part of .NET or GPL core.
quantum={p.name for p in app.glob('QuantumConcepts*.dll')}
if quantum:
    text=download('https://raw.githubusercontent.com/QuantumConcepts/STLdotNET/9c6d0e36e84901106634f2145306fadfe6be2457/LICENSE.txt')
    add('QuantumConcepts-STLdotNET','1.3.1',quantum,text,'https://github.com/QuantumConcepts/STLdotNET; original 2013 AGPL-3.0 license; local STL/ sources included')
project_files-=quantum
add('PrePoMax-RCM','L2b',project_files,gpl,'Corresponding-source archive supplied alongside binaries')
if (app/'ThreeMF.dll').exists():
    binding=(repo/'3MF/Lib3MF.cs').read_text(encoding='utf-8-sig').split('*/',1)[0]+'*/\n'
    add('ThreeMF-binding','2024',{'ThreeMF.dll'},binding,'3MF Consortium; generated Lib3MF.cs BSD-2-Clause notice')
vtktext=(a.vtk_source/'Copyright.txt').read_text()
# Static VTK includes its bundled third parties: preserve all original license files.
vtkl=dst/'VTK-third-party'; vtkl.mkdir(exist_ok=True)
for p in (a.vtk_source/'ThirdParty').rglob('*'):
    if p.is_file() and (p.name.lower().startswith(('license','copying','copyright','notice'))):
        target_path=vtkl/p.relative_to(a.vtk_source/'ThirdParty'); target_path.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(p,target_path)
add('VTK','9.4.2',{'libvtkmax.so.0.2.0'},vtktext+'\n\nInternal notices: VTK-third-party/\n','https://vtk.org/files/release/9.4/VTK-9.4.2.tar.gz')
add('vtkmax','0.2.0',{'libvtkmax.so.0.2.0'},(repo/'native/vtkmax/NOTICE.md').read_text()+gpl,'Corresponding-source archive supplied alongside binaries')

unknown=[]
for p in app.rglob('*'):
    if not p.is_file() or p.is_symlink() or dst in p.parents: continue
    if (p.suffix=='.dll' or '.so' in p.name) and p.name not in owners: unknown.append(str(p.relative_to(app)))
if unknown: raise RuntimeError('Unlicensed shipped binaries: '+', '.join(unknown))
(dst/'README.md').write_text('# Shipped binary licenses\n\n| Component/version | Shipped files | License | Source |\n|---|---|---|---|\n'+'\n'.join(rows)+'\n')
(app/'THIRD-PARTY-NOTICES.txt').write_text('\n\n'.join(r['component']+' '+r['version']+'\nSource: '+r['source']+'\nFiles: '+', '.join(r['files'])+'\n'+(dst/r['license']).read_text() for r in records))
(app/'license-inventory.json').write_text(json.dumps(records,indent=2))
(app/'CORRESPONDING-SOURCE.txt').write_text('GPL-3.0-or-later section 6: binaries are accompanied by prepomax-l2b-source.tar.gz containing PrePoMax, vtkmax, local third-party sources, build/installation scripts and the exact VTK source archive (section 6(a)). Upstream package sources are offered from the repository servers identified beside the binaries in SOURCE-ACCESS.md and SOURCE-REPOSITORIES.json (section 6(d)); the project archive does not contain all upstream dependency source trees. Anyone receiving these binaries may obtain, copy and redistribute that archive from the same delivery directory without additional charge. Keep this archive with redistributed binary packages. No contact address or three-year written offer is asserted. See SOURCE_REVISION.txt for revision and dirty status.\n')
print('PASS license coverage:',len(owners),'binary names,',len(records),'components')
