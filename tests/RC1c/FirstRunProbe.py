#PrePoMax Python Script
import clr, json, os
from System import Environment
from System.IO import MemoryStream
from System.Reflection import BindingFlags
from System.Runtime.Serialization.Formatters.Binary import BinaryFormatter
from PrePoMax import FrmMain, SolverSettings, SettingsContainer

program = clr.GetClrType(FrmMain).Assembly.GetType('PrePoMax.Program')
main = program.GetField('_mainForm', BindingFlags.Static | BindingFlags.NonPublic).GetValue(None)
controller = main.GetType().GetField('_controller', BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main)
settings = controller.GetType().GetField('_settings', BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller)
expected = os.path.join(str(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
                        'Documents', 'PrePoMax-RCM', 'Work')
assert os.path.isdir(expected), expected
assert settings.Solver.WorkDirectoryForSettingsOnly == expected
cli_override = settings.GetRegenerationWorkDirectory()
settings.SetRegenerationWorkDirectory(None)
assert os.path.normcase(str(pmx.work_directory)) == os.path.normcase(expected), pmx.work_directory
settings.CheckWorkingDirectory()
settings.SetRegenerationWorkDirectory(cli_override)
assert not settings.Solver.UsePmxFolderAsWorkDirectory  # upstream/RC1 preference retained
from PrePoMax import GeneralSettings
assert settings.General.Language == GeneralSettings().Language
assert str(settings.Solver.CalculixDefaultSolverType) == 'Spooles'

def roundtrip(value):
    stream = MemoryStream()
    formatter = BinaryFormatter()
    formatter.Serialize(stream, value)
    stream.Position = 0
    result = formatter.Deserialize(stream)
    stream.Dispose()
    return result

solver = SolverSettings()
field = clr.GetClrType(SolverSettings).GetField('_workDirectory', BindingFlags.Instance | BindingFlags.NonPublic)
field.SetValue(solver, None)  # a settings.bin produced by the old first-run Reset
solver.UsePmxFolderAsWorkDirectory = True
migrated = roundtrip(solver)
assert migrated.WorkDirectoryForSettingsOnly == expected
assert migrated.UsePmxFolderAsWorkDirectory
custom = os.path.dirname(__file__)
solver.WorkDirectoryForSettingsOnly = custom
assert roundtrip(solver).WorkDirectoryForSettingsOnly == custom
missing = os.path.join(custom, 'explicit-missing-directory')
solver.WorkDirectoryForSettingsOnly = missing
assert roundtrip(solver).WorkDirectoryForSettingsOnly == missing
assert not os.path.exists(missing)
container = SettingsContainer()
container.Solver.UsePmxFolderAsWorkDirectory = True
container.General.LastFileName = os.environ['RC1C_FIXTURE']
assert container.GetWorkDirectory() == os.path.dirname(os.environ['RC1C_FIXTURE'])
container.SetRegenerationWorkDirectory(custom)
assert container.GetWorkDirectory() == custom
container.Reset()
assert container.Solver.WorkDirectoryForSettingsOnly == expected
result = dict(status='PASS', default_work_directory=expected, exists=True,
              upstream_use_pmx_folder=False, pmx_folder_opt_in=True,
              regeneration_override=True, null_migration=True,
              explicit_custom_and_missing_paths_preserved=True, reset=True,
              language=settings.General.Language)
json.dump(result, open(os.environ['RC1C_PROBE_OUT'], 'w', encoding='utf-8'), indent=2)
print('FIRST RUN DEFAULT AND SETTINGS MIGRATION PASS')
