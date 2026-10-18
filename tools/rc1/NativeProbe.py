#PrePoMax Python Script
import clr,json,os
from System import Array,Object,Int32,IntPtr
from System.Diagnostics import Process
from CaeMesh import FeMesh
from System.Windows.Forms import Application
clr.AddReference(os.path.join(Application.StartupPath,'lib','ThreeMF.dll'))
from Lib3MF import Lib3MFWrapper
version=Lib3MFWrapper.GetLibraryVersion()
assert version[0]>=2,version
lp=clr.GetClrType(FeMesh).Assembly.GetType('CaeMesh.LpSolve',True)
h=lp.GetMethod('make_lp').Invoke(None,Array[Object]([Int32(0),Int32(1)]))
assert h!=IntPtr.Zero
lp.GetMethod('delete_lp').Invoke(None,Array[Object]([h]))
modules=[str(m.FileName) for m in Process.GetCurrentProcess().Modules if any(s in m.ModuleName.lower() for s in ['msvc','vcruntime','lib3mf','lpsolve'])]
json.dump(dict(lib3mf=list(version),lpsolve=True,modules=modules),open(os.path.join(pmx.work_directory,'native.json'),'w'),indent=2)
print('TEST PASSED')
