#PrePoMax Python Script
"""Language settings, dynamic property metadata, state routing, and export invariance."""
import clr, json, os
from System import Action
from System.ComponentModel import TypeDescriptor
from System.Globalization import CultureInfo
from System.Reflection import BindingFlags
from PrePoMax import FrmMain, Controller, ViewUserMaterial, ViewCompositeLayup, GeneralSettings, SettingsContainer, Globals
from PrePoMax.Settings import ViewGeneralSettings
from CaeModel import UserMaterial
from PrePoMax.L10n import Translator, UiTranslator
assembly=clr.GetClrType(FrmMain).Assembly
main=assembly.GetType('PrePoMax.Program').GetField('_mainForm',BindingFlags.Static | BindingFlags.NonPublic).GetValue(None)
controller=main.GetType().GetField('_controller',BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main)
before_culture=(CultureInfo.CurrentCulture.Name,CultureInfo.CurrentUICulture.Name)
settings=controller.Settings
settings.SetRegenerationWorkDirectory(None) # detached clone; test-only settings may be saved
settings.General.Language='en'
from CaeModel import SolverTypeEnum
settings.Solver.CalculixDefaultSolverType=SolverTypeEnum.Spooles
settings.SaveToFile(os.path.join(pmx.work_directory,'english_settings.bin'))
assert os.path.isfile(os.path.join(pmx.work_directory,'english_settings.bin'))

def ui_contracts():
    Translator.SetLanguage('zh-TW')
    main.SetState(Globals.OpeningText,True)
    assert main.IsStateOpening()
    main.SetStateReady(Globals.OpeningText)
    assert main.SetStateWorking(Globals.RegeneratingText)
    assert main.IsStateRegeneratingOrUndoing()
    main.SetStateReady(Globals.RegeneratingText)
    assert main.SetStateWorking(Globals.PythonScriptText)
    Translator.SetLanguage('en')
main.Invoke(Action(ui_contracts))

material=UserMaterial.CreateFatczm()
view=ViewUserMaterial(material)
properties=TypeDescriptor.GetProperties(view)
prop=properties['NumberOfStateVariables']
english=(prop.DisplayName,prop.Category,prop.Description)
value=prop.GetValue(view)
Translator.SetLanguage('zh-TW')
assert prop.DisplayName=='狀態變數 (DEPVAR)' and prop.Category.strip('\u200b')=='使用者材料'
assert prop.Description=='狀態變數數量。FATCZM 至少需要 6 個；啟裂模型至少需要 8 個。'
assert prop.GetValue(view)==value
clr.AddReference(os.path.join(pmx.work_directory,'L10nChecks.dll'))
from PrePoMax.L10n.Tests import L10nChecks
layup_view=ViewCompositeLayup(controller.Model.CompositeLayups['Persist'])
layup_view.Populate(controller.Model)
L10nChecks.CheckChoice(layup_view,'RegionType','元素集名稱')
L10nChecks.CheckChoice(layup_view,'Symmetric','否')
Translator.SetLanguage('en')
assert (prop.DisplayName,prop.Category,prop.Description)==english
general=GeneralSettings()
assert general.Language=='en' # RC1 follows the English-only upstream default.
general.Language='zh-TW'
language_view=ViewGeneralSettings(general)
assert language_view.Language=='繁體中文'
choices=language_view.GetType().GetField('_dctd',BindingFlags.Instance | BindingFlags.NonPublic).GetValue(language_view).GetProperty('Language').StatandardValues
assert [item.Value for item in choices]==['English','繁體中文']
language_view.Language='English'
assert general.Language=='en'
settings.General.Language='zh-TW'
settings.SaveToFile(os.path.join(pmx.work_directory,'chinese_settings.bin'))
previous_override=SettingsContainer.OverrideFileName
try:
    for language,filename in [('en','english_settings.bin'),('zh-TW','chinese_settings.bin')]:
        SettingsContainer.OverrideFileName=os.path.join(pmx.work_directory,filename)
        loaded=SettingsContainer();loaded.LoadFromFile()
        assert loaded.General.Language==language
finally:SettingsContainer.OverrideFileName=previous_override

for language in ['en','zh-TW']:
    Translator.SetLanguage(language)
    pmx.export_to_calculix(os.path.join(pmx.work_directory,'language_'+language+'.inp'))
assert open(os.path.join(pmx.work_directory,'language_en.inp'),'rb').read()==open(os.path.join(pmx.work_directory,'language_zh-TW.inp'),'rb').read()
assert before_culture==(CultureInfo.CurrentCulture.Name,CultureInfo.CurrentUICulture.Name)
Translator.SetLanguage('en')
with open(os.path.join(pmx.work_directory,'contracts.json'),'w',encoding='utf8') as stream:
    json.dump(dict(dynamic_metadata=True,metadata_restore=True,state_routing=True,language_choices=True,
        settings_files=True,cultures_unchanged=True,export_bytes_identical=True),stream,indent=2)
print('PASS dynamic FATCZM metadata, invariant state, language choices, settings and identical en/zh-TW INP bytes')
print('TEST PASSED')
