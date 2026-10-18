"""Windows capture on an explicitly created, never-switched desktop.
No input injection or capture of the owner's desktop. Uses existing Windows build.
"""
from pathlib import Path
import ctypes as c,hashlib,json,os,shutil,subprocess as sp,sys
sys.stdout.reconfigure(encoding='utf-8',errors='replace')
repo=Path(__file__).resolve().parents[2];logs=repo.parent/'logs/L2c';data=logs/'comparison';data.mkdir(exist_ok=True)
runtime=logs/'windows-runtime'
shutil.copy2(repo.parent/'logs/L1a_linux_handoff/data/dcb_fatczm_static.inp',data/'dcb.inp')
shutil.copy2(repo.parent/'logs/L2b/dcb/dcb.frd',data/'dcb.frd')
u=c.WinDLL('user32',use_last_error=True)
u.CreateDesktopW.argtypes=[c.c_wchar_p,c.c_wchar_p,c.c_void_p,c.c_uint32,c.c_uint32,c.c_void_p];u.CreateDesktopW.restype=c.c_void_p
u.CloseDesktop.argtypes=[c.c_void_p]
desktop_name='PPM_L2b_Capture_'+str(os.getpid())
desktop=u.CreateDesktopW(desktop_name,None,None,0,0x01ff,None)
if not desktop:raise c.WinError(c.get_last_error())
try:
    startup=sp.STARTUPINFO();startup.lpDesktop=desktop_name;startup.dwFlags|=sp.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    cmd=[str(runtime/'PrePoMax.com'),'--script',str(repo/'tests/L2b/windows_dcb.py'),'-g','No','-x','Yes','-w',str(data),'--report',str(data/'run.json'),'--noSettingsSave','--timeout','300']
    with (logs/'windows-hidden-capture.log').open('wb') as log:
        log.write(b'git rev: '+sp.check_output(['git','rev-parse','HEAD'],cwd=repo,creationflags=0x08000000)+b'git status --short:\n'+sp.check_output(['git','status','--short'],cwd=repo,creationflags=0x08000000))
        log.write(('desktop='+desktop_name+'; never switched\ncommand='+repr(cmd)+'\n').encode());log.flush()
        result=sp.run(cmd,cwd=runtime,env={**os.environ,'OMP_NUM_THREADS':'1','DOTNET_PROCESSOR_COUNT':'8'},stdout=log,stderr=sp.STDOUT,creationflags=0x08000000,startupinfo=startup,timeout=330)
    if result.returncode:raise RuntimeError('Hidden Windows capture exit '+str(result.returncode))
    report=json.loads((data/'run.json').read_text(encoding='utf-8-sig'));assert report['exitStatus']=='Ok',report
    images=list(data.glob('Windows-*.png'));assert len(images)==4,images
    (data/'capture-manifest.json').write_text(json.dumps(dict(desktop=desktop_name,switched=False,images={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in images},input=hashlib.sha256((data/'dcb.inp').read_bytes()).hexdigest(),frd=hashlib.sha256((data/'dcb.frd').read_bytes()).hexdigest()),indent=2))
    print('PASS hidden-desktop Windows DCB captures')
finally:u.CloseDesktop(desktop)
