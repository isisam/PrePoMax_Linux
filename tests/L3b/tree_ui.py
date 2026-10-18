"""Exercise real Avalonia tree selections/menu routed events and native pointer input in Xvfb."""
from pathlib import Path
import json, os, signal, subprocess as sp, sys, time
from PIL import Image

app, fixture, out = map(Path, sys.argv[1:4]); out.mkdir(parents=True, exist_ok=True)
env = dict(os.environ, PPM_L2C_EVIDENCE='1', DOTNET_PROCESSOR_COUNT='4', LP_NUM_THREADS='2', OMP_NUM_THREADS='1')
for name in ('ready','state.json','command.json'): (out/name).unlink(missing_ok=True)

def read():
    try: return json.loads((out/'state.json').read_text())
    except (OSError, json.JSONDecodeError): return None

def wait(fn, seconds=60):
    start=time.monotonic()
    while time.monotonic()-start<seconds:
        value=fn()
        if value: return value
        if p.poll() is not None: raise RuntimeError('shell exit '+str(p.returncode)+'\n'+(out/'gui.log').read_text()[-4000:])
        time.sleep(.05)
    raise TimeoutError('tree UI wait')

def command(**kw):
    (out/'command.tmp').write_text(json.dumps(kw));(out/'command.tmp').replace(out/'command.json')
    wait(lambda:not(out/'command.json').exists());time.sleep(.2)

with (out/'gui.log').open('w') as log:
    p=sp.Popen([str(app/'PrePoMax.Avalonia'),str(fixture),'--evidence',str(out)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists()); initial=wait(read)
        (out/'initial.json').write_text(json.dumps(initial,indent=2))
        def entries(): return read()['tree']['entries']
        categories={e['Caption'] for e in entries() if e['entity'] is None and e['view']=='Model'}
        assert {'Mesh','Features','Materials','Composite Layups','Sections','Steps','Analyses'} <= categories
        material=next(e for e in entries() if e['Kind']=='Material' and e['entity'])
        before=read()['tree']['commandCount']
        command(op='tree-action',key=material['Key'],action='Duplicate')
        copy=wait(lambda:next((e for e in entries() if e['Kind']=='Material' and e['entity'] and e['Caption'].startswith(material['Caption']) and e['Caption']!=material['Caption']),None))
        assert read()['tree']['commandCount']==before+1
        command(op='tree-action',key=copy['Key'],action='Deactivate')
        assert next(e for e in entries() if e['Key']==copy['Key'])['Active'] is False
        command(op='tree-action',key=copy['Key'],action='Delete')
        assert not any(e['Key']==copy['Key'] for e in entries())
        part=next(e for e in entries() if e['view']=='Model' and e['Kind']=='Part' and e['entity'])
        command(op='tree-action',key=part['Key'],action='Hide')
        assert next(e for e in entries() if e['Key']==part['Key'])['Visible'] is False
        command(op='tree-action',key=part['Key'],action='Show')
        command(op='tree-select',key=part['Key'])
        assert read()['tree']['selected']==part['Key']
        png=out/'highlight.png';command(op='png',path=str(png));wait(png.exists)
        colors=Image.open(png).convert('RGB').getdata()
        assert sum(1 for r,g,b in colors if r>170 and 100<g<230 and b<100)>3, 'Native yellow selection highlight missing'
        command(op='tree-tab',index=0);assert read()['tree']['tab']==0
        command(op='tree-tab',index=2);assert read()['tree']['tab']==2
        command(op='tree-tab',index=1)
        # Return to deterministic camera, then exercise a real native left click.
        command(op='front');time.sleep(.5);s=read()
        main=str(s['windowId']);assert int(main)>0
        origin=s['origin'];x=round(origin['X']*s['scale']+s['screen'][0]);y=round((origin['Y']+s['height'])*s['scale']-1-s['screen'][1])
        sp.run(['xdotool','windowfocus',main,'mousemove','--window',main,str(x),str(y),'click','1'],check=True)
        picked=wait(lambda:(s if (s:=read()) and s['lastPick']['Hit']==1 else None))
        assert picked['tree']['selected'] and '/Part/' in picked['tree']['selected']
        assert not picked['tree']['errors']
        sp.run(['import','-window','root',str(out/'tree.png')],check=True)
        (out/'final.json').write_text(json.dumps(picked,indent=2))
        command(op='close');p.wait(timeout=20);assert p.returncode==0,p.returncode
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=p.returncode,forced=False,checks=['categories','menu commands','command count','persisted visibility','native highlight','three tabs','pointer selection sync']),indent=2))
        print('PASS actual Avalonia tree menus, model state, VTK highlight and pointer/tree sync')
    finally:
        if p.poll() is None: os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
