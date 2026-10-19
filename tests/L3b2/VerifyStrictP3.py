#PrePoMax Python Script
"""Read-only verification and export of Linux form artifacts in hidden Windows."""
import os, json
from System.Reflection import BindingFlags
from CaeModel import ElasticEngineeringConstants, ElasticLamina, UserMaterial
resolver=pmx.model.GetType().GetField('_modelResolver',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pmx.model)
gateway=resolver.ModelGateway
controller=gateway.GetType().GetField('_controller',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(gateway)
model=controller.Model
kind=os.path.basename(os.path.dirname(pmx.work_directory))
if kind=='ui-old-DCB' and os.environ.get('PPM_FINAL_P3_OLD')=='1':
    step=controller.GetStep('Step-1')
    assert step.InitialTimeIncrement==0.1 and step.IncrementationType.ToString()=='Automatic'
    assert step.BoundaryConditions.ContainsKey('StrictFixed') and step.Loads['StrictForce'].F2.Value==0.001
    assert list(model.Mesh.NodeSets['StrictNodes'].Labels)==[1,2,3,4]
    assert not controller.GetCommandCollectionErrors()
if kind!='ui-old-DCB':
    assert len(model.Mesh.NodeSets['StrictNodes'].Labels)==4
    assert len(model.Mesh.ElementSets['StrictElements'].Labels)==1
    assert model.Mesh.Surfaces['StrictSurface'].Valid and model.Mesh.Surfaces['StrictSurface'].Area>0
    if kind=='ui-DCB':
        m=model.Materials['USERFATCZM'];um=next(p for p in m.Properties if isinstance(p,UserMaterial))
        assert um.Fatczm and um.Constants.Count==26 and um.Constants[12].Value.Value==0.02 and um.Constants[13].Value.Value==2
        assert next(p for p in model.Materials['IM7'].Properties if isinstance(p,ElasticEngineeringConstants)).EngineeringConstantsTemp[0][0].Value==161001
        arm=next(s for s in model.Sections.Values if s.RegionName=='ARMS')
        assert arm.MaterialName!='IM7'
        assert next(p for p in model.Materials[arm.MaterialName].Properties if isinstance(p,ElasticEngineeringConstants)).EngineeringConstantsTemp[0][0].Value==161002
        assert controller.GetStepNames().Length==0 # exact D1 fixture: both steps remain raw keywords
        before=json.load(open(os.path.join(os.path.dirname(pmx.work_directory),'before.json'),encoding='utf-8'))
        assert [str(k.Value.Data) for k in model.CalculixUserKeywords]==[k['text'] for k in before['keywords']]
    else:
        assert model.CompositeLayups.Count>0 and all(l.Active for l in model.CompositeLayups.Values)
        assert any(isinstance(p,ElasticEngineeringConstants) and p.EngineeringConstantsTemp[0][0].Value==12345 or isinstance(p,ElasticLamina) and p.LaminaConstantsTemp[0][0].Value==12345 for m in model.Materials.Values for p in m.Properties)
        step=controller.GetStep('StrictStatic');assert step.InitialTimeIncrement==0.1 and step.SolverType.ToString()=='Spooles'
        assert step.BoundaryConditions.ContainsKey('StrictFixed') and step.Loads['StrictForce'].F2.Value==0.001
controller.ExportToCalculix(os.path.join(pmx.work_directory,'model.inp'))
print('TEST PASSED')
