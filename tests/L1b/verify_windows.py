"""Run unchanged M1, M2a and M2b acceptance gates against an isolated Windows runtime."""
import argparse, json, os, subprocess, sys, time
from pathlib import Path

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exe',type=Path,required=True)
    parser.add_argument('--work',type=Path,required=True)
    args=parser.parse_args()
    repo=Path(__file__).resolve().parents[2]
    scripts=repo/'APIs/Python/Test_Scripts'
    args.work.mkdir(parents=True,exist_ok=True)
    commands=[
      ('M1',[sys.executable,'-B',str(scripts/'Run_Tests.py'),'--exe',str(args.exe),'--filter','Test_Model_User_Material.py','Test_Solver_Ccx_Fat','Test_Model_Materials_Sections.py','Audit_','test_runner_offline.py','--work',str(args.work/'M1')]),
      ('M2a',[sys.executable,'-B',str(scripts/'Run_Composite_Layup_Tests.py'),'--exe',str(args.exe),'--work',str(args.work/'M2a')]),
      ('M2b',[sys.executable,'-B',str(scripts/'Run_Composite_Layup_Editor_Tests.py'),'--exe',str(args.exe),'--work',str(args.work/'M2b')])]
    results={}
    for name,cmd in commands:
        start=time.time()
        with (args.work/(name+'.log')).open('wb') as log:
            result=subprocess.run(cmd,cwd=repo,env={**os.environ,'DOTNET_PROCESSOR_COUNT':'4','OMP_NUM_THREADS':'1'},stdout=log,stderr=subprocess.STDOUT,timeout=1800,**({'creationflags':0x08000000} if os.name=='nt' else {}))
        results[name]={'exit':result.returncode,'seconds':round(time.time()-start,2)}
        print(name,results[name],flush=True)
        if result.returncode:
            print((args.work/(name+'.log')).read_text(encoding='utf-8',errors='replace')[-3500:],flush=True)
    (args.work/'summary.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
    return int(any(r['exit'] for r in results.values()))
if __name__=='__main__':
    sys.stdout.reconfigure(encoding='utf-8',errors='replace')
    raise SystemExit(main())
