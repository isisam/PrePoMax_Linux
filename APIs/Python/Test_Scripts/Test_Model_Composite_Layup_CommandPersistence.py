# Regression: command targets must survive the same serialization used by PMX/history.
import clr
from System import Array, Object, String
from System.IO import MemoryStream
from System.Runtime.Serialization.Formatters.Binary import BinaryFormatter
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
    formatter = BinaryFormatter()
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
