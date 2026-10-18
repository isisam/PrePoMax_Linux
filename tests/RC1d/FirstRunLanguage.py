#PrePoMax Python Script
"""Read actual startup form texts before changing language; never show dialogs."""
import clr, json, os
from System import Action
from System.Globalization import CultureInfo
from System.Reflection import BindingFlags
from System.Windows.Forms import Application
from PrePoMax import FrmMain
from PrePoMax.Settings import ViewGeneralSettings
from PrePoMax.L10n import Translator, UiTranslator

assert not os.path.exists(os.path.join(Application.StartupPath, 'settings.bin'))
program=clr.GetClrType(FrmMain).Assembly.GetType('PrePoMax.Program')
main=program.GetField('_mainForm', BindingFlags.Static | BindingFlags.NonPublic).GetValue(None)
flags=BindingFlags.Instance | BindingFlags.NonPublic
controller=main.GetType().GetField('_controller', flags).GetValue(main)
settings=controller.GetType().GetField('_settings', flags).GetValue(controller)
records=[]
errors=[]

def check():
    try:
        culture=CultureInfo.CurrentUICulture.Name
        assert CultureInfo.CurrentCulture.Name==''
        assert CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator==''
        expected='zh-TW' if culture.lower() in ['zh-tw','zh-hk','zh-hant'] or culture.lower().startswith('zh-hant-') else 'en'
        def texts():
            return {name:str(main.GetType().GetField(name, flags).GetValue(main).Text)
                    for name in ['tsmiFile','tsmiTools','tsmiSettings']}
        def verify(language):
            actual=texts()
            assert settings.General.Language==language
            assert Translator.Language==language
            assert actual['tsmiFile']==('File' if language=='en' else Translator.Translate('File','FrmMain.tsmiFile')),actual
            assert ('檔案' in actual['tsmiFile']) if language=='zh-TW' else actual['tsmiFile']=='File'
            records.append(dict(language=language,form_texts=actual))
        # This read-back precedes any explicit translation call in the probe.
        verify(expected)
        view=ViewGeneralSettings(settings.General)
        for choice,language in [('English','en'),('繁體中文','zh-TW'),('English','en')]:
            view.Language=choice
            controller.ApplySettings()
            UiTranslator.Translate(main)
            verify(language)
        assert CultureInfo.CurrentUICulture.Name==culture
        settings.General.Language=expected
        controller.ApplySettings()
        result=dict(status='PASS',ui_culture=culture,numeric_culture=CultureInfo.CurrentCulture.Name,initial_language=expected,
                    settings_absent_before_start=True,checks=records)
        json.dump(result,open(os.environ['RC1D_LANGUAGE_OUT'],'w',encoding='utf-8'),ensure_ascii=False,indent=2)
    except Exception as ex:
        errors.append(str(ex))
main.Invoke(Action(check))
assert not errors, errors
# Retain all RC1c working-directory/serialization/override assertions.
path=os.path.join(os.path.dirname(__file__),'..','RC1c','FirstRunProbe.py')
exec(compile(open(path,encoding='utf-8').read(),path,'exec'),globals())
print('FIRST RUN FORM LANGUAGE AND BIDIRECTIONAL SETTINGS PASS')
