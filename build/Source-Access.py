# SPDX-License-Identifier: GPL-3.0-or-later
"""Generate exact upstream source checkout instructions from restored package metadata."""
from pathlib import Path
import json,sys,xml.etree.ElementTree as ET
app=Path(sys.argv[1]);cache=Path.home()/'.nuget/packages'
deps=json.loads((app/'PrePoMax.Avalonia.deps.json').read_text())
rows=[]
for key,lib in deps['libraries'].items():
    if lib['type']!='package':continue
    name,version=key.rsplit('/',1);pkg=cache/name.lower()/version
    root=ET.parse(next(pkg.glob('*.nuspec'))).getroot()
    repos=[dict(e.attrib) for e in root.iter() if e.tag.split('}')[-1]=='repository']
    fallback={
      'Antlr':dict(url='https://www.antlr3.org/download/antlr-3.1.3.tar.gz',archive_sha256='1fca0fc7304140807e64ec263a58d93ef1b380cfb6407084e6909f8220bc1039',ref='source archive; runtime/CSharp'),
      'MicroCom.Runtime':dict(url='https://github.com/kekekeks/MicroCom',commit='7e773759cfc1012a5d080d4129c4718a662884b0'),
      'CommandLineParser':dict(url='https://github.com/commandlineparser/commandline'),
      'ncalc':dict(url='https://github.com/ncalc/ncalc',commit='34eeabf0b529110874fa1fcc33741a627d08e71a'),
      'OptimizedPriorityQueue':dict(url='https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp',commit='33f4fd3fcc69a30edf9dc61021efcdb7f0f2c850'),
    }
    if not repos:repos=[fallback[name]]
    elif name in fallback:repos[0]={**fallback[name],**repos[0]}
    if name=='MicroCom.Runtime':repos[0].update(fallback[name])
    if name.startswith('HarfBuzzSharp'):repos[0]['url']='https://github.com/mono/SkiaSharp'
    rows.append(dict(package=name,version=version,repository=repos))
runtime=json.loads((app/'PrePoMax.Avalonia.runtimeconfig.json').read_text())['runtimeOptions']['includedFrameworks'][0]['version']
rows.append(dict(package='.NET runtime',version=runtime,repository=[dict(url='https://github.com/dotnet/runtime',ref='v'+runtime)]))
(app/'SOURCE-REPOSITORIES.json').write_text(json.dumps(rows,indent=2))
text=['# Corresponding-source access','',
 'Keep prepomax-l2b-source.tar.gz beside both binary packages: it contains the project, vtkmax, VTK 9.4.2, local STL/ThreeMF sources and build/installation scripts.',
 'The bundled third-party package sources are also available from these upstream servers without charge (GPL section 6(d) source-access instructions). Repository commits below come from original NuGet metadata unless identified as a historical release tag. Use recursive submodules and the upstream dependency-fetch/build instructions, especially for native Skia/HarfBuzz. Archives of the project alone do not contain all upstream dependency sources.',
 'For each entry: git clone URL; git -C DIRECTORY checkout COMMIT_OR_REF; git -C DIRECTORY submodule update --init --recursive. Read upstream BUILDING/README for additional pinned source dependencies. Distributors must keep source access available when redistributing, including mirroring it if an upstream disappears. This is source-access information, not an invented three-year written offer.','',
 '| Package | Version | Repository | Commit/ref |','|---|---|---|---|']
for row in rows:
    for r in row['repository']:
        revision=r.get('commit',r.get('ref','NuGet metadata omits commit; upstream version '+row['version']))
        if r.get('archive_sha256'):revision+='; SHA256 '+r['archive_sha256']
        text.append('| '+row['package']+' | '+row['version']+' | '+r['url']+' | '+revision+' |')
(app/'SOURCE-ACCESS.md').write_text('\n'.join(text)+'\n')
print('PASS upstream source-access metadata:',len(rows),'packages/runtime')
