"""Read-only keyword UI, user steps, search, real key input and unchanged export."""
from pathlib import Path
source=Path(__file__).with_name('forms_ui.py').read_text(encoding='utf-8')
exec(compile(source[:source.index("with (out/'gui.log').open")],str(Path(__file__).with_name('forms_ui.py')),'exec'))
env['VTKMAX_EMBED']='x11'
with (out/'gui.log').open('w',encoding='utf-8') as log:
    p=sp.Popen([str(app/'PrePoMax.Avalonia'),str(fixture),'--non-interactive','--frd',str(out/'no-results.frd'),'--evidence',str(out)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        wait(lambda:(out/'ready').exists());count=read()['tree']['commandCount']
        command(op='form-export',path=str(out/'before.inp'));command(op='keyword-open');wait(lambda:read().get('keyword'))
        assert read()['keyword']['readOnly'] and read()['keyword']['users']>0 and not read()['keyword']['warnings']
        command(op='keyword-dump',path=str(out/'hidden.json'))
        command(op='keyword-hide',hide=False);command(op='keyword-dump',path=str(out/'full.json'))
        full=json.loads((out/'full.json').read_text());assert '*STEP' in full['deck'].upper() and '*STATIC' in full['deck'].upper()
        command(op='keyword-select-user',query='*STEP');assert read()['keyword']['selectedCaption']
        command(op='keyword-search',query='*STATIC');assert read()['keyword']['selectionLength']==7 and read()['keyword']['status'].startswith('Match')
        window=sp.check_output(['xdotool','search','--name','CalculiX keywords']).decode().split()[-1]
        sp.run(['xdotool','windowfocus',window,'key','ctrl+f'],check=True);sp.run(['xdotool','key','ctrl+a'],check=True)
        sp.run(['xdotool','type','--clearmodifiers','FATCZM'],check=True);sp.run(['xdotool','key','Return'],check=True)
        wait(lambda:read()['keyword']['query']=='FATCZM' and read()['keyword']['selectionLength']==6);assert read()['keyword']['status'].startswith('Match')
        sp.run(['import','-window','root',str(out/'keyword-viewer.png')],check=True)
        command(op='keyword-focus');sp.run(['xdotool','key','ctrl+a'],check=True);sp.run(['xdotool','type','--clearmodifiers','SHOULD_NOT_BE_INSERTED'],check=True);sp.run(['xdotool','key','Delete'],check=True)
        command(op='keyword-dump',path=str(out/'after-typing.json'))
        assert json.loads((out/'after-typing.json').read_text())['deck']==full['deck']
        command(op='keyword-close');assert read()['keyword'] is None
        command(op='form-export',path=str(out/'after.inp'));assert (out/'before.inp').read_bytes()==(out/'after.inp').read_bytes()
        assert read()['tree']['commandCount']==count
        command(op='close');p.wait(timeout=25);assert p.returncode==0
        (out/'acceptance.json').write_text(json.dumps(dict(passed=True,exit=0,forced=False,read_only=True,real_keyboard=True,search=True,user_steps=True,model_unchanged=True),indent=2))
        print('PASS AvaloniaEdit read-only keyword tree/deck, user steps, search and unchanged INP')
    finally:
        if p.poll() is None:
            try:command(op='close');p.wait(timeout=20)
            except Exception:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
