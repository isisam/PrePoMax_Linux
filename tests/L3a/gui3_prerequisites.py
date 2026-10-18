"""Compile the unchanged GUI3 in-process test against L2c and L3a, without launching UI."""
from pathlib import Path
import json,os,subprocess as sp
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L3a';flags=0x08000000
source=repo.parent/'work_m2c/tests/M2bUi'
vswhere=Path(os.environ['ProgramFiles(x86)'])/'Microsoft Visual Studio/Installer/vswhere.exe'
vs=sp.check_output([str(vswhere),'-latest','-products','*','-requires','Microsoft.Component.MSBuild','-property','installationPath'],creationflags=flags).decode().strip()
msbuild=Path(vs)/'MSBuild/Current/Bin/MSBuild.exe'
records={}
for label,runtime in [('L2c',repo.parent/'logs/L2c/windows-runtime'),('L3a',logs/'windows-runtime')]:
    out=logs/'gui3-prerequisites'/label;out.mkdir(parents=True,exist_ok=True)
    (out/'Program.cs').write_bytes((source/'Program.cs').read_bytes())
    (out/'App.config').write_bytes((source/'App.config').read_bytes())
    project=(source/'M2bUi.csproj').read_text().replace('../../PrePoMax/bin/x64/Release/',runtime.as_posix()+'/').replace('<OutputPath>'+runtime.as_posix()+'/</OutputPath>','<OutputPath>bin/</OutputPath>')
    if label=='L3a':
        project=project.replace('<Reference Include="PrePoMax.Core">','<Reference Include="PrePoMax.ViewModels"><HintPath>'+str(runtime/'lib/PrePoMax.ViewModels.dll')+'</HintPath><Private>false</Private></Reference><Reference Include="PrePoMax.PropertyMetadata"><HintPath>'+str(runtime/'lib/PrePoMax.PropertyMetadata.dll')+'</HintPath><Private>false</Private></Reference><Reference Include="PrePoMax.Core">')
    (out/'M2bUi.csproj').write_text(project,encoding='utf-8')
    with (out/'build.log').open('wb') as log:
        log.write(b'Application source git rev: '+sp.check_output(['git','rev-parse','HEAD'],cwd=repo,creationflags=flags))
        log.write(b'Application source status:\n'+sp.check_output(['git','status','--short'],cwd=repo,creationflags=flags));log.flush()
        p=sp.run([str(msbuild),str(out/'M2bUi.csproj'),'/t:Build','/p:Configuration=Release','/m:1','/nr:false','/v:minimal'],stdout=log,stderr=sp.STDOUT,creationflags=flags,timeout=180)
    lines=(out/'build.log').read_text(encoding='utf-8',errors='replace').splitlines()
    errors=[line for line in lines if ': error CS' in line]
    records[label]={'compile_exit':p.returncode,'errors':errors,'execution':'not launched'}
records['gate']='BLOCKED: GUI3 prerequisites absent from the requested L2c base' if all(records[k]['compile_exit'] for k in ['L2c','L3a']) else 'REVIEW REQUIRED'
(logs/'gui3-prerequisites.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(records['gate'])
raise SystemExit(0 if records['gate'].startswith('BLOCKED:') else 1)
