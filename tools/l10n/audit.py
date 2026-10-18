"""Offline catalogue completeness, placeholder preservation and evidence summary."""
from pathlib import Path
from collections import Counter
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[2]
LOGS=ROOT.parent/'logs/L10N'

def main():
    catalogue=json.loads((ROOT/'lang/zh-TW.json').read_text(encoding='utf8'))
    inventory=json.loads((ROOT/'lang/strings.inventory.json').read_text(encoding='utf8'))
    excluded=json.loads((ROOT/'lang/excluded.json').read_text(encoding='utf8'))
    missing=[s for s in inventory if s not in catalogue and s not in excluded]
    invalid=[]
    for source,translated in catalogue.items():
        if Counter(re.findall(r'\{[0-9]+\}',source))!=Counter(re.findall(r'\{[0-9]+\}',translated)):
            invalid.append(dict(source=source,translation=translated))
    leaked=[s for s,t in catalogue.items() if re.search(r'ZXQ|QXZ|XZXZ|ZQZ|QZ[0-9]',t,re.I)]
    forms=json.loads((LOGS/'forms.json').read_text(encoding='utf-8-sig'))
    summary=dict(source_strings=len(inventory),source_locations=sum(map(len,inventory.values())),
        catalogue_entries=len(catalogue),protected_sources=len(excluded),missing=missing,
        placeholder_mismatches=invalid,unrestored_draft_tokens=leaked,
        forms=len(forms),construction_failures=json.loads((LOGS/'construction_failures.json').read_text(encoding='utf-8-sig')),
        translated=sum(f['translated'] for f in forms),total=sum(f['total'] for f in forms),
        overflow=sum(f['overflow'] for f in forms),all_english_snapshots_restored=all(f['restoredExactly'] for f in forms),
        glossary_terms=sum(1 for line in (ROOT/'lang/GLOSSARY_zh-TW.md').read_text(encoding='utf8').splitlines() if line.startswith('| '))-2,
        catalogue_sha256=hashlib.sha256((ROOT/'lang/zh-TW.json').read_bytes()).hexdigest())
    (LOGS/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print(json.dumps(summary,ensure_ascii=False,indent=2))
    return bool(missing or invalid or leaked or summary['construction_failures'] or not summary['all_english_snapshots_restored'])
if __name__=='__main__':raise SystemExit(main())
