#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#  PrePoMax.com --script Test_Solver_Ccx_Fat_Defaults.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
# Headless settings/persistence checks. Writes only an isolated test settings file.
import os
import clr
clr.AddReference("PrePoMax")
from PrePoMax import SolverSettings, SettingsContainer
from CaeModel import SolverTypeEnum
from System.IO import MemoryStream
from System.Reflection import BindingFlags
from System.Runtime.Serialization.Formatters.Binary import BinaryFormatter


def check(value, message):
    if not value:
        raise Exception("TEST FAILED: " + message)


def clone(value):
    stream = MemoryStream()
    try:
        formatter = BinaryFormatter()
        formatter.Serialize(stream, value)
        stream.Position = 0
        return formatter.Deserialize(stream)
    finally:
        stream.Dispose()


settings = SolverSettings()
check(os.path.basename(settings.CalculixExe).lower() == "ccx_fat.exe", "reset must discover bundled solver")
check(settings.CalculixDefaultSolverType == SolverTypeEnum.Spooles, "reset matrix solver")
field = settings.GetType().GetField("_calculixExe", BindingFlags.Instance | BindingFlags.NonPublic)
check(field.GetValue(settings) == r"#\Solver\ccx_fat.exe", "path must be application relative")
official = os.path.join(os.path.dirname(settings.CalculixExe), "ccx_dynamic.exe")
settings.CalculixExe = official
settings.CalculixDefaultSolverType = SolverTypeEnum.PaStiX
restored = clone(settings)
check(restored.CalculixExe == official, "explicit executable must survive deserialization")
check(restored.CalculixDefaultSolverType == SolverTypeEnum.PaStiX, "explicit matrix solver must survive")
field.SetValue(settings, None)
restored = clone(settings)
check(os.path.basename(restored.CalculixExe).lower() == "ccx_fat.exe", "empty saved executable discovery")
check(restored.CalculixDefaultSolverType == SolverTypeEnum.PaStiX, "saved matrix choice must win discovery")
settings.Reset()
check(settings.CalculixDefaultSolverType == SolverTypeEnum.Spooles, "reset must restore bundled default")
isolated = SettingsContainer()
isolated.SaveToFile(os.path.join(pmx.work_directory, "M1_spooles_settings.bin"))
print("PASS reset, application-relative storage, serialization and explicit-choice preservation")
print("TEST PASSED")
