"""Isisam L2a carry-overs: production evidence isolation, inherited launcher budget, idle evidence."""
from pathlib import Path
import json,os,signal,subprocess as sp,sys,tempfile,time
app=Path(sys.argv[1]);model=Path(sys.argv[2]);output=Path(sys.argv[3]);output.mkdir(parents=True,exist_ok=True)
env={**os.environ,'LP_NUM_THREADS':'4','DOTNET_PROCESSOR_COUNT':'4'}
for name in ('PPM_L2C_EVIDENCE','PPM_L2B_EVIDENCE'):env.pop(name,None)
target=Path(tempfile.mkdtemp(prefix='ppm-l2c-guard-'))/'disabled'
with (output/'disabled-evidence.log').open('w') as log:
    log.write('git rev: '+(app/'SOURCE_REVISION.txt').read_text().splitlines()[0]+'\ngit status --short:\n'+'\n'.join((app/'SOURCE_REVISION.txt').read_text().splitlines()[1:])+'\n');log.flush()
    p=sp.Popen([str(app/'run.sh'),str(model),'--evidence',str(target)],env=env,stdout=log,stderr=sp.STDOUT,start_new_session=True)
    try:
        start=time.monotonic()
        while 'FRD ready:' not in (output/'disabled-evidence.log').read_text():
            assert p.poll() is None,'shell exited';assert time.monotonic()-start<60,'open timed out';time.sleep(.1)
        time.sleep(3);assert not target.exists(),'evidence enabled without env guard'
        ticks=lambda:sum(int(x) for x in Path('/proc/'+str(p.pid)+'/stat').read_text().split()[13:15])
        before=ticks();time.sleep(1);after=ticks()
    finally:os.killpg(p.pid,signal.SIGTERM);p.wait(timeout=10)
for launcher in ('run.sh','prepomax-rcm'):
    text=(app/launcher).read_text();assert not any(name in text for name in ('DOTNET_PROCESSOR_COUNT','OMP_NUM_THREADS','LP_NUM_THREADS','VTK_SMP'))
receipt=dict(evidenceWithoutEnv=False,launchersInheritThreadBudget=True,idleCpuTicks=after-before,
    notes='X11/native pump delta zero and VTK default-key swallow are asserted by the embedding matrix; CPU ticks include runtime maintenance.')
(output/'receipt.json').write_text(json.dumps(receipt,indent=2));print('PASS evidence env isolation and uncapped production launchers; idle ticks',after-before)
