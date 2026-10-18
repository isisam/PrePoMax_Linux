"""Compare unnormalized Windows property/field snapshots against the L2c baseline."""
import json,sys
from pathlib import Path
a,b=map(lambda p:json.loads(Path(p).read_text(encoding='utf-8-sig')),sys.argv[1:3])
# Raw snapshots retain CLR assembly identities. Only the explicitly moved assembly
# name in generic signatures is mapped back for this comparison.
b=json.loads(json.dumps(b).replace(', PrePoMax.ViewModels, Version=3.0.0.0', ', PrePoMax, Version=3.0.0.0'))
differences=[]
for name in sorted(a.keys()|b.keys()):
    if a.get(name)!=b.get(name):
        # The only intentionally neutralized public wrapper signature: this unused parent
        # argument was previously Form; all source callers still bind to object.
        left,right=a.get(name),b.get(name)
        if name=='PrePoMax.Forms.ViewNodeSet':
            left={**left,'Constructors':[c.replace('System.Windows.Forms.Form','System.Object') for c in left['Constructors']]}
        if left!=right: differences.append(name)
out={'compared':len(a),'unexplained_differences':differences,'documented_difference':'ViewNodeSet unused parent argument Form -> object'}
print(json.dumps(out,indent=2));Path(sys.argv[3]).write_text(json.dumps(out,indent=2),encoding='utf-8')
raise SystemExit(bool(differences))
