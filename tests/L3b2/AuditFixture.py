#PrePoMax Python Script
"""Read-only inventory of the original acceptance fixture, with a Windows export."""
import json, os
from System.Reflection import BindingFlags
resolver = pmx.model.GetType().GetField('_modelResolver', BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
gateway = resolver.ModelGateway
controller = gateway.GetType().GetField('_controller', BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gateway)
model = controller.Model
inventory = dict(materials=[dict(name=m.Name, properties=[p.GetType().Name for p in m.Properties]) for m in model.Materials.Values],
                 steps=list(controller.GetStepNames()), user_keywords=model.CalculixUserKeywords.Count,
                 overrides=[dict(text=k.Data, disabled=k.Disabled, suppressed=k.IsSuppressed) for k in model.CalculixKeywordOverrides])
with open(os.path.join(pmx.work_directory, 'fixture.json'), 'w', encoding='utf-8') as stream:
    json.dump(inventory, stream, indent=2)
controller.ExportToCalculix(os.path.join(pmx.work_directory, 'model.inp'))
print('TEST PASSED')
