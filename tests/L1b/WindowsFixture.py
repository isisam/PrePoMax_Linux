#PrePoMax Python Script
# Build native commands explicitly: the driver itself is not part of the saved history.
import os, json
from System import Activator, Array, Object, Double, String, Boolean
from System.Reflection import BindingFlags
from System.Collections.Generic import List
from PrePoMax.Commands import Command
from CaeGlobals import UnitSystemType, RegionTypeEnum
from CaeModel import Material, Elastic, CompositeLayup, CompositePly, ModelSpaceEnum

resolver = pmx.model.GetType().GetField("_modelResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
gateway = resolver.ModelGateway
controller = gateway.GetType().GetField("_controller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gateway)
assembly = controller.GetType().Assembly
commands = List[Command]()
def execute(name, *args):
    value = Activator.CreateInstance(assembly.GetType("PrePoMax.Commands." + name, True), Array[Object](args))
    assert value.Execute(controller)
    commands.Add(value)

execute("CClear")
execute("CSetNewModelProperties", ModelSpaceEnum.ThreeD, UnitSystemType.MM_TON_S_C)
execute("CImportFile", os.path.join(pmx.work_directory, "fixture.inp"), Boolean(False))
material = Material("ReplayMaterial")
material.AddProperty(Elastic(Array[Array[Double]]([Array[Double]([210000., .3, 0.]) ])))
execute("CAddMaterial", material)
region = controller.GetModelPartNames()[0]
layup = CompositeLayup("ReplayLayup", region, RegionTypeEnum.PartName)
layup.Plies.Add(CompositePly("Ply1", material.Name, 1., 30.))
execute("CAddCompositeLayup", layup)
execute("CRunPythonScript", 'pmx.model.parameters.add("ReplayParameter", 2.0)\nprint("portable Python history executed")', None, None)
controller.SetCommands(commands)
controller.SaveToPmx(os.path.join(pmx.work_directory, "full.pmx"))
controller.ExportToCalculix(os.path.join(pmx.work_directory, "windows.inp"))
with open(os.path.join(pmx.work_directory, "fixture.json"), "w") as stream:
    json.dump(dict(command_count=len(commands), commands=[str(c.GetType().FullName) for c in commands],
                   nodes=len(controller.Model.Mesh.Nodes), elements=len(controller.Model.Mesh.Elements)), stream, indent=2)
print("TEST PASSED")
