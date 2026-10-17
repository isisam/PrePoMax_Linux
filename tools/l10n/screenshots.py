#PrePoMax Python Script
"""Explicit GUI evidence fixture. A command file selects the three review stages.
The driver only changes this disposable test model; all windows close on exit.
Use a native screenshot client to capture each ready stage.
"""
import clr, json, os, time
from System import Action
from System.Drawing import Point, Size
from System.Reflection import BindingFlags
from System.Windows.Forms import FormStartPosition
from PrePoMax import Controller
from PrePoMax.Forms import FrmMaterial, FrmCompositeLayup
from PrePoMax.L10n import Translator, UiTranslator
assembly=clr.GetClrType(Controller).Assembly
main=assembly.GetType('PrePoMax.Program').GetField('_mainForm',BindingFlags.Static | BindingFlags.NonPublic).GetValue(None)
controller=main.GetType().GetField('_controller',BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main)
pmx.model.materials.add('USERFATCZML10N',pmx.create.fatczm())
forms=[]
def stage(name):
    for form in forms:form.Hide()
    Translator.SetLanguage('zh-TW')
    if name=='main':
        main.Text='PrePoMax [L10N zh-TW]'
        main.StartPosition=FormStartPosition.Manual
        main.Location=Point(30,30);main.Size=Size(1250,850)
        main.TopMost=False; main.Show()
        UiTranslator.Translate(main)
    elif name=='material':
        form=FrmMaterial(controller)
        form.PrepareForm(None,'USERFATCZML10N')
        form.StartPosition=FormStartPosition.Manual;form.Location=Point(60,40)
        UiTranslator.Translate(form);form.Show(main);UiTranslator.Translate(form)
        forms.append(form)
    elif name=='layup':
        form=FrmCompositeLayup(controller)
        model=controller.Model
        form.LoadFromModel(model,model.CompositeLayups['Persist'],'Persist')
        form.StartPosition=FormStartPosition.Manual;form.Location=Point(60,40)
        UiTranslator.Translate(form);form.Show(main);UiTranslator.Translate(form)
        forms.append(form)
    else:raise Exception('Unknown screenshot stage')
last=None
try:
    for tick in range(2400):
        command=os.path.join(pmx.work_directory,'gui_command.txt')
        name=open(command,encoding='utf8').read().strip() if os.path.isfile(command) else 'main'
        if name=='close':break
        if name!=last:
            main.Invoke(Action(lambda:stage(name)))
            last=name
            with open(os.path.join(pmx.work_directory,'gui_ready.json'),'w',encoding='utf8') as stream:json.dump(dict(stage=name,pid=os.getpid()),stream)
            if forms:
                samples=UiTranslator.Inspect(forms[-1])
                with open(os.path.join(pmx.work_directory,'gui_'+name+'_samples.json'),'w',encoding='utf8') as stream:
                    json.dump([dict(context=s.Context,source=s.Source,text=s.Text,width=s.Width,required=s.RequiredWidth) for s in samples],stream,ensure_ascii=False,indent=2)
            print('READY screenshot stage '+name,flush=True)
        time.sleep(.2)
finally:
    def cleanup():
        for form in forms:form.Dispose()
        main.Hide();controller.ModelChanged=False
        Translator.SetLanguage('en')
    main.Invoke(Action(cleanup))
print('TEST PASSED')
