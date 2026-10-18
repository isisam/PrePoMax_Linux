#PrePoMax Python Script
"""Read-only Windows verification of the actual Linux form-edited model."""
import os
from System.Reflection import BindingFlags
from CaeModel import Elastic, ElasticEngineeringConstants, ElasticWithDensity, UserMaterial, ElementFieldOutput
resolver = pmx.model.GetType().GetField("_modelResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
gateway = resolver.ModelGateway
controller = gateway.GetType().GetField("_controller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gateway)
model = controller.Model
assert len(model.Mesh.NodeSets['L3FormsNodes'].Labels) == 4
assert list(model.Mesh.ElementSets['L3FormsElements'].Labels) == [1]
surface = model.Mesh.Surfaces['L3FormsSurface']
assert surface.Area > 0 and surface.Valid, 'Surface must contain a real element face'
assert model.Sections.ContainsKey('L3FormsSection')
elastic_changed = False
fat_changed = False
for material in model.Materials.Values:
    for prop in material.Properties:
        if isinstance(prop, Elastic): elastic_changed = elastic_changed or prop.YoungsPoissonsTemp[0][0].Value == 12345
        if isinstance(prop, ElasticEngineeringConstants): elastic_changed = elastic_changed or prop.EngineeringConstantsTemp[0][0].Value == 12345
        if isinstance(prop, ElasticWithDensity): elastic_changed = elastic_changed or prop.YoungsModulus.Value == 12345
        if isinstance(prop, UserMaterial) and prop.Fatczm: fat_changed = fat_changed or prop.Constants[12].Value.Value == 0.02
assert elastic_changed and fat_changed
fatigue = controller.GetStep('L3FormsFatigue')
assert fatigue.IncrementationType.ToString() == 'Direct' and fatigue.Direct
assert fatigue.InitialTimeIncrement == 1 and fatigue.TimePeriod == 300 and fatigue.MaxIncrements >= 310
assert fatigue.SolverType.ToString() == 'Spooles' and not fatigue.Nlgeom
assert any(isinstance(o, ElementFieldOutput) and 'SDV' in o.Variables.ToString() for o in fatigue.FieldOutputs.Values)
ramp = controller.GetStep('L3FormsRamp')
assert ramp.BoundaryConditions.ContainsKey('L3FormsFixed')
assert ramp.Loads['L3FormsForce'].F2.Value == 1
controller.ExportToCalculix(os.path.join(pmx.work_directory, 'model.inp'))
print('TEST PASSED')
