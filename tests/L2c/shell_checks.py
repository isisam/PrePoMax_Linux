"""Actual Avalonia shell under private Xvfb; saved PMX reopened by shared core."""
from pathlib import Path
import json,os,subprocess as sp,sys,time,signal
from PIL import Image
repo=Path(__file__).resolve().parents[2]
out=Path(sys.argv[1]);case=Path(sys.argv[2]);case.mkdir(parents=True,exist_ok=True)
model=Path(sys.argv[3]);frd=Path(sys.argv[4])
def wait(fn,seconds=90):
    start=time.monotonic()
    while time.monotonic()-start<seconds:
        result=fn()
        if result:return result
        if p.poll() is not None:raise RuntimeError('shell exit '+str(p.returncode))
        time.sleep(.05)
    raise TimeoutError('shell evidence timeout')
def state():
    try:return json.loads((case/'state.json').read_text())
    except (OSError,json.JSONDecodeError):return None
def command(**kw):
    (case/'command.tmp').write_text(json.dumps(kw));(case/'command.tmp').replace(case/'command.json');wait(lambda:not(case/'command.json').exists());time.sleep(.25)
header='git rev: '+sp.check_output(['git.exe','-C',sp.check_output(['wslpath','-w',str(repo)],text=True).strip(),'rev-parse','HEAD'],text=True)+'git status --short:\n'+sp.check_output(['git.exe','-C',sp.check_output(['wslpath','-w',str(repo)],text=True).strip(),'status','--short'],text=True)
env={**os.environ,'PPM_L2C_EVIDENCE':'1','LD_LIBRARY_PATH':str(out/'app'),'LP_NUM_THREADS':'4','DOTNET_PROCESSOR_COUNT':'4'}
with (case/'gui.log').open('w') as log:
    log.write(header);log.flush()
    p=sp.Popen(['dotnet',str(out/'src/PrePoMax.Avalonia/bin/Release/net10.0/PrePoMax.Avalonia.dll'),str(model),'--frd',str(frd),'--evidence',str(case)],stdout=log,stderr=sp.STDOUT,env=env,start_new_session=True)
    try:
        wait(lambda:(case/'ready').exists());s=wait(state);assert s['actors']>0 and s['increment']==70,s
        command(op='save',path=str(case/'saved.pmx'));wait(lambda:(case/'save-done').exists());assert (case/'saved.pmx').stat().st_size>1000
        command(op='language',chinese=True);assert wait(state)['language']=='zh-TW';command(op='language',chinese=False)
        for field,name in [('S (Mises)','S'),('U (magnitude)','U')]:
            command(op='contour',field=field);command(op='front');command(op='png',path=str(case/(name+'.png')));wait(lambda:(case/(name+'.png')).exists())
        command(op='deform',scale=1);command(op='front');command(op='png',path=str(case/'U-deformed.png'));wait(lambda:(case/'U-deformed.png').exists())
        for name in ['S','U','U-deformed']:assert Image.open(case/(name+'.png')).getbbox()
        s=wait(state);(case/'results.json').write_text(json.dumps(dict(nodes=11580,elements=5100,step=s['step'],increment=s['increment'],time=s['time'],fields=s['fields']),indent=2))
        # X11 screen captures include menus, controller tree, log and units, not only VTK pixels.
        sp.run(['import','-window','root',str(case/'shell.png')],check=True,timeout=20)
        command(op='close');p.wait(timeout=20);assert p.returncode==0
        print('PASS actual shell: DCB PMX/FRD, per-part actors, S/U/deformed PNG, language switch, core PMX save')
    finally:
        if p.poll() is None:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
