#PrePoMax Python Script
"""Read-only Windows reload of Linux pointer/form-created sets and surfaces."""
import os,json
from System.Reflection import BindingFlags
resolver=pmx.model.GetType().GetField('_modelResolver',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pmx.model)
gateway=resolver.ModelGateway
controller=gateway.GetType().GetField('_controller',BindingFlags.Instance|BindingFlags.NonPublic).GetValue(gateway)
mesh=controller.Model.Mesh
membership=json.load(open(os.path.join(os.path.dirname(pmx.work_directory),'membership.json'),encoding='utf-8'))
for entry in membership['nodes']:
    s=mesh.NodeSets[entry['Name']];assert sorted(s.Labels)==entry['ids'] and s.Valid
    assert [str(n.GetType().Name) for n in s.CreationData.Nodes]==entry['history']
for entry in membership['elements']:
    s=mesh.ElementSets[entry['Name']];assert sorted(s.Labels)==entry['ids'] and s.Valid
    assert [str(n.GetType().Name) for n in s.CreationData.Nodes]==entry['history']
for entry in membership['surfaces']:
    s=mesh.Surfaces[entry['Name']];assert sorted(s.FaceIds)==entry['ids'] and s.Valid
    assert abs(s.Area-entry['Area'])<1e-10*max(1,s.Area)
    assert [str(n.GetType().Name) for n in s.CreationData.Nodes]==entry['history']
controller.ExportToCalculix(os.path.join(pmx.work_directory,'model.inp'))
print('TEST PASSED selection membership, creation history, faces and export')
