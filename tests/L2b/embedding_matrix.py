"""Hidden Xvfb and headless-Weston/XWayland matrix; never uses WSLg DISPLAY.
Latency is XTest injection -> observed projection confirmation, including 20ms
evidence sampling and file IPC, not scanout latency or a native GPU benchmark.
"""
from pathlib import Path
import ctypes as c,json,os,re,signal,statistics,subprocess as sp,sys,tempfile,time
import traceback
from PIL import Image,ImageChops
from xtest import PointerInjector
app=Path(sys.argv[1]);model=Path(sys.argv[2]);output=Path(sys.argv[3]);output.mkdir(parents=True,exist_ok=True)
provenance=(app/'SOURCE_REVISION.txt').read_text();header='git rev: '+provenance.splitlines()[0]+'\ngit status --short:\n'+'\n'.join(provenance.splitlines()[1:])+'\n'
def run(*args):return sp.check_output(list(map(str,args)),text=True,timeout=20).strip()
def wait_for(fn,timeout=45):
    start=time.monotonic()
    while time.monotonic()-start<timeout:
        result=fn()
        if result:return result
        time.sleep(.005)
    raise AssertionError('timeout: '+repr(fn))
def load(path):
    try:return json.loads(path.read_text())
    except (OSError,json.JSONDecodeError):return None
def close(process):
    if process.poll() is None:
        os.killpg(process.pid,signal.SIGTERM)
        try:process.wait(timeout=10)
        except sp.TimeoutExpired:os.killpg(process.pid,signal.SIGKILL);process.wait()
    if getattr(process,'parent_xvfb',None) is not None:close(process.parent_xvfb)
def server(kind):
    log=(output/(kind+'-server.log')).open('w');log.write(header);log.flush()
    env={**os.environ,'LIBGL_ALWAYS_SOFTWARE':'1','LP_NUM_THREADS':'8'}
    if kind=='xvfb':
        display=':97';p=sp.Popen(['Xvfb',display,'-screen','0','3000x2000x24','-nolisten','tcp','-noreset'],stdout=log,stderr=sp.STDOUT,env=env,start_new_session=True)
        env['DISPLAY']=display;env.pop('WAYLAND_DISPLAY',None)
        wait_for(lambda:sp.run(['xdotool','getdisplaygeometry'],env=env,stdout=sp.DEVNULL,stderr=sp.DEVNULL,timeout=5).returncode==0)
    else:
        # Weston's headless backend creates no input seat; Xwayland in Noble
        # crashes on XTest warp with a null seat. A Weston X11 backend nested
        # inside another hidden Xvfb supplies a real virtual seat and compositor.
        runtime=Path(tempfile.mkdtemp(prefix='ppm-l2b-wayland-'));runtime.chmod(0o700);env['XDG_RUNTIME_DIR']=str(runtime)
        parent=sp.Popen(['Xvfb',':96','-screen','0','3000x2000x24','-nolisten','tcp','-noreset'],stdout=log,stderr=sp.STDOUT,env=env,start_new_session=True)
        env['DISPLAY']=':96';wait_for(lambda:sp.run(['xdotool','getdisplaygeometry'],env=env,stdout=sp.DEVNULL,stderr=sp.DEVNULL,timeout=5).returncode==0)
        p=sp.Popen(['weston','--backend=x11-backend.so','--use-pixman','--socket=ppm-l2b','--idle-time=0','--width=3000','--height=2000','--xwayland'],stdout=log,stderr=sp.STDOUT,env=env,start_new_session=True)
        p.parent_xvfb=parent;env['SCREENSHOT_DISPLAY']=':96'
        def display_ready():
            try:text=(output/(kind+'-server.log')).read_text()
            except OSError:return None
            m=re.search(r'xserver listening on display (:\d+)',text);return m.group(1) if m else None
        env['DISPLAY']=wait_for(display_ready);env['WAYLAND_DISPLAY']='ppm-l2b'
    return p,log,env
receipts=[]
for server_kind in os.environ.get('PPM_MATRIX_SERVERS','xvfb,xwayland').split(','):
    service,service_log,server_env=server(server_kind)
    injector=PointerInjector(server_env['DISPLAY'])
    try:
        for backend in os.environ.get('PPM_MATRIX_BACKENDS','x11,generic').split(','):
            for scale in (float(s) for s in os.environ.get('PPM_MATRIX_SCALES','1,1.5,2').split(',')):
                case=output/f'{server_kind}-{backend}-{scale}';case.mkdir(exist_ok=True)
                for stale in ('state.json','ready','command.json','capture'): (case/stale).unlink(missing_ok=True)
                env={**server_env,'VTKMAX_EMBED':backend,'PPM_L2B_EVIDENCE':'1','AVALONIA_GLOBAL_SCALE_FACTOR':str(scale),'DOTNET_PROCESSOR_COUNT':'8'}
                with (case/'shell.log').open('w') as log:
                    log.write(header);log.flush()
                    p=sp.Popen([str(app/'run.sh'),str(model),'--evidence',str(case)],stdout=log,stderr=sp.STDOUT,env=env,start_new_session=True)
                    phase='ready'
                    try:
                        def ready():
                            if p.poll() is not None:raise AssertionError((case/'shell.log').read_text()[-4500:])
                            return (case/'ready').exists() and load(case/'state.json')
                        initial=wait_for(ready,90);assert abs(initial['scale']-scale)<.01,initial
                        candidates=sp.check_output(['xdotool','search','--onlyvisible','--pid',str(p.pid),'--name','Linux viewer prototype'],env=env,text=True).splitlines()
                        def area(window):
                            geometry=sp.check_output(['xdotool','getwindowgeometry','--shell',window],env=env,text=True)
                            values={k:int(v) for k,v in re.findall(r'^(WIDTH|HEIGHT)=(\d+)$',geometry,re.M)};return values['WIDTH']*values['HEIGHT']
                        main=int(max(candidates,key=area))
                        def xdo(*args):return sp.check_output(['xdotool',*map(str,args)],env=env,text=True,timeout=15).strip()
                        def pointer(window,x,y,click=False):
                            command=['mousemove','--window',window,x,y]
                            if click:command+=['click','1']
                            xdo(*command)
                        text=(case/'shell.log').read_text();m=re.search(r'EMBED parent=(\d+) child=(\d+)',text);child=int(m.group(2)) if m else main
                        def snapshot(name):sp.run(['import','-display',env.get('SCREENSHOT_DISPLAY',env['DISPLAY']),'-window','root',str(case/name)],check=True,timeout=20)
                        def command(**value):
                            tmp=case/'command.tmp';tmp.write_text(json.dumps(value));tmp.replace(case/'command.json');wait_for(lambda:not (case/'command.json').exists());time.sleep(.12)
                        def coords(s):
                            x,y=s['screen'][:2]
                            if backend=='x11':return round(x),round(s['height']*scale-1-y)
                            origin=s['origin'];return round(origin['X']*scale+x),round((origin['Y']+s['height'])*scale-1-y)
                        phase='pick';xdo('windowmove',main,10,40)
                        if server_kind=='xwayland':xdo('windowactivate',main)
                        xdo('windowfocus',main);time.sleep(.15)
                        x,y=coords(initial);snapshot('before-pick.png');(case/'geometry.txt').write_text(xdo('getwindowgeometry','--shell',child)+'\ncoords='+str((x,y)));pointer(child,x,y,click=True)
                        hit=wait_for(lambda:(s if (s:=load(case/'state.json')) and s['lastPick']['Hit']==1 else None))
                        assert hit['lastPick']['ElementId']==initial['expectedElement'] and hit['lastPick']['FaceId']==5,hit
                        # Return camera before wheel samples; no resize between before/after.
                        phase='wheel';command(op='front');latencies=[];projection=[]
                        for i in range(7):
                            before=wait_for(lambda:load(case/'state.json'));started=time.perf_counter();injector.click(4 if i%2==0 else 5)
                            after=wait_for(lambda:(s if (s:=load(case/'state.json')) and abs(s['screen'][0]-before['screen'][0])>.5 else None))
                            latencies.append((time.perf_counter()-started)*1000);projection.append([before['screen'],after['screen']])
                        # Host default VTK keys must not change camera, representation or exit.
                        phase='keys';before=wait_for(lambda:load(case/'state.json'));xdo('windowfocus',child);xdo('key','--window',child,'w','s','p','f','e','q');time.sleep(.25)
                        after=wait_for(lambda:load(case/'state.json'));assert p.poll() is None and max(abs(a-b) for a,b in zip(before['screen'],after['screen']))<1e-6
                        phase='overlays';snapshot('before-overlays.png')
                        pointer(main,round((initial['origin']['X']+initial['width']/2)*scale),round((initial['origin']['Y']+initial['height']/2)*scale))
                        command(op='tooltip',open=True);snapshot('tooltip.png');command(op='tooltip',open=False)
                        command(op='menu',open=True);snapshot('menu.png');command(op='menu',open=False)
                        base=Image.open(case/'before-overlays.png').convert('RGB')
                        changed={}
                        for name in ('tooltip','menu'):
                            diff=ImageChops.difference(base,Image.open(case/(name+'.png')).convert('RGB'));changed[name]=sum(1 for pixel in diff.getdata() if max(pixel)>30)
                            assert changed[name]>100,(case,name,changed)
                        phase='resize';geometry=xdo('getwindowgeometry','--shell',main)
                        native_size={key:int(value) for key,value in re.findall(r'^(WIDTH|HEIGHT)=(\d+)$',geometry,re.M)}
                        xdo('windowsize',main,native_size['WIDTH']-round(100*scale),native_size['HEIGHT']-round(80*scale))
                        resized=wait_for(lambda:(s if (s:=load(case/'state.json')) and abs(s['width']-initial['width'])>30 else None))
                        snapshot('resized.png');assert resized['width']>0 and resized['height']>0
                        phase='idle';time.sleep(.5);idle0=wait_for(lambda:load(case/'state.json'))['pumpCount'];time.sleep(.5);idle1=wait_for(lambda:load(case/'state.json'))['pumpCount'];assert idle0==idle1,(idle0,idle1)
                        receipt=dict(server=server_kind,backend=backend,scale=scale,overlayPopups=env.get('VTKMAX_OVERLAY_POPUPS','0'),passed=True,latencyMedianMs=statistics.median(latencies),latencyMaxMs=max(latencies),samples=latencies,wheelProjection=projection,exactFace=hit['lastPick']['FaceId'],focusAfterClick=hit['focus'],overlayChangedPixels=changed,resize=[resized['width'],resized['height']],idlePumpDelta=idle1-idle0,keySwallow=True,initial=initial)
                        (case/'receipt.json').write_text(json.dumps(receipt,indent=2));receipts.append(receipt);print('PASS',server_kind,backend,scale,'latency_ms',round(receipt['latencyMedianMs'],2),flush=True)
                        command(op='close');p.wait(timeout=15);assert p.returncode==0
                    except Exception as e:
                        receipt=dict(server=server_kind,backend=backend,scale=scale,passed=False,phase=phase,error=repr(e),traceback=traceback.format_exc());receipts.append(receipt)
                        (case/'receipt.json').write_text(json.dumps(receipt,indent=2));print('FAIL',receipt,flush=True)
                    finally:close(p)
    finally:injector.close();close(service);service_log.close()
(output/'matrix.json').write_text(json.dumps(receipts,indent=2))
print('EMBEDDING MATRIX:',sum(r['passed'] for r in receipts),'/',len(receipts),'passed')

raise SystemExit(int(any(not r['passed'] for r in receipts)))
