#PrePoMax Python Script
"""Run the real-form inventory on the host UI thread, without showing dialogs."""
import clr, os, traceback
from System import Action
from System.Reflection import BindingFlags
from PrePoMax import Controller
from PrePoMax.L10n import Translator
flags=BindingFlags.Static | BindingFlags.NonPublic
program=clr.GetClrType(Controller).Assembly.GetType('PrePoMax.Program')
main=program.GetField('_mainForm',flags).GetValue(None)
controller=main.GetType().GetField('_controller',BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main)
clr.AddReference(os.path.join(pmx.work_directory,'L10nChecks.dll'))
from PrePoMax.L10n.Tests import L10nChecks
result=[]
def check():
    try:result.append(L10nChecks.Run(main,controller,pmx.work_directory))
    except Exception:result.append(traceback.format_exc())
main.Invoke(Action(check))
print(result[0])
if len(result)!=1 or 'Traceback' in result[0] or 'CHECK FAILED' in result[0]:raise Exception('L10N headless failure')
print('TEST PASSED')
