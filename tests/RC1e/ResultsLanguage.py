#PrePoMax Python Script
"""Native form/controller/VTK acceptance; no initializer or test-only repair on language switch."""
import os, json, clr, traceback
from System import Action, Array, String, Enum
from System.Reflection import BindingFlags
from System.Drawing import Point, Size
from System.Windows.Forms import Application, FormStartPosition
from PrePoMax import FrmMain, SettingsContainer, ViewResultsTypeEnum
from PrePoMax.Settings import ViewGeneralSettings
from PrePoMax.L10n import Translator, UiTranslator
from CaeResults import ComplexResultTypeEnum, DeformationScaleFactorTypeEnum

F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public
program=clr.GetClrType(FrmMain).Assembly.GetType('PrePoMax.Program')
main=program.GetField('_mainForm',BindingFlags.Static|BindingFlags.NonPublic).GetValue(None)
def field(obj,name):
    t=obj.GetType()
    while t:
        f=t.GetField(name,F)
        if f:return f.GetValue(obj)
        t=t.BaseType
    raise AssertionError(name)
c=field(main,'_controller');vtk=field(main,'_vtk')
SettingsContainer.IsSaveDisabled=True
out=os.environ['RC1E_OUTPUT'];os.makedirs(out,exist_ok=True)
clr.AddReference(os.path.join(out,'SettingsChoices.dll'))
from RC1e.Tests import SettingsChoices
records=[];panels=[];errors=[]
def save():
    json.dump(dict(status='FAIL' if errors else 'PASS',records=records,settings=panels,errors=errors),
              open(os.path.join(out,'results.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=2)

def run():
    try:
        main.StartPosition=FormStartPosition.Manual;main.Location=Point(-32000,-32000)
        main.ShowInTaskbar=False;main.Size=Size(1280,900);main.Show();Application.DoEvents()
        vtk.Visible=True;main.RenderingOn=True
        settings=field(c,'_settings')
        general=ViewGeneralSettings(settings.General)
        def language(lang):
            general.Language='繁體中文' if lang=='zh-TW' else 'English'
            c.ApplySettings()  # same product path as Settings Apply
            Application.DoEvents()
            assert Translator.Language==lang
        language('en')
        dialog=field(main,'_frmSettings');dialog.PrepareForm(c,None)
        views=field(dialog,'_viewSettings')
        # Reuse the same view objects and converters across both switches, as an open Settings dialog does.
        for lang in ['en','zh-TW','en']:
            language(lang);UiTranslator.Translate(dialog)
            pages={str(entry.Key):json.loads(SettingsChoices.Check(entry.Value)) for entry in views}
            assert len(pages)==13,len(pages)
            panels.append(dict(language=lang,pages=pages))
        assert panels[0]['pages']==panels[2]['pages']
        color=field(main,'tscbColorBy')
        language('zh-TW')
        for index in range(color.Items.Count):
            color.SelectedIndex=index
            choice=color.SelectedItem
            menu=choice.GetType().GetField('Value').GetValue(choice)
            if menu is not None:assert menu.Checked,str(menu.Name)
        color.SelectedIndex=3 # Parts: retain a non-default menu identity in both directions.
        for lang in ['en','zh-TW','en']:
            language(lang)
            assert color.SelectedIndex==3
            assert field(main,'tsmiAnnotateParts').Checked
            assert color.Text==field(main,'tsmiAnnotateParts').Text
        color.SelectedIndex=0
        assert str(c.AnnotateWithColor)=='None'
        language('en')
        sources=json.load(open(os.environ['RC1E_SOURCES'],encoding='utf-8'))
        for example,path in sources.items():
            main.Open(path,Action[String,String](lambda p,args:c.Open(p,args)),False,None)
            c.ViewResultsType=ViewResultsTypeEnum.ColorContours
            # Exercise every enum identity, including the non-default complex and user-defined selections.
            complex_box=field(main,'tscbComplex');deformation=field(main,'tscbDeformationType')
            for index,value in enumerate(Enum.GetValues(clr.GetClrType(ComplexResultTypeEnum))):
                complex_box.SelectedIndex=index;assert main.GetComplexResultType()==value
            complex_box.SelectedIndex=0
            for value in Enum.GetValues(clr.GetClrType(DeformationScaleFactorTypeEnum)):
                main.SetDeformationType(value);assert main.GetDeformationType()==value
            main.SetDeformationFactor(1.75)
            field(main,'tstbAngle').Text='37'
            # Non-default selections must survive both language changes.
            complex_box.SelectedIndex=2 # Magnitude (these three fixtures are real-valued)
            main.SetDeformationType(DeformationScaleFactorTypeEnum.UserDefined)
            baseline=None
            data_names=['tscbResultNames','tscbDeformationVariable','tscbStepAndIncrement','tscbSymbols']
            data_lists={name:([str(item) for item in field(main,name).Items],field(main,name).SelectedIndex)
                        for name in data_names}
            for index,lang in enumerate(['en','zh-TW','en']):
                before=(complex_box.SelectedIndex,deformation.SelectedIndex,main.GetDeformationVariable(),
                        main.GetDeformationFactor(),main.GetComplexAngleDeg())
                language(lang)
                after=(complex_box.SelectedIndex,deformation.SelectedIndex,main.GetDeformationVariable(),
                       main.GetDeformationFactor(),main.GetComplexAngleDeg())
                assert before==after,(before,after)
                assert data_lists=={name:([str(item) for item in field(main,name).Items],field(main,name).SelectedIndex)
                                    for name in data_names}
                assert main.GetComplexResultType()==ComplexResultTypeEnum.Magnitude
                assert main.GetDeformationType()==DeformationScaleFactorTypeEnum.UserDefined
                # Check stored display strings, including ComboBox.Text (not just dynamic ToString()).
                assert complex_box.Text==str(complex_box.SelectedItem)
                assert complex_box.Text==('大小' if lang=='zh-TW' else 'Magnitude'),complex_box.Text
                assert deformation.Text==str(deformation.SelectedItem)
                assert deformation.Text==('使用者定義' if lang=='zh-TW' else 'User defined'),deformation.Text
                # The public setter and the actual SelectedIndexChanged handlers also work after switching.
                main.SetDeformationType(DeformationScaleFactorTypeEnum.Automatic025)
                assert main.GetDeformationType()==DeformationScaleFactorTypeEnum.Automatic025
                main.SetDeformationType(DeformationScaleFactorTypeEnum.UserDefined)
                complex_box.SelectedIndex=0;assert main.GetComplexResultType()==ComplexResultTypeEnum.Real
                complex_box.SelectedIndex=2
                c.DrawResults(False);main.SetFrontBackView(False,True);main.SetZoomToFit(False)
                main.UpdateScalarsAndCameraAndRedraw()
                Application.DoEvents()
                fd=c.CurrentFieldData
                values=list(c.CurrentResult.GetField(fd,True).GetComponentValues(fd.Component))
                expected=[float(min(values)),float(max(values))]
                legend=field(vtk,'_scalarBarWidget')
                # The widget normalizes its color LUT for drawing. Its edit endpoints and labels
                # retain the physical values actually displayed to the user.
                actual=[float(field(legend,'_editMinValue')),float(field(legend,'_editMaxValue'))]
                labels=[str(x) for x in field(legend,'_labels')]
                for a,b in zip(actual,expected):assert abs(a-b)<=max(1e-8,abs(b)*1e-5),(actual,expected)
                for a,b in zip([float(labels[-1]),float(labels[0])],expected):
                    assert abs(a-b)<=max(1e-8,abs(b)*5e-4),(labels,expected)
                if baseline is None:baseline=actual
                else:assert actual==baseline,(example,actual,baseline)
                actors=[entry.Value for entry in field(vtk,'_actors') if entry.Value.ColorContours and entry.Value.VtkMaxActorVisible]
                assert actors,example+' has no visible contour actors'
                cells=sum(int(a.GeometryMapper.GetInput().GetNumberOfCells()) for a in actors)
                assert cells>0
                image=example+'_'+str(index)+'_'+lang+'.png'
                vtk.RenderToPNG(os.path.join(out,image),False,1)
                # Independent empty-scene control: axes/legend remain; contour geometry is hidden.
                for a in actors:a.Geometry.VisibilityOff()
                empty=example+'_'+str(index)+'_without_contours.png'
                vtk.RenderToPNG(os.path.join(out,empty),False,1)
                for a in actors:a.Geometry.VisibilityOn()
                records.append(dict(example=example,language=lang,result_type=str(main.GetComplexResultType()),
                    deformation=str(main.GetDeformationType()),factor=main.GetDeformationFactor(),angle=main.GetComplexAngleDeg(),
                    complex_text=complex_box.Text,deformation_text=deformation.Text,legend=actual,legend_labels=labels,field_extrema=expected,
                    field=str(fd.Name),component=str(fd.Component),step=int(fd.StepId),increment=int(fd.StepIncrementId),
                    contour_actors=len(actors),contour_cells=cells,image=image,empty_control=empty))
                save()
            complex_box.SelectedIndex=0;main.SetDeformationType(DeformationScaleFactorTypeEnum.Automatic)
        c.ModelChanged=False;main.Hide()
    except Exception:
        errors.append(traceback.format_exc());save()
main.Invoke(Action(run))
save();assert not errors,errors
print('TEST PASSED: live en -> zh-TW -> en, three FRDs, enum identity, Settings choices, VTK contours and legend')
