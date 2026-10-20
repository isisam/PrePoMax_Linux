"""Byte and UTF-16 scan, including decompressed PMX content; no secret files read."""
import hashlib,json,re,sys,zlib,zipfile,io
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RULES={
 'private-extension':rb'(?i)ccx_fat|fatczm',
 'windows-profile-path':rb'(?i)[a-z]:[\\/]users[\\/][^\x00\r\n /\\]+',
 'build-home-path':rb'(?i)/home/(?:xeon|alpha|lota|kappa|isisam)[/\\]',
 'private-drive-path':rb'(?i)(?:z:[\\/][a-z_][a-z0-9_. -]{2,}[\\/]|[a-z]:[\\/]claudecalculix|\\\\192\.168\.)',
 'host-username':rb'(?i)\bxeon\b|XEONMICROPC',
 'secret-reference':rb'(?i)(?:[\\/]_secrets(?:[\\/]|\b)|\bauth\.json\b)',
 'github-token':rb'(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,})',
 'other-credential':rb'-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----|(?:sk-proj-|sk-ant-)[A-Za-z0-9_-]{20,}'
}

def inspect(data,rel):
    blobs=[data,rel.encode('utf-8')]
    if rel.lower().endswith('.pmx'):blobs.append(zlib.decompress(data[36:],-15))
    if rel.lower().endswith('.zip'):
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            for entry in archive.infolist():
                if not entry.is_dir():blobs.extend([entry.filename.encode('utf-8'),archive.read(entry)])
    found=[]
    for data in blobs:
        # Removing UTF-16 null bytes exposes ASCII strings in managed assemblies.
        for name,pattern in RULES.items():
            if re.search(pattern,data) or re.search(pattern,data.replace(b'\x00',b'')):
                found.append(name)
    return sorted(set(found))

def main():
    target=Path(sys.argv[1]) if len(sys.argv)>1 else ROOT/'payload'
    hits=[];manifest=[]
    for p in sorted(target.rglob('*')):
        if not p.is_file():continue
        rel=p.relative_to(target).as_posix();data=p.read_bytes()
        rules=inspect(data,rel)
        if rules:hits.append({'file':rel,'rules':rules})
        manifest.append({'file':rel,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
    report={'target':target.name,'files':len(manifest),'rules':list(RULES),'hits':hits,'passed':not hits}
    output=ROOT/'validation'/('scan-'+target.name+'.json')
    output.write_text(json.dumps(report,indent=2),encoding='utf-8')
    (ROOT/'validation'/('manifest-'+target.name+'.json')).write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2));return bool(hits)
if __name__=='__main__':sys.exit(main())
