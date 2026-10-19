"""Final R3 regression: typed Step-1 edit, Fixed BC and concentrated force."""
from pathlib import Path
source=Path(__file__).with_name('forms_ui.py').read_text(encoding='utf-8')
exec(compile(source[:source.index("with (out/'gui.log').open")],str(Path(__file__).with_name('forms_ui.py')),'exec'))
with (out/'gui.log').open('w',encoding='utf-8') as log:
    p=sp.Popen([str(app/'PrePoMax.Avalonia'),str(fixture),'--non-interactive','--frd',str(out/'none.frd'),'--evidence',str(out)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists());command(op='form-export',path=str(out/'initial.inp'))
        command(op='form-audit',path=str(out/'before.json'))
        open_entity(find('Step','Step-1'));setprop('IncrementationType','Automatic');setprop('InitialTimeIncrement','0.1');close()
        new('NodeSet','Node set');setprop('Name','StrictNodes');ids('1 2 3 4');close()
        new('BoundaryCondition','Fixed',step='Step-1');setprop('Name','StrictFixed');setprop('RegionType','Node set name');setprop('NodeSetName','StrictNodes');close()
        new('Load','Concentrated force',step='Step-1');setprop('Name','StrictForce');setprop('RegionType','Node set name');setprop('NodeSetName','StrictNodes');setprop('F2','0.001');close()
        command(op='form-audit',path=str(out/'after.json'));command(op='save',path=str(out/'edited.pmx'));wait(lambda:(out/'save-done').exists())
        command(op='form-export',path=str(out/'linux.inp'));assert not read()['tree']['errors']
        command(op='close');p.wait(timeout=25);assert p.returncode==0
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=0,forced=False,typed_Step1=True,fixed=True,force=True,real_controls=True),indent=2))
        print('PASS old DCB edited Step-1, Fixed and concentrated force, save/export')
    finally:
        if p.poll() is None:
            try:command(op='close');p.wait(timeout=20)
            except Exception:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
