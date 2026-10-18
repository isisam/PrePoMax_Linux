#PrePoMax Python Script
import os,json
from System.Reflection import BindingFlags
from System import Action,String,Array,Int32
from CaeModel import FeModel,ModelSpaceEnum
from vtkControl import vtkEdgesVisibility
resolver=pmx.model.GetType().GetField("_modelResolver",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pmx.model)
gateway=resolver.ModelGateway
controller=gateway.GetType().GetField("_controller",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(gateway)
out=pmx.work_directory
model=FeModel("DCB",None,True);model.Properties.ModelSpace=ModelSpaceEnum.ThreeD
errors,raw=model.ImportModelFromInpFileForViewing(os.path.join(out,"dcb.inp"),Action[String](lambda s:None))
assert errors is None or errors.Count==0
controller.GetType().GetField("_model",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,model)
controller.DrawModel(True)
print("Viewer-only legacy INP import; raw USER MATERIAL retained; no solver/material conversion")
assert len(controller.Model.Mesh.Nodes)==11580 and len(controller.Model.Mesh.Elements)==5100
controller.SaveToPmx(os.path.join(out,"dcb.pmx"))
pmx.view.current="femodel";pmx.view.set_orientation("Front");pmx.view.zoom_to_fit()
pmx.view.save_image(os.path.join(out,"Windows-mesh.png"))
pmx.results.open(os.path.join(out,"dcb.frd"));pmx.view.current="results"
result=pmx.results.current
fields=list(controller.CurrentResult.GetAllFieldData());disp={(f.StepId,f.StepIncrementId) for f in fields if f.Name=="DISP"}
common=max([f for f in fields if f.Name=="STRESS" and (f.StepId,f.StepIncrementId) in disp],key=lambda f:(f.StepId,f.StepIncrementId))
step,inc=common.StepId,common.StepIncrementId
pmx.view.set_result_increment(step,inc)
pmx.view.result_type="ColorContours";pmx.view.deformation_type="Undeformed"
pmx.view.set_orientation("Front");pmx.view.zoom_to_fit()
controller.CurrentEdgesVisibility=vtkEdgesVisibility.NoEdges
pmx.view.set_result_field("STRESS","MISES");pmx.view.save_image(os.path.join(out,"Windows-S.png"))
pmx.view.set_result_field("DISP","ALL");pmx.view.save_image(os.path.join(out,"Windows-U.png"))
pmx.view.deformation_type="TrueScale";pmx.view.zoom_to_fit();pmx.view.save_image(os.path.join(out,"Windows-U-deformed.png"))
ids=Array[Int32](sorted(controller.Model.Mesh.Nodes.keys()))
sv=controller.CurrentResult.GetValues(controller.CurrentResult.GetFieldData("STRESS","MISES",step,inc),ids)
uv=controller.CurrentResult.GetValues(controller.CurrentResult.GetFieldData("DISP","ALL",step,inc),ids)
assert sv is not None and uv is not None
with open(os.path.join(out,"windows-view.json"),'w') as f:json.dump(dict(view="Front",step=step,increment=inc,time=common.Time,camera=list(pmx.view.get_parameters()),nodes=11580,elements=5100,S=[min(sv),max(sv)],U=[min(uv),max(uv)]),f,indent=2)
print("TEST PASSED: DCB full PMX, FRD S/U/deformation and Front screenshots")
