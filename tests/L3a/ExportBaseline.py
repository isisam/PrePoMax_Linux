#PrePoMax Python Script
import os
from System.Reflection import BindingFlags
resolver = pmx.model.GetType().GetField("_modelResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
gateway = resolver.ModelGateway
controller = gateway.GetType().GetField("_controller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gateway)
controller.ExportToCalculix(os.path.join(pmx.work_directory, "model.inp"))
print("TEST PASSED")
