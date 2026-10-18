"""Keep native SIGSEGV backtraces, continue CLR-handled signals, and require a real clean exit."""
from pathlib import Path
import hashlib, json, os, signal, subprocess as sp, sys, time

app, fixture, out = map(Path,sys.argv[1:4]); count=int(sys.argv[4]); out.mkdir(parents=True,exist_ok=True)
plain='--plain' in sys.argv
env=dict(os.environ,PPM_L2C_EVIDENCE='1',DOTNET_PROCESSOR_COUNT='2',LP_NUM_THREADS='2',OMP_NUM_THREADS='1')
script=out/'capture.gdb'
script.write_text('''set pagination off
set confirm off
set print thread-events off
handle SIG34 nostop noprint pass
handle SIG35 nostop noprint pass
handle SIGPWR nostop noprint pass
handle SIGSEGV stop print pass
python
import gdb
gdb.execute("run")
while gdb.selected_inferior().pid:
    gdb.execute("thread apply all bt 16")
    gdb.execute("continue")
end
''')
records=[]
for i in range(count):
    case=out/str(i+1);case.mkdir(exist_ok=True)
    for name in ('ready','command.json','state.json'):(case/name).unlink(missing_ok=True)
    log_path=case/('gui.log' if plain else 'gdb.log')
    with log_path.open('w') as log:
        launch=[] if plain else ['gdb','--batch','--return-child-result','-x',str(script),'--args']
        p=sp.Popen(launch+[str(app/'PrePoMax.Avalonia'),str(fixture),'--evidence',str(case)],cwd=case,env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
        ready=False;forced=False;graphics=[]
        try:
            start=time.monotonic()
            while p.poll() is None and time.monotonic()-start<60:
                if (case/'ready').exists():ready=True;break
                time.sleep(.1)
            if ready:
                if plain:
                    try:
                        maps=Path('/proc/'+str(p.pid)+'/maps').read_text()
                        graphics=sorted({line.split()[-1] for line in maps.splitlines() if any(n in line for n in ('d3d12','nvwgf2','swrast_dri','libGLX'))})
                    except OSError: pass
                (case/'command.tmp').write_text('{"op":"close"}');(case/'command.tmp').replace(case/'command.json')
            p.wait(timeout=30)
        except sp.TimeoutExpired:
            forced=True;os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
    text=log_path.read_text(errors='replace')
    debugger_error=not plain and ('Error in sourced command file' in text or "Couldn't read debug register" in text)
    record=dict(case=i+1,ready=ready,exit=p.returncode,forced=forced,segv=p.returncode==-signal.SIGSEGV or 'terminated with signal SIGSEGV' in text,graphics=graphics,
                signal_stops=text.count('received signal SIGSEGV'),debugger_error=debugger_error,
                clean=ready and not forced and p.returncode==0 and (plain or ('exited normally' in text and not debugger_error and 'received signal SIGSEGV' not in text)))
    records.append(record);print(record,flush=True)
    identity={name:hashlib.sha256((app/name).read_bytes()).hexdigest() for name in ('PrePoMax.Avalonia','PrePoMax.Avalonia.dll','libvtkmax.so.0.2.0','libSkiaSharp.so','libcoreclr.so') if (app/name).is_file()}
    (out/'summary.json').write_text(json.dumps(dict(app=str(app),mode='plain' if plain else 'gdb',identity=identity,records=records),indent=2))
print('DONE native shutdown probe',flush=True)
if '--require-clean' in sys.argv:
    assert all(r['clean'] for r in records), records
    assert all(not any('d3d12' in lib or 'nvwgf2' in lib for lib in r['graphics']) for r in records), records
