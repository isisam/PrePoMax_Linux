#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Solver_Ccx_Fat_Warning.py -g No -x Yes -w <dir>
#
#Expected process exit code 5.
#Expected report prompts[-1].refused = true
#Expected report prompts[-1].text contains "ccx_fat.exe does not include Pardiso or PaStiX"
#Expected report prompts[-1].text contains "Step 'Unsupported': PaStiX"
#Expected report jobs[-1].status = "None"
import os
from System.Windows.Forms import Application

pmx.model.steps.add_static("Unsupported", solver_type="PaStiX")
job = pmx.jobs.add("WarningCheck", executable=os.path.join(Application.StartupPath, "Solver", "ccx_fat.exe"),
                   number_of_cpus=1)
job.run(timeout=30)
raise Exception("TEST FAILED: the unsupported solver must warn before submission")
