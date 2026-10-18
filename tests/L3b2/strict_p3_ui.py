"""L3b evidence transport; edits go through displayed form controls and routed events under Xvfb."""
from pathlib import Path
import json, os, signal, subprocess as sp, sys, time

app, fixture, out = map(Path, sys.argv[1:4])
out.mkdir(parents=True, exist_ok=True)
env = dict(os.environ, PPM_L2C_EVIDENCE='1', DOTNET_PROCESSOR_COUNT='4', LP_NUM_THREADS='1', OMP_NUM_THREADS='1')
records = []
for name in ('ready', 'state.json', 'command.json'):
    if (out/name).exists(): raise RuntimeError('Use a fresh output directory, preserve prior attempts: '+str(out))

def wait(fn, seconds=90):
    start = time.monotonic()
    while time.monotonic()-start < seconds:
        value = fn()
        if value: return value
        if p.poll() is not None: raise RuntimeError('shell exit '+str(p.returncode)+'\n'+(out/'gui.log').read_text()[-5000:])
        time.sleep(.07)
    raise TimeoutError('form UI wait')

def read_once():
    try: return json.loads((out/'state.json').read_text())
    except (OSError, json.JSONDecodeError): return None

def read(): return wait(read_once)
def stamp():
    try: return (out/'state.json').stat().st_mtime_ns
    except OSError: return None
def command(**kw):
    (out/'command.tmp').write_text(json.dumps(kw)); (out/'command.tmp').replace(out/'command.json')
    wait(lambda: not (out/'command.json').exists()); time.sleep(.3)
    if kw['op']=='close': return read_once()
    # Wait a subsequent evidence frame, after the control event and deferred descriptor refresh.
    old = wait(stamp)
    wait(lambda: (value if (value:=stamp()) and value != old else None))
    if (out/'command-error.txt').exists(): raise RuntimeError((out/'command-error.txt').read_text())
    state = read(); records.append(dict(command=kw, form=state.get('form'), commandCount=state['tree']['commandCount']))
    (out/'events.json').write_text(json.dumps(records, indent=2))
    return state

def entries(): return read()['tree']['entries']
def find(kind, name=None, category=False, step=None):
    return next(e for e in entries() if e['view']=='Model' and e['Kind']==kind and bool(e['entity']) != category
                and (name is None or e['Caption']==name) and (step is None or e['Step']==step))
def open_entity(e):
    command(op='tree-action', key=e['Key'], action='Edit'); return wait(lambda:read().get('form'))
def new(kind, type, step=None):
    e = find(kind, category=True, step=step)
    command(op='tree-action', key=e['Key'], action='New '+type+'…'); return wait(lambda:read().get('form'))
def click(name): return command(op='form-click',button=name)
def setprop(name,value,data=False): return command(op='form-set',property=name,value=str(value),data=data)
def close():
    click('OK'); assert read()['form'] is None, read()['form']
def name(value): return command(op='form-name',value=value)
def ids(value, action='Add IDs'):
    command(op='form-ids',value=value); return command(op='form-selection',button=action)


def ok():
    click('OK');assert read()['form'] is None,read()['form']
def noop(e):
    n=read()['tree']['commandCount'];open_entity(e);ok();assert read()['tree']['commandCount']==n,('unchanged OK',e)
def edit(kind,entity,prop,value,step=None):
    e=find(kind,entity,step=step);noop(e);open_entity(e);setprop(prop,value);ok();noop(find(kind,entity,step=step))
with (out/'gui.log').open('w') as log:
    p=sp.Popen([str(app/'PrePoMax.Avalonia'),str(fixture),'--non-interactive','--frd',str(out/'no-results.frd'),'--evidence',str(out)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists());(out/'initial.json').write_text(json.dumps(read(),indent=2))
        command(op='form-export',path=str(out/'initial.inp'));command(op='form-audit',path=str(out/'before.json'))
        if '--audit-only' not in sys.argv:
            dcb='--dcb' in sys.argv
            m=find('Material','IM7') if dcb else next(e for e in entries() if e['Kind']=='Material' and e['entity'])
            noop(m);f=open_entity(m);i=next(i for i,t in enumerate(f['pages']) if t in ('Engineering Constants','Lamina','Elastic'))
            command(op='form-page',index=i);prop=next(q['Name'] for q in read()['form']['properties'] if q['Name'].startswith('YoungsModulus'))
            setprop(prop,'161001' if dcb else '12345');ok();noop(find('Material',m['Caption']))
            if dcb:
                f=open_entity(find('Material','USERFATCZM'));command(op='form-page',index=f['pages'].index('Fatigue CZM (FATCZM)'));command(op='form-row',index=12)
                assert read()['form']['rows'][12].startswith('13. dDmax');assert next(q['value'] for q in read()['form']['data'] if q['Name']=='ValueEq')=='0.03'
                setprop('ValueEq','0.02',True);ok();noop(find('Material','USERFATCZM'))
                old={e['Caption'] for e in entries() if e['Kind']=='Material' and e['entity']};command(op='tree-action',key=find('Material','IM7')['Key'],action='Duplicate')
                dup=next(e for e in entries() if e['Kind']=='Material' and e['entity'] and e['Caption'] not in old)
                f=open_entity(dup);command(op='form-page',index=f['pages'].index('Engineering Constants'));setprop(prop,'161002');ok()
                audit=json.loads((out/'before.json').read_text());arm=next(s for s in audit['sections'] if s['region']=='ARMS');edit('Section',arm['Name'],'MaterialName',dup['Caption'])
            lines=(out/'initial.inp').read_text().splitlines();card=next(i for i,l in enumerate(lines) if l.upper().startswith('*ELEMENT,'));element=[int(v.strip()) for v in lines[card+1].split(',') if v.strip()];face=element[1:5]
            new('NodeSet','Node set');setprop('Name','StrictNodes');ids(' '.join(map(str,face)));command(op='form-selection',button='OK selection');ok();noop(find('NodeSet','StrictNodes'))
            new('ElementSet','Element set');setprop('Name','StrictElements');ids(str(element[0]));ok();noop(find('ElementSet','StrictElements'))
            new('Surface','Surface');setprop('Name','StrictSurface');setprop('CreateSurfaceFrom','NodeSet');setprop('NodeSetName','StrictNodes');ok();noop(find('Surface','StrictSurface'))
            if '--materials-only' not in sys.argv:
                if not dcb:
                    new('Step','Static step');setprop('Name','StrictStatic');setprop('SolverType','Spooles');setprop('IncrementationType','Automatic');ok()
                step='Step-1' if dcb else 'StrictStatic';edit('Step',step,'InitialTimeIncrement','0.1')
                new('BoundaryCondition','Fixed',step=step);setprop('Name','StrictFixed');setprop('RegionType','Node set name');setprop('NodeSetName','NCLAMP' if dcb else 'StrictNodes');ok();noop(find('BoundaryCondition','StrictFixed',step=step))
                new('Load','Concentrated force',step=step);setprop('Name','StrictForce');setprop('RegionType','Node set name');setprop('NodeSetName','StrictNodes');setprop('F2','0.001');ok();noop(find('Load','StrictForce',step=step))
        command(op='form-audit',path=str(out/'after.json'));command(op='save',path=str(out/'edited.pmx'));command(op='form-export',path=str(out/'linux.inp'))
        assert not read()['tree']['errors'];(out/'final.json').write_text(json.dumps(read(),indent=2))
        if '--dcb' not in sys.argv and '--audit-only' not in sys.argv:
            before=json.loads((out/'before.json').read_text());after=json.loads((out/'after.json').read_text());assert before['layups']==after['layups'] and all(l['Active'] for l in after['layups'])
        command(op='close');p.wait(timeout=30);assert p.returncode==0
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=0,forced=False,routed_controls=True,fixture=str(fixture),strict_edits='--audit-only' not in sys.argv and '--materials-only' not in sys.argv,partial_materials_sets='--materials-only' in sys.argv,strict_ui_edits_complete='--audit-only' not in sys.argv and '--materials-only' not in sys.argv,unchanged_OK_no_commands=True),indent=2));print(('PASS partial form operations ' if '--materials-only' in sys.argv else 'PASS form UI operations ')+str(fixture),flush=True)
    finally:
        if p.poll() is None:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=15)
