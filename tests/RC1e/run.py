"""Run native result switching on a private desktop; validate contour-only image contribution."""
import argparse, hashlib, json, os, subprocess, sys, time
from pathlib import Path
from PIL import Image, ImageChops

def main():
    p=argparse.ArgumentParser()
    p.add_argument('--runtime',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    p.add_argument('--sources',type=Path,required=True,help='JSON mapping DCB/ENF/Layup to FRD paths')
    p.add_argument('--repo',type=Path,default=Path(__file__).resolve().parents[2])
    a=p.parse_args();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
    runtime=a.runtime.resolve();repo=a.repo.resolve()
    sys.path.insert(0,str(repo/'tests/M2bUi'))
    from run import private_desktop_run
    env=dict(os.environ,OMP_NUM_THREADS='1',OPENBLAS_NUM_THREADS='1',MKL_NUM_THREADS='1',DOTNET_PROCESSOR_COUNT='4',
             RC1E_OUTPUT=str(out),RC1E_SOURCES=str(a.sources.resolve()),PYTHONDONTWRITEBYTECODE='1')
    os.environ.update(env)
    csc=Path(os.environ['ProgramFiles(x86)'])/'Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
    with (out/'compile.log').open('wb') as log:
        code=subprocess.run([str(csc),'/nologo','/target:library','/r:System.Web.Extensions.dll',
            '/out:'+str(out/'SettingsChoices.dll'),str(repo/'tests/RC1e/SettingsChoices.cs')],
            env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=0x08000000).returncode
    assert code==0,(out/'compile.log').read_text(errors='replace')
    fixture=runtime/'Examples/Layup/Layup.pmx'
    command=[str(runtime/'PrePoMax.com'),'-f',str(fixture),'--script',str(repo/'tests/RC1e/ResultsLanguage.py'),
        '-g','No','-x','Yes','-w',str(out),'--report',str(out/'host.json'),'--timeout','600']
    start=time.time()
    with (out/'native.log').open('wb') as log:code=private_desktop_run(command,runtime,log,660)
    summary=dict(exit=code,seconds=round(time.time()-start,2),private_desktop=True,never_switched=True,command=command,
        binary_sha256=hashlib.sha256((runtime/'PrePoMax.exe').read_bytes()).hexdigest())
    (out/'launch.json').write_text(json.dumps(summary,indent=2))
    print(json.dumps(summary),flush=True)
    assert code==0,(out/'native.log').read_text(encoding='utf-8',errors='replace')[-7000:]
    data=json.loads((out/'results.json').read_text(encoding='utf-8'))
    assert data['status']=='PASS' and len(data['records'])==9,data.get('errors')
    image_checks=[]
    for record in data['records']:
        with Image.open(out/record['image']) as full,Image.open(out/record['empty_control']) as empty:
            difference=ImageChops.difference(full.convert('RGB'),empty.convert('RGB'))
            pixels=sum(1 for rgb in difference.getdata() if max(rgb)>15)
            assert pixels>500,(record['image'],pixels,'only axes/legend or blank render')
            image_checks.append(dict(image=record['image'],geometry_pixels=pixels,width=full.width,height=full.height))
    (out/'image_checks.json').write_text(json.dumps(image_checks,indent=2))
    print('PASS 9 native renders with independent empty controls and matching field/legend extrema')

if __name__=='__main__':main()
