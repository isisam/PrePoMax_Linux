"""Prove Windows full PMX -> Linux net8 replay/roundtrip -> Windows, with exact INP hashes."""
import argparse,hashlib,json,os,shutil,subprocess,sys,struct,zlib
from pathlib import Path

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--exe',type=Path,required=True)
    parser.add_argument('--headless-linux',type=Path,required=True)
    parser.add_argument('--work',type=Path,required=True)
    args=parser.parse_args()
    repo=Path(__file__).resolve().parents[2]
    work=args.work.resolve();work.mkdir(parents=True,exist_ok=True)
    shutil.copy2(repo/'APIs/Python/Test_Scripts/Models/cube.inp',work/'fixture.inp')
    records={}
    def run(label,command):
        result=subprocess.run(list(map(str,command)),stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=240,
                              env={**os.environ,'DOTNET_PROCESSOR_COUNT':'4','OMP_NUM_THREADS':'1'},creationflags=0x08000000)
        (work/(label+'.log')).write_bytes(result.stdout)
        records[label]={'exit':result.returncode}
        if result.returncode: raise RuntimeError(label+': '+result.stdout.decode('utf-8','replace')[-3500:])
        return result.stdout.decode('utf-8','replace')
    def application(label,script,model=None):
        command=[args.exe,'--script',repo/'tests/L1b'/script,'-g','No','-x','Yes','-w',work,'--report',work/(label+'.json'),'--timeout','180']
        if model:command+=['-f',model]
        output=run(label,command)
        assert 'TEST PASSED' in output
    def linux(path):
        value=str(Path(path).resolve()).replace('\\','/')
        return '/mnt/'+value[0].lower()+value[2:]
    host=linux(args.headless_linux)
    prefix=['wsl','-d','Ubuntu-24.04','--','env','TZ=Asia/Taipei','DOTNET_PROCESSOR_COUNT=4','nice','-n','19',host]
    application('windows_fixture','WindowsFixture.py')
    data=(work/'full.pmx').read_bytes()
    length=struct.unpack_from('<i',data,32)[0]
    payload=zlib.decompress(data[36:36+length],-15)
    object_length=struct.unpack_from('<q',payload)[0]
    record=payload[8:8+object_length]
    assert b'PrePoMax.Commands.Command' in record
    records['full_command_records']=True
    output=run('contracts',prefix+['contracts']);assert 'stable command identities=' in output
    output=run('linux_export',prefix+['export',linux(work/'full.pmx'),linux(work/'linux_export.inp')]);assert 'commands=6' in output
    output=run('linux_replay',prefix+['replay',linux(work/'full.pmx'),linux(work/'linux_replay.inp'),linux(work)]);assert 'replayed=6' in output
    run('linux_roundtrip',prefix+['roundtrip',linux(work/'full.pmx'),linux(work/'linux.pmx')])
    application('windows_reload','VerifyReload.py',work/'linux.pmx')
    names=['windows.inp','linux_export.inp','linux_replay.inp','windows_reload.inp']
    hashes={name:hashlib.sha256((work/name).read_bytes()).hexdigest() for name in names}
    assert len(set(hashes.values()))==1,hashes
    records['exports']=hashes
    records['fixture']=json.loads((work/'fixture.json').read_text())
    (work/'acceptance.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
    print('PASS: full Windows PMX, Linux replay/roundtrip, Windows reload; byte-identical INP '+next(iter(hashes.values())),flush=True)
if __name__=='__main__':
    sys.stdout.reconfigure(encoding='utf-8',errors='replace')
    main()
