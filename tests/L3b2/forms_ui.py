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
    # Wait a subsequent evidence frame, after the control event and deferred descriptor refresh.
    old = wait(stamp)
    wait(lambda: (value if (value:=stamp()) and value != old else None))
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

with (out/'gui.log').open('w') as log:
    p = sp.Popen([str(app/'PrePoMax.Avalonia'), str(fixture), '--evidence', str(out)], env=env, stdout=log, stderr=sp.STDOUT, start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists()); initial=read(); (out/'initial.json').write_text(json.dumps(initial,indent=2))
        # Existing material elasticity, including the layup engineering-constant path.
        elastic_done = False; fat_done = False
        materials = [e for e in entries() if e['Kind']=='Material' and e['entity']]
        for material in materials:
            f = open_entity(material)
            if not elastic_done:
                for i, title in enumerate(f['pages']):
                    if title in ('Elastic','Engineering Constants','Elastic with density'):
                        command(op='form-page',index=i)
                        prop=next(q['Name'] for q in read()['form']['properties'] if q['Name'].startswith('YoungsModulus'))
                        setprop(prop,'12345'); assert not read()['form']['errors']; click('Apply'); assert not read()['form']['error']; elastic_done=True; break
            for i,title in enumerate(f['pages']):
                if title=='Fatigue CZM (FATCZM)':
                    command(op='form-page',index=i); command(op='form-row',index=12); setprop('ValueEq','0.02',True)
                    click('Apply'); assert not read()['form']['error']; fat_done=True
            click('Cancel')
        assert elastic_done, 'Fixture needs an elastic property'
        if not fat_done:
            new('Material','Material'); name('USERFATCZM_L3Forms')
            command(op='form-material-type',value='Fatigue CZM (FATCZM)'); click('Add property'); command(op='form-row',index=12)
            assert read()['form']['rows'][12].startswith('13. dDmax')
            sp.run(['import','-window','root',str(out/'fatczm-form.png')],check=True)
            setprop('ValueEq','0.02',True); close()
        # Real ID panel operations, including rejection without changing prior selection.
        new('NodeSet','Node set'); setprop('Name','L3FormsNodes')
        ids('1 2 3 4'); ids('4','Remove IDs'); command(op='form-selection',button='Undo last')
        prior=read()['form']['selection']; ids('999999999'); assert read()['form']['selection']==prior
        command(op='form-selection',button='Clear'); ids('1 2 3 4'); command(op='form-selection',button='OK selection')
        assert read()['form']['selection']==[1,2,3,4]; close(); find('NodeSet','L3FormsNodes')
        new('ElementSet','Element set'); setprop('Name','L3FormsElements'); ids('1'); close(); find('ElementSet','L3FormsElements')
        new('Surface','Surface'); setprop('Name','L3FormsSurface'); setprop('CreateSurfaceFrom','NodeSet'); setprop('NodeSetName','L3FormsNodes'); close(); find('Surface','L3FormsSurface')
        new('Section','Solid Section'); setprop('Name','L3FormsSection'); setprop('RegionType','Element set name'); setprop('ElementSetName','L3FormsElements'); close(); find('Section','L3FormsSection')
        command(op='save',path=str(out/'checkpoint-pre-steps.pmx'))
        new('Step','Static step'); setprop('Name','L3FormsRamp'); setprop('SolverType','Spooles'); close(); find('Step','L3FormsRamp')
        new('Step','Static step'); setprop('Name','L3FormsFatigue'); click('FATCZM fatigue step')
        f=read()['form']; assert next(q['value'] for q in f['properties'] if q['Name']=='MaxIncrements')=='310'
        assert next(q['value'] for q in f['properties'] if q['Name']=='IncrementationType')=='Direct'; close()
        new('BoundaryCondition','Fixed',step='L3FormsRamp'); setprop('Name','L3FormsFixed'); setprop('RegionType','Node set name'); setprop('NodeSetName','L3FormsNodes'); close()
        new('Load','Concentrated force',step='L3FormsRamp'); setprop('Name','L3FormsForce'); setprop('RegionType','Node set name'); setprop('NodeSetName','L3FormsNodes'); setprop('F2','1'); close()
        command(op='save',path=str(out/'edited.pmx')); wait(lambda:(out/'edited.pmx').exists())
        command(op='form-export',path=str(out/'linux.inp')); assert (out/'linux.inp').exists()
        sp.run(['import','-window','root',str(out/'tree.png')],check=True)
        assert not read()['tree']['errors']; final=read(); (out/'final.json').write_text(json.dumps(final,indent=2))
        command(op='close'); p.wait(timeout=25); assert p.returncode==0,p.returncode
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=p.returncode,forced=False,fixture=str(fixture),checks=['elastic edit','FATCZM dDmax','ID node/element sets','node-set surface','solid section','static ramp','typed fatigue preset 300','Fixed BC','force','save PMX','export INP','tree after commands']),indent=2))
        print('PASS Linux real form host: '+str(fixture),flush=True)
    finally:
        if p.poll() is None: os.killpg(p.pid,signal.SIGTERM); p.wait(timeout=15)
