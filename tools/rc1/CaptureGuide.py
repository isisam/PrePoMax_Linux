#PrePoMax Python Script
"""Native controls captured only inside the runner's never-visible desktop."""
import clr,os,json,traceback
from System import Action
from System.Drawing import Bitmap,Point,Size,Rectangle
from System.Reflection import BindingFlags
from System.Windows.Forms import FormStartPosition,Application,FormWindowState
from PrePoMax import FrmMain
from PrePoMax.Forms import FrmCompositeLayup,FrmMaterial,FrmSettings
from PrePoMax.L10n import Translator,UiTranslator
assembly=clr.GetClrType(FrmMain).Assembly
main=assembly.GetType('PrePoMax.Program').GetField('_mainForm',BindingFlags.Static|BindingFlags.NonPublic).GetValue(None)
c=main.GetType().GetField('_controller',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(main)
if not pmx.model.materials.contains('USERFATCZM'):pmx.model.materials.add('USERFATCZM',pmx.create.fatczm())
results=[]
def capture(form,name):
    form.StartPosition=FormStartPosition.Manual;form.Location=Point(-32000,-32000);form.ShowInTaskbar=False
    form.Show();UiTranslator.Translate(form);Application.DoEvents()
    b=Bitmap(form.Width,form.Height);form.DrawToBitmap(b,Rectangle(0,0,form.Width,form.Height))
    b.Save(os.path.join(pmx.work_directory,name+'.png'));b.Dispose()
def work():
    forms=[]
    try:
        main.WindowState=FormWindowState.Normal;main.Size=Size(1180,780)
        # No personal paths in the captured title.
        main.Text='PrePoMax-RCM 0.1.0-rc1 | Composite layup coupon'
        for lang in ['en','zh-TW']:
            settings=c.GetType().GetField('_settings',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c)
            settings.General.LastFileName='Examples/Layup/Layup.pmx'
            settings.General.Language=lang
            c.ApplySettings()
            Translator.SetLanguage(lang)
            main.GetType().GetField('tsmiClearOutputTextbox',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(main).PerformClick()
            capture(main,'main_'+lang)
            form=FrmSettings();forms.append(form);form.PrepareForm(c,None)
            capture(form,'settings_'+lang);form.Dispose()
            form=FrmCompositeLayup(c);forms.append(form);form.PrepareForm(None,'Layup')
            form.Size=Size(1180,710);capture(form,'layup_'+lang);form.Dispose()
            form=FrmMaterial(c);forms.append(form);form.PrepareForm(None,'USERFATCZM')
            capture(form,'material_'+lang);form.Dispose()
        # Capture selected native libraries from this actual build for runtime audit.
        from System.Diagnostics import Process
        modules=[str(m.FileName) for m in Process.GetCurrentProcess().Modules if any(x in m.ModuleName.lower() for x in ['msvc','vcruntime','vtk'])]
        json.dump(modules,open(os.path.join(pmx.work_directory,'loaded_modules.json'),'w'),indent=2)
        results.append('PASS')
    except Exception:results.append(traceback.format_exc())
    finally:
        for f in forms:f.Dispose()
        main.Hide();c.ModelChanged=False;Translator.SetLanguage('en')
main.Invoke(Action(work))
assert results==['PASS'],str(results)
print('TEST PASSED')
