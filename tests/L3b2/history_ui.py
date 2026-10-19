"""Reuse the X11 form harness; verify every edit and history transition."""
from pathlib import Path

# Keep the existing transport, real X11 gestures, and no-overwrite guard.
source=(Path(__file__).with_name('selection_ui.py')).read_text(encoding='utf-8')
exec(compile(source[:source.index("with (out/'gui.log').open")],str(Path(__file__).with_name('selection_ui.py')),'exec'))
manifest=[]

def snapshot(operation,**data):
    state=read();index=len(manifest);path=out/f'linux-{index:03}.inp'
    assert not state['history']['busy'] and not state['history']['error'],state['history']
    assert not state['tree']['errors'],state['tree']['errors']
    command(op='form-export',path=str(path))
    manifest.append(dict(operation=operation,count=state['tree']['commandCount'],file=path.name,**data))
    (out/'history-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

with (out/'gui.log').open('w',encoding='utf-8') as log:
    p=sp.Popen([str(app/'PrePoMax.Avalonia'),str(fixture),'--non-interactive','--frd',str(out/'no-results.frd'),'--evidence',str(out)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists())
        # The supplied PMX contains generated mesh state beyond its three stored
        # commands. Save through the real UI first, exactly as on Windows, so
        # both sessions replay from a byte-preserved, usable save checkpoint.
        command(op='save',path=str(out/'baseline-linux.pmx'));snapshot('baseline')
        command(op='form-camera',**windows['camera'])
        material=next(e for e in read()['tree']['entries'] if e['Kind']=='Material' and e['entity'])
        def edit(value,keep=False):
            global page,prop
            if read()['form'] is None:
                command(op='tree-action',key=material['Key'],action='Edit')
            form=read()['form'];page=next(i for i,title in enumerate(form['pages']) if title in ('Engineering Constants','Elastic','Lamina'))
            command(op='form-page',index=page)
            prop=next(q['Name'] for q in read()['form']['properties'] if q['Name'].startswith('YoungsModulus'))
            command(op='form-set',property=prop,value=str(value));click('Apply' if keep else 'OK')
            snapshot('material',name=material['Caption'],property=prop,value=str(value))
        for value in range(12341,12345):edit(value)
        new('NodeSet','Node set','HistoryBoxNodes');command(op='form-select-mode',value='Node')
        form=gesture(rect=[200,200,600,450],capture=True)
        assert form['selection'] and form['history'][-1]['mouse']['PlaneParameters']
        selected=sorted(form['selection']);assert selected==windows['cases'][1]['points']
        selection=form['history'][-1]['mouse'];accept();snapshot('node-set',name='HistoryBoxNodes',ids=selected,selection=selection)
        for value in range(12345,12348):edit(value,keep=value==12347)
        sp.run(['import','-window','root',str(out/'form.png')],check=True)
        base=manifest[0]['count'];assert manifest[-1]['count']==base+8,manifest
        patterns=[(3,2),(4,1),(2,6)];active=8;responsive=False;keyboard=False;busy_pumps=[]
        edit_values=[12341,12342,12343,12344,None,12345,12346,12347]
        for pattern,(undo,redo) in enumerate(patterns):
            for action,times in [('undo',undo),('redo',redo)]:
                for _ in range(times):
                    before=read();sequence=before['history']['sequence'];pump=before['pumpCount']
                    # One real shortcut in each direction, including inside the open form.
                    if not keyboard or (keyboard=='undo' and action=='redo'):
                        ids=sp.check_output(['xdotool','search','--name','^Edit Material$']).decode().split()
                        sp.run(['xdotool','windowfocus',ids[-1]],check=True)
                        sp.run(['xdotool','key','ctrl+z' if action=='undo' else 'ctrl+y'],check=True)
                        keyboard='undo' if action=='undo' else True
                    else:command(op='history',redo=action=='redo')
                    def completed():
                        state=read()
                        if state['history']['error']:raise AssertionError(state['history']['error'])
                        if state['history']['busy']:busy_pumps.append(state['pumpCount'])
                        return state if state['history']['sequence']>sequence and not state['history']['busy'] else None
                    wait(completed)
                    active+=1 if action=='redo' else -1
                    state=read();assert state['tree']['commandCount']==base+active,(active,state['tree'])
                    assert state['form'] and state['form']['name']==material['Caption'],'Open form was not refreshed'
                    command(op='form-page',index=page)
                    displayed=next(q['value'] for q in read()['form']['properties'] if q['Name']==prop)
                    expected=next(v for v in reversed(edit_values[:active]) if v is not None)
                    assert float(displayed.split()[0])==expected,('Stale form after history',displayed,expected)
                    responsive=responsive or state['pumpCount']>pump
                    snapshot(action,pattern=pattern)
        assert keyboard is True and responsive and len(set(busy_pumps))>1
        click('Cancel');command(op='save',path=str(out/'edited.pmx'))
        command(op='close');p.wait(timeout=25);assert p.returncode==0
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=0,forced=False,edits=8,patterns=patterns,steps=len(manifest),real_shortcuts=True,refreshed_open_form=True,form_values_checked=True,event_pump_progress=responsive,busy_heartbeat_count=len(set(busy_pumps))),indent=2),encoding='utf-8')
        print('PASS history UI: 8 edits, 3 patterns, every state exported; Windows parity pending')
    finally:
        if p.poll() is None:
            # Preserve failure and ask the shell to exit normally before containment cleanup.
            try:command(op='close');p.wait(timeout=20)
            except Exception:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
