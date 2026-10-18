#PrePoMax Python Script
"""Build a portable teaching snapshot from the mesh and meta.json in -w."""
import os,json,re,clr,sys
sys.path.insert(0,os.path.abspath(os.path.join(os.path.dirname(__file__),'../../APIs/Python/Test_Scripts')))
from System.Reflection import BindingFlags
from System.Collections.Generic import List
from PrePoMax.Commands import Command
from CaeModel import SolverTypeEnum
from Composite_Layup_Utils import mesh,plies,steps
meta=json.load(open(os.path.join(pmx.work_directory,'meta.json')))
name=meta['name']
if name=='Layup':
    region=mesh(pmx,name,[i*.125 for i in range(9)],nx=4,ny=2)
    pmx.model.composite_layups.add('Layup',region,plies(pmx,[45,90,-45,0],.125),symmetric=True)
    steps(pmx)
else:
    original=open(os.path.join(pmx.work_directory,'reference.inp')).read()
    types={}
    from Composite_Layup_Utils import blocks
    for key,options,rows in blocks(original):
        if key=='*ELEMENT':
            for row in rows:types[int(row.split(',')[0])]=options['TYPE']
    for part in pmx.model.parts.get_names():
        values={types[e] for e in pmx.model.parts[part].get_element_ids()}
        assert len(values)==1,(part,values)
        pmx.model.parts.set_element_type(part,'linear_hexa',next(iter(values)))
    text=original[original.index('*STEP'):]
    text=text.replace('*STATIC\n','*STATIC,SOLVER=SPOOLES\n').replace('*STATIC, DIRECT\n','*STATIC,DIRECT,SOLVER=SPOOLES\n')
    if '*NODE FILE' not in text:
        text=text.replace('*END STEP','*NODE FILE\nU\n*EL FILE\nSDV\n*END STEP')
    roots=[k for k in pmx.model.keywords.get_model_keywords() if len(k.indices)==1]
    pmx.model.keywords.set_user_keyword([max(list(k.indices)[0] for k in roots)+1],text)
pmx.jobs.add(name,number_of_cpus=1)
# Publish a model snapshot, with no workstation-dependent command/import history.
resolver=pmx.model.GetType().GetField('_modelResolver',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pmx.model)
gateway=resolver.ModelGateway
c=gateway.GetType().GetField('_controller',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(gateway)
c.SetCommands(List[Command]())
for job in c.Jobs.Values:
    job.Executable='';job.WorkDirectory=''
commands=c.GetType().GetField('_commands',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c)
commands.GetType().GetField('_historyFileNameBin',BindingFlags.Instance|BindingFlags.NonPublic).SetValue(commands,None)
c.SaveToPmx(os.path.join(pmx.work_directory,name+'.pmx'))
pmx.export_to_calculix(os.path.join(pmx.work_directory,name+'.inp'))
print('TEST PASSED')
