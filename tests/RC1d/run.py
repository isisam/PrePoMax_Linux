"""Real net48 language units plus fresh-copy hidden startup/form-text checks."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, time

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--runtime',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    a=p.parse_args(); runtime=a.runtime.resolve(); out=a.output.resolve()
    repo=Path(__file__).resolve().parents[2]; out.mkdir(parents=True,exist_ok=True)
    env=dict(os.environ,DOTNET_PROCESSOR_COUNT='4',OMP_NUM_THREADS='1',MKL_NUM_THREADS='1',OPENBLAS_NUM_THREADS='1',PYTHONDONTWRITEBYTECODE='1',PYTHONIOENCODING='utf-8')
    results={}
    def run(name,command):
        start=time.time()
        with (out/(name+'.log')).open('wb') as log:
            proc=subprocess.run(list(map(str,command)),cwd=runtime,env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=0x08004000,timeout=180)
        results[name]=dict(exit=proc.returncode,seconds=round(time.time()-start,2))
        (out/'summary.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
        assert proc.returncode==0, (name, (out/(name+'.log')).read_text(encoding='utf-8',errors='replace')[-4000:])
    csc=Path(os.environ['ProgramFiles(x86)'])/'Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
    exe=runtime/'LanguageDefaults.exe'; config=runtime/'LanguageDefaults.exe.config'
    assert not exe.exists() and not config.exists()
    try:
        run('compile',[csc,'/nologo','/target:exe','/out:'+str(exe),'/r:'+str(runtime/'lib/PrePoMax.Core.dll'),'/r:'+str(runtime/'lib/CaeGlobals.dll'),repo/'tests/RC1d/LanguageDefaults.cs'])
        shutil.copy2(runtime/'PrePoMax.exe.config',config)
        run('units',[exe])
    finally:
        exe.unlink(missing_ok=True);config.unlink(missing_ok=True)
    assert not (runtime/'settings.bin').exists(), 'Use a fresh copy without settings.bin'
    fixture=runtime/'Examples/Layup/Layup.pmx'
    env.update(RC1D_LANGUAGE_OUT=str(out/'language.json'),RC1C_FIXTURE=str(fixture),RC1C_PROBE_OUT=str(out/'work_directory.json'))
    run('startup',[runtime/'PrePoMax.com','-f',fixture,'--script',repo/'tests/RC1d/FirstRunLanguage.py','-g','No','-x','Yes','--report',out/'startup_report.json','--timeout','120'])
    print('PASS language units, fresh form text and RC1c work-directory checks')

if __name__=='__main__':main()
