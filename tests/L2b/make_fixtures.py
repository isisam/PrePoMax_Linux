"""Maintainer tool: static CalculiX decks, not called by the acceptance tests."""
from pathlib import Path
import json
out=Path(__file__).parent/'fixtures';out.mkdir(exist_ok=True)
tet=[(0,0,0),(1,0,0),(0,1,0),(0,0,1),(.5,0,0),(.5,.5,0),(0,.5,0),(0,0,.5),(.5,0,.5),(0,.5,.5)]
hexa=[(0,0,0),(1,0,0),(1,1,0),(0,1,0),(0,0,1),(1,0,1),(1,1,1),(0,1,1),(.5,0,0),(1,.5,0),(.5,1,0),(0,.5,0),(.5,0,1),(1,.5,1),(.5,1,1),(0,.5,1),(0,0,.5),(1,0,.5),(1,1,.5),(0,1,.5)]
wedge=[(0,0,0),(0,1,0),(1,0,0),(0,0,1),(0,1,1),(1,0,1),(0,.5,0),(.5,.5,0),(.5,0,0),(0,.5,1),(.5,.5,1),(.5,0,1),(0,0,.5),(0,1,.5),(1,0,.5)]
quad=[(0,0,0),(1,0,0),(1,1,0),(0,1,0),(.5,0,0),(1,.5,0),(.5,1,0),(0,.5,0)]
expected=[]
for name,coords,typ,order,point,face in [
 ('C3D10',tet,24,list(range(10)),[.2,.2,.6],1),
 ('C3D15',wedge,26,[0,2,1,3,5,4,8,7,6,11,10,9,12,14,13],[.2,.2,1],1),
 ('C3D20',hexa,25,list(range(20)),[.4,.4,1],5),
 ('S8R',quad,23,list(range(8)),[.4,.4,0],0),
 ('C3D6',wedge[:6],13,[0,2,1,3,5,4],[.2,.2,1],1)]:
    ids=[1001+i*7 for i in range(len(coords))]
    text='*HEADING\nL2b real FeModel adapter fixture '+name+'\n*NODE\n'
    text+=''.join(str(id)+','+','.join(map(str,p))+'\n' for id,p in zip(ids,coords))
    text+='*ELEMENT, TYPE='+name+', ELSET=PART_A\n9001,'+','.join(map(str,ids))+'\n'
    (out/(name+'.inp')).write_text(text)
    expected.append(dict(name=name,type=typ,order=[ids[i] for i in order],point=point,face=face))
(out/'expected.json').write_text(json.dumps(expected,indent=2))
