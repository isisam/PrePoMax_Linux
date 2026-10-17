#PrePoMax Python Script
import os
from System.Reflection import BindingFlags
resolver = pmx.model.GetType().GetField("_modelResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
gateway = resolver.ModelGateway
controller = gateway.GetType().GetField("_controller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gateway)
assert controller.GetActiveCommandCount() == 6
assert len(controller.Model.CompositeLayups) == 1
assert "ReplayParameter" in pmx.model.parameters.get_names()
controller.ExportToCalculix(os.path.join(pmx.work_directory, "windows_reload.inp"))
print("TEST PASSED")
