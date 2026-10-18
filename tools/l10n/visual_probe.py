#PrePoMax Python Script
"""Record tree identities and displayed category text without showing a window."""
import clr,json,os
from System import Action
from System.Reflection import BindingFlags
from System.Windows.Forms import TreeView
from PrePoMax import FrmMain, Controller
from PrePoMax.L10n import Translator,UiTranslator
assembly=clr.GetClrType(FrmMain).Assembly
main=assembly.GetType('PrePoMax.Program').GetField('_mainForm',BindingFlags.Static | BindingFlags.NonPublic).GetValue(None)
controller=main.GetType().GetField('_controller',BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main)
pmx.model.materials.add('USERFATCZML10N',pmx.create.fatczm())
records=[]
def inspect():
    Translator.SetLanguage('zh-TW');UiTranslator.Translate(main)
    def nodes(items):
        for node in items:
            records.append(dict(name=node.Name,text=node.Text,display=UiTranslator.TreeCaption(node),
                tag=None if node.Tag is None else node.Tag.GetType().FullName,
                forecolor=node.ForeColor.Name,bounds=str(node.Bounds)))
            nodes(node.Nodes)
    def walk(root):
        if isinstance(root,TreeView):nodes(root.Nodes)
        for child in root.Controls:walk(child)
    walk(main)
    Translator.SetLanguage('en')
main.Invoke(Action(inspect))
with open(os.path.join(pmx.work_directory,'tree_probe.json'),'w',encoding='utf8') as stream:json.dump(records,stream,ensure_ascii=False,indent=2)
matches=[item for item in records if item['name']=='USERFATCZML10N']
assert len(matches)==1 and matches[0]['text']=='USERFATCZML10N' and matches[0]['display']=='USERFATCZML10N',matches
print('PASS model-tree identity and displayed name are preserved for the FATCZM demo material')
print('TEST PASSED')
