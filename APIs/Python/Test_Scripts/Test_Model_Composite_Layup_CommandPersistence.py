#PrePoMax Python Script
# PrePoMax.com --script Test_Model_Composite_Layup_CommandPersistence.py -g No -x Yes
#Expected process exit code 0.
# Regression: command targets must survive the same serialization used by PMX/history.
import clr
from System import Array, Object, String
from System.IO import MemoryStream
from CaeGlobals import LegacyFormatter
from PrePoMax import Controller
from PrePoMax.Python.Journal import PythonJournalWriter

command_type = clr.GetClrType(Controller).Assembly.GetType('PrePoMax.Commands.CRemoveCompositeLayup')
command = command_type.GetConstructors()[0].Invoke(Array[Object]([String('EditedCopy')]))
writer = PythonJournalWriter()
command.ToPython(writer)
expected = writer.GetText()
assert "remove('EditedCopy')" in expected
for iteration in range(3):
    stream = MemoryStream()
    # Match the actual L1b PMX formatter: its binder resolves legacy command
    # identities after implementation types moved to PrePoMax.Core.
    formatter = LegacyFormatter.Create()
    formatter.Serialize(stream, command)
    stream.Position = 0
    command = formatter.Deserialize(stream)
    stream.Dispose()
    writer = PythonJournalWriter()
    command.ToPython(writer)
    assert writer.GetText() == expected, writer.GetText()
    assert writer.EmittedCommandCount == 1 and len(writer.Refusals) == 0
    assert command.Name == 'Remove composite layup'
    assert command.GetCommandString().endswith('Remove composite layup: EditedCopy')
print('PASS remove-composite-layup target survives three PMX-style serialization round trips')
print('TEST PASSED')
