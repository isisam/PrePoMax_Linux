"""Real X11 drags/clicks into vtkmax; entities are created by the real form controls."""
from pathlib import Path
import json, os, signal, subprocess as sp, sys, time
from PIL import Image
app,fixture,out,snapshot=map(Path,sys.argv[1:5]);out.mkdir(parents=True,exist_ok=True)
assert not (out/'gui.log').exists(),'Preserve prior attempt'
windows=json.loads(snapshot.read_text(encoding='utf-8-sig'));events=[]
env=dict(os.environ,PPM_L2C_EVIDENCE='1',VTKMAX_EMBED='x11',DOTNET_PROCESSOR_COUNT='4',LP_NUM_THREADS='1',OMP_NUM_THREADS='1',OPENBLAS_NUM_THREADS='1')
def wait(fn,seconds=90):
    start=time.monotonic()
    while time.monotonic()-start<seconds:
        value=fn()
        if value:return value
        if p.poll() is not None:raise RuntimeError('shell exit '+str(p.returncode)+'\n'+(out/'gui.log').read_text(encoding='utf-8',errors='replace')[-5000:])
        time.sleep(.05)
    raise TimeoutError('selection UI wait')
def read_once():
    try:return json.loads((out/'state.json').read_text(encoding='utf-8'))
    except (OSError,json.JSONDecodeError):return None
def read():return wait(read_once)
def stamp():
    try:return (out/'state.json').stat().st_mtime_ns
    except OSError:return None
def command(**kw):
    old=wait(stamp)
    (out/'command.tmp').write_text(json.dumps(kw),encoding='utf-8');(out/'command.tmp').replace(out/'command.json')
    wait(lambda:not (out/'command.json').exists());time.sleep(.15)
    if kw['op']=='close':return
    # The shell removes a command before executing it, and publishes state
    # before dispatching commands. Require a new frame AFTER consumption.
    old=wait(stamp)
    wait(lambda:(stamp() or 0)>old)
    time.sleep(.15)
    if (out/'command-error.txt').exists():raise RuntimeError((out/'command-error.txt').read_text(encoding='utf-8'))
    result=read();events.append(dict(command=kw,form=result['form']));(out/'events.json').write_text(json.dumps(events,indent=2),encoding='utf-8');return result
def new(kind,type,name):
    category=next(e for e in read()['tree']['entries'] if e['view']=='Model' and e['Kind']==kind and e['entity'] is None)
    command(op='tree-action',key=category['Key'],action='New '+type+'…');wait(lambda:read().get('form'))
    command(op='form-position',x=1450,y=0)
    command(op='form-set',property='Name',value=name)
def click(button):return command(op='form-click',button=button)
def accept():
    click('OK');wait(lambda:read()['form'] is None)
def point(x,y):
    s=read();return round(s['origin']['X']*s['scale']+x),round((s['origin']['Y']+s['height'])*s['scale']-1-y)
def gesture(rect=None,pixel=None,modifiers=(),capture=False):
    s=read();sequence=s.get('nativeSequence',0);main=str(s['windowId']);sp.run(['xdotool','windowfocus',main],check=True)
    if modifiers:sp.run(['xdotool','keydown',*modifiers],check=True)
    try:
        if rect:
            x1,y1,x2,y2=rect;start=point(x2,y2);end=point(x1,y1)
            sp.run(['xdotool','mousemove','--window',main,*map(str,start),'mousedown','1'],check=True)
            time.sleep(.1);sp.run(['xdotool','mousemove','--window',main,*map(str,end)],check=True);time.sleep(.2)
            if capture:sp.run(['import','-window','root',str(out/'rubber-band.png')],check=True)
            sp.run(['xdotool','mouseup','1'],check=True)
        else:
            sp.run(['xdotool','mousemove','--window',main,*map(str,point(*pixel)),'click','1'],check=True)
    finally:
        if modifiers:sp.run(['xdotool','keyup',*modifiers],check=True)
    wait(lambda:read().get('nativeSequence',0)>sequence)
    time.sleep(.1);events.append(dict(real_x11=dict(rect=rect,pixel=pixel,modifiers=modifiers),form=read()['form']));(out/'events.json').write_text(json.dumps(events,indent=2),encoding='utf-8')
    return read()['form']
def regions():
    checks=[]
    camera_path=Path(sys.argv[sys.argv.index('--camera')+1])
    command(op='form-camera',**json.loads(camera_path.read_text(encoding='utf-8-sig'))['camera'])
    def canonical(cells):
        return sorted(tuple(sorted(tuple(round(v,8) for v in xyz) for xyz in cell)) for cell in cells)
    for expected in windows['records']:
        entry=next(e for e in read()['tree']['entries'] if e['Kind']==expected['kind'] and e['Caption']==expected['name'])
        command(op='tree-action',key=entry['Key'],action='Edit');command(op='form-position',x=1450,y=0)
        f=read()['form'];assert not f['error'] and not f['errors'],f
        assert f['highlightedNodes']==expected['nodes'] and f['highlightedFaces']==expected['faces'],(expected['name'],f)
        if expected['faces']:
            cells=[ [a['xyz'][i] for i in cell] for a in f['facePreview'] for cell in a['cells'] ]
            assert canonical(cells)==canonical(expected['drawnFaces']),(expected['name'],'Windows/Linux rendered face coordinates')
        if expected['point']:
            assert f['highlightedPoint']==expected['point'] and expected['drawnNodes'],(expected['name'],'reference point')
            assert all(max(abs(a-b) for a,b in zip(xyz,expected['point']))<1e-9 for xyz in expected['drawnNodes'])
        if expected['nodes']:assert len(expected['drawnNodes'])>=len(expected['nodes'])
        accept()
        checks.append(expected['name'])
    new('BoundaryCondition','Fixed','L3RegionPickedFixed')
    command(op='form-set',property='RegionType',value='Selection');command(op='form-select-mode',value='Node')
    f=gesture(rect=[0,0,999,699]);assert f['selection'] and f['highlightedNodes']==sorted(f['selection']);accept()
    new('Load','Pressure','L3RegionPickedPressure')
    command(op='form-set',property='Magnitude',value='1')
    command(op='form-set',property='RegionType',value='Selection');command(op='form-select-mode',value='Surface angle')
    command(op='form-angle',value='30 deg')
    f=gesture(pixel=(500,350));assert f['selection'] and f['highlightedFaces']==sorted(f['selection']);accept()
    checks.extend(['BC from real box selection','pressure from real surface-angle selection'])
    # Solid-section selection uses the same real panel as sets. The active M2a
    # layup must still reject an overlapping ordinary section before publishing.
    new('Section','Solid Section','L3SelectSection')
    command(op='form-set',property='RegionType',value='Selection')
    command(op='form-select-mode',value='Geometry part')
    f=gesture(rect=[0,0,999,699]);assert f['selection'],f
    if 'M2a' in str(fixture):
        click('OK');assert 'Overlaps ordinary section' in read()['form']['error'];click('Cancel')
        checks.append('active layup overlap rejected')
    else:
        accept();checks.append('solid section from real box selection')
    reopen=[('BoundaryCondition','L3RegionPickedFixed'),('Load','L3RegionPickedPressure')]
    if 'M2a' not in str(fixture):reopen.append(('Section','L3SelectSection'))
    for kind,name in reopen:
        entry=next(e for e in read()['tree']['entries'] if e['Kind']==kind and e['Caption']==name)
        before=read()['tree']['commandCount']
        command(op='tree-action',key=entry['Key'],action='Edit');command(op='form-position',x=1450,y=0)
        f=read()['form'];assert f['selection'] and f['history'],(name,'reopened creation selection')
        accept();assert read()['tree']['commandCount']==before,(name,'unchanged reopen must not Replace')
        checks.append(name+' selection history reopen without Replace')
    command(op='form-membership',path=str(out/'membership.json'))
    command(op='save',path=str(out/'edited.pmx'));wait(lambda:(out/'edited.pmx').exists())
    command(op='form-export',path=str(out/'linux.inp'));wait(lambda:(out/'linux.inp').exists())
    sp.run(['import','-window','root',str(out/'regions.png')],check=True)
    command(op='close');p.wait(timeout=20);assert p.returncode==0
    (out/'acceptance.json').write_text(json.dumps(dict(passed=True,checks=checks,real_windows_forms=True,real_linux_forms=True,exit=p.returncode,forced=False),indent=2),encoding='utf-8')
    print('PASS region membership, rendered faces/RP and solid section box selection')
with (out/'gui.log').open('w',encoding='utf-8') as log:
    p=sp.Popen([str(app/'PrePoMax.Avalonia'),str(fixture),'--non-interactive','--frd',str(out/'none.frd'),'--evidence',str(out)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists());read()
        if '--regions' in sys.argv:
            regions();raise SystemExit(0)
        command(op='form-camera',**windows['camera']);time.sleep(.6)
        new('NodeSet','Node set','L3SelectNodes');command(op='form-select-mode',value='Node')
        rect=[600,450,200,200];f=gesture(rect=rect,capture=True)
        assert len(f['selection'])>0,f
        assert sorted(f['selection'])==windows['cases'][1]['points'],('same display rectangle node IDs',f)
        assert f['history'][-1]['mouse']['CompletelyInside'] is True,f
        accept()
        new('NodeSet','Node set','L3SelectCrossNodes');command(op='form-select-mode',value='Node')
        f=gesture(rect=[200,200,600,450]);assert f['selection']
        assert f['history'][-1]['mouse']['CompletelyInside'] is False
        initial=set(f['selection'])
        f=gesture(rect=[999,699,0,0],modifiers=('Shift_L',));assert set(f['selection'])>=initial
        f=gesture(rect=rect,modifiers=('Control_L',));assert not set(f['selection'])&initial and f['selection'],f
        retained=set(f['selection']);history_count=len(f['history'])
        f=gesture(rect=[999,699,0,0],modifiers=('Shift_L','Control_L'))
        # Shared Controller splits area picks by part. Its intersection is applied
        # per part, so disjoint parts can intersect to empty on both platforms.
        # Compare that result with Windows, then undo this gesture before saving.
        for _ in range(len(f['history'])-history_count):command(op='form-selection',button='Undo last')
        assert set(read()['form']['selection'])==retained
        accept()
        new('ElementSet','Element set','L3SelectWindowElements');command(op='form-select-mode',value='Element')
        f=gesture(rect=rect);assert f['selection'],f
        accept()
        new('ElementSet','Element set','L3SelectElements');command(op='form-select-mode',value='Element')
        f=gesture(rect=[200,200,600,450]);assert f['selection'],f
        assert f['history'][-1]['mouse']['CompletelyInside'] is False,f
        accept()
        new('Surface','Surface','L3SelectSurface');command(op='form-select-mode',value='Surface angle');command(op='form-angle',value='30 deg')
        # Find a real hit with nonempty angle-selected face IDs. Every probe is a real X11 click.
        for pixel in [(500,350),(550,350),(450,350),(600,350),(400,350),(500,400),(500,300),(350,350)]:
            f=gesture(pixel=pixel)
            if f['selection']:break
        assert f['selection'] and f['selectAngle']==30,f
        assert f['highlightedFaces']==sorted(f['selection']),('face-level preview',f)
        preview=out/'angle-preview.png';sp.run(['import','-window','root',str(preview)],check=True)
        s=read();x=round(s['origin']['X']*s['scale']);y=round(s['origin']['Y']*s['scale'])
        colors=Image.open(preview).convert('RGB').crop((x,y,x+round(s['width']*s['scale']),y+round(s['height']*s['scale']))).getdata()
        assert sum(1 for r,g,b in colors if r>120 and g>70 and b<90 and r>g*1.1 and g>b*2)>10,'Native face highlight missing'
        angle_pixel=pixel
        accept()
        geometry_camera=dict(windows['camera']);geometry_camera['scale']/=4
        command(op='form-camera',**geometry_camera)
        new('Surface','Surface','L3SelectGeometry');command(op='form-select-mode',value='Geometry')
        for geometry_pixel in [angle_pixel,(500,350),(520,360),(480,340),(560,330),(440,370)]:
            f=gesture(pixel=geometry_pixel)
            if f['selection']:break
        assert f['selection'] and f['history'][-1]['type']=='SelectionNodeMouse',f
        accept()
        entry=next(e for e in read()['tree']['entries'] if e['Kind']=='Surface' and e['Caption']=='L3SelectGeometry')
        command(op='tree-action',key=entry['Key'],action='Edit');command(op='form-position',x=1450,y=0)
        assert read()['form']['selectBy']=='Geometry'
        command(op='form-geometry-mode',index=1);assert read()['form']['selection']==[]
        f=gesture(pixel=geometry_pixel);assert f['selection'] and f['history'][-1]['type']=='SelectionNodeIds',f
        accept()
        command(op='form-membership',path=str(out/'membership.json'))
        command(op='save',path=str(out/'edited.pmx'));wait(lambda:(out/'edited.pmx').exists())
        command(op='form-export',path=str(out/'linux.inp'));wait(lambda:(out/'linux.inp').exists())
        sp.run(['import','-window','root',str(out/'selection.png')],check=True)
        final=read();(out/'final.json').write_text(json.dumps(final,indent=2),encoding='utf-8')
        command(op='close');p.wait(timeout=20);assert p.returncode==0,p.returncode
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=p.returncode,forced=False,real_x11=True,angle_pixel=angle_pixel,
            checks=['node box Windows equality','window/crossing node and element forms','Shift add Ctrl subtract Shift+Ctrl intersect','surface-angle 30 degrees form','geometry history reopen and switch to ID','face-level preview','PMX save and INP export']),indent=2),encoding='utf-8')
        print('PASS real X11 box/angle forms and save/export')
    finally:
        if p.poll() is None:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
