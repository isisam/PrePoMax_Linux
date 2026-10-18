#PrePoMax Python Script
"""Run the analysis from an opened teaching PMX; validate expected.json beside this script."""
import os,json,re,math
name=list(pmx.jobs.get_names())[0]
job=pmx.jobs[name]
assert os.path.basename(job.executable).lower()=='ccx_fat.exe',job.executable
assert not job.inherit_executable is False
status=job.run(timeout=900)
assert status=='OK',status+'\n'+job.get_output()
assert '*ERROR' not in job.get_output()
dat=open(os.path.join(pmx.work_directory,name+'.dat')).read()
values={}
if name=='Layup':
    totals=re.findall(r'total force.*?\n\s*\n\s*([\d.E+\-]+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)',dat,re.I|re.S)
    assert totals
    values['Ex_MPa']=float(totals[-1][0])/2.0/0.0001
else:
    rows=[]
    for line in open(os.path.join(pmx.work_directory,'fatczm_jumps.log')):
        f=line.split()
        if f and f[0]=='2':rows.append([float(x) for x in f])
    values['jumps']=len(rows)
    values['final_N']=rows[-1][2]+rows[-1][5]
    if name=='ENF':
        values['onset_N']=rows[100][2]
        failed=[]
        for m in re.finditer(r'internal state variables.*?for set TIPCOH.*?time\s+([\d.E+-]+)(.*?)(?=\n\s*internal state variables|\Z)',dat,re.I|re.S):
            d=[float(v) for v in re.findall(r'^\s*2368\s+\d+\s+([\d.E+-]+)',m[2],re.M)]
            if len(d)==8 and min(d)>=1:failed.append(float(m[1]))
        assert failed and min(failed)==101
        values['first_full_failure_time']=min(failed)
    else:
        u=re.findall(r'displacements.*?for set NTOP.*?\n\s*\n\s*\d+\s+([\d.E+-]+)\s+([\d.E+-]+)',dat,re.I|re.S)
        assert u
        values['top_U2_mm']=float(u[-1][1])
assert os.path.getsize(os.path.join(pmx.work_directory,name+'.frd'))>1000
with open(os.path.join(pmx.work_directory,'actual.json'),'w') as f:json.dump(values,f,indent=2)
expected_file=os.path.join(os.path.dirname(__file__),'expected.json')
if os.path.isfile(expected_file):
    expected=json.load(open(expected_file))[name]
    for k,v in expected.items():assert abs(values[k]-v)<=max(abs(v)*1e-5,1e-8),(k,values[k],v)
print(json.dumps(values))
print('TEST PASSED')
