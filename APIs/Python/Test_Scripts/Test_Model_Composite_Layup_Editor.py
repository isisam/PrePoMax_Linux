#PrePoMax Python Script
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Composite_Layup_Editor.py -g No -x Yes -w <dir>
#Expected process exit code 0.
# Run with -g No -x Yes. Never calls Show, ShowDialog, Application.Run or creates FrmMain.
import json
import os
import sys
import traceback
sys.path.insert(0, os.path.dirname(__file__))
from Composite_Layup_Utils import check, mesh, plies
from System.Reflection import BindingFlags
from System import Int32, Array, Object, String, Boolean
from System.Threading import Thread, ThreadStart, ApartmentState
from CaeModel import CompositeLayupExpansion
from CaeGlobals import RegionTypeEnum, NamedClass
from PrePoMax import CompositeLayupEditorData, ViewCompositeLayup, ViewGeometryModelResults
from PrePoMax.Forms import FrmCompositeLayup
from PrePoMax.Python.Journal import PythonJournalWriter

region = mesh(pmx, "M2b_editor", [0, .5, 1.])
pmx.model.coordinate_systems.add("Rect", [0, 0, 0], [1, 0, 0], [0, 1, 0])
pmx.model.coordinate_systems.add("Cylinder", [0, 0, 0], [1, 0, 0], [0, 1, 0], coordinate_system_type="Cylindrical")
pmx.model.composite_layups.add("UI", region, plies(pmx, [0, 90], .5), coordinate_system_name="Rect", boundary_tolerance=2e-6)
resolver = pmx.model.GetType().GetField("_modelResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
model = resolver.GetModel()
source = model.CompositeLayups["UI"]

def snapshot(layup):
    return dict(name=layup.Name, active=layup.Active, region_name=layup.RegionName, region_type=str(layup.RegionType),
                coordinate_system=layup.CoordinateSystemName, axis=layup.StackingAxis,
                symmetric=layup.Symmetric, tolerance=layup.BoundaryTolerance,
                creation_ids=None if layup.CreationIds is None else list(layup.CreationIds),
                plies=[dict(name=p.Name, material=p.MaterialName, thickness=p.Thickness, angle=p.AngleDeg,
                            elements=p.ElementsThroughThickness) for p in layup.Plies])

before = snapshot(source)
data = CompositeLayupEditorData(source)
data.SetCell(0, 2, "0.05 cm")
check(abs(data.Layup.Plies[0].Thickness - .5) < 1e-12, "length converter to model mm")
data.SetCell(0, 3, "1.5707963267948966 rad")
check(abs(data.Layup.Plies[0].AngleDeg - 90.) < 1e-10, "angle converter to degrees")
data.SetCell(0, 4, "")
check(data.Layup.Plies[0].ElementsThroughThickness is None, "optional elements blank")
for column, value in [(0, ""), (2, "-1 mm"), (2, "NaN"), (3, "NaN"), (4, "0"), (4, "1.5")]:
    try:
        data.SetCell(0, column, value)
    except Exception:
        pass
    else:
        raise Exception("TEST FAILED: invalid cell accepted " + str((column, value)))
data.SetCell(0, 3, "0 deg")
data.SetCell(0, 4, "1")
index = data.Duplicate(0)
data.SetCell(index, 0, "Independent")
data.SetCell(index, 3, "-45 deg")
check(source.Plies[0].AngleDeg == 0 and data.Layup.Plies[0].AngleDeg == 0, "duplicate row is independent")
index = data.Move(index, 1)
check(data.Layup.Plies[index].Name == "Independent", "move down")
data.Move(index, -1)
data.Reverse()
data.Reverse()
data.Delete(1)
index = data.Add(1, "IM7_8552")
data.Delete(index)
check(snapshot(data.ReadBack()) == before and snapshot(source) == before, "all table operations and detached readback")
errors, expanded = data.Validate(model, "UI")
check(len(errors) == 0 and len(expanded) == 2, "editor uses core classifier")
data.SetCell(0, 2, ".4 mm")
errors, expanded = data.Validate(model, "UI")
check(any("thickness" in e.lower() for e in errors), "pre-OK thickness validation")
data.SetCell(1, 2, ".6 mm")
errors, expanded = data.Validate(model, "UI")
check(any("crossing" in e.lower() for e in errors), "pre-OK boundary crossing validation")
data.SetCell(0, 2, ".5 mm")
data.SetCell(1, 2, ".5 mm")
pmx.model.sections.add_solid("Ordinary", "IM7_8552", region)
errors, expanded = data.Validate(model, "UI")
check(any("Ordinary" in e for e in errors), "pre-OK ordinary section overlap")
pmx.model.sections.remove("Ordinary")
view = ViewCompositeLayup(data.Layup)
view.Populate(model)
check(view.CoordinateSystemName == "Rect", "view loads API CS")
choices = [v.Value for v in view.DynamicCustomTypeDescriptor.GetProperty("CoordinateSystemName").StatandardValues]
check("Global" in choices and "Rect" in choices and "Cylinder" not in choices, "only rectangular/global CS dropdown")
check([v.Value for v in view.DynamicCustomTypeDescriptor.GetProperty("StackingAxis").StatandardValues] == [1, 2, 3], "axis dropdown")
view.CoordinateSystemName = "Global"
check(view.GetBase().CoordinateSystemName is None, "Global maps to null")
view.CoordinateSystemName = "Rect"
print("PASS table add/insert/delete/move/duplicate/reverse, converters and pre-OK validation")

# Construct/fill/read/dispose the real WinForms editor on an STA thread, without showing it.
form_result = []
def form_check():
    try:
        form = FrmCompositeLayup(None)
        try:
            form.LoadFromModel(model, source, "UI")
            check(not form.Visible, "form must remain invisible")
            check(snapshot(form.ReadBack()) == before, "real form round trip")
            grid = form.GetType().GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form)
            check(grid.Rows.Count == 2 and grid.Columns.Count == 5, "real ply table populated")
            summary = form.GetType().GetField("_summary", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form)
            check(summary.Text.startswith("Valid:"), "real form live summary")
            invalid = CompositeLayupEditorData(source).ReadBack()
            invalid.CoordinateSystemName = "MissingCS"
            form.LoadFromModel(model, invalid, "UI")
            check(form.ReadBack().CoordinateSystemName == "MissingCS" and "MissingCS" in summary.Text,
                  "invalid reference stays editable without silently falling back to Global")
            form.LoadFromModel(model, source, "UI")
            # Use the exact form edit session, then rebuild its controls, as the table buttons do.
            session = form.GetType().GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form)
            session.SetCell(0, 3, "-30 deg")
            form.GetType().GetMethod("FillRows", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, [Int32(0)])
            check(form.ReadBack().Plies[0].AngleDeg == -30, "form reads edited session")
            check(source.Plies[0].AngleDeg == 0 and not form.Visible, "cancel-equivalent edit leaves model intact")
        finally:
            form.Dispose()
        form_result.append(None)
    except Exception:
        form_result.append(traceback.format_exc())
thread = Thread(ThreadStart(form_check))
thread.SetApartmentState(ApartmentState.STA)
thread.Start()
thread.Join(30000)
check(not thread.IsAlive and form_result == [None], "headless real form: " + str(form_result))
print("PASS real FrmCompositeLayup constructed/filled/edited/read/disposed on STA, never shown")

# Replay actual command journal methods through the public gateway, as session journals do.
import clr
assembly = clr.GetClrType(FrmCompositeLayup).Assembly
journal_fragments = []
def emitted(command_name, arguments):
    command_type = assembly.GetType("PrePoMax.Commands." + command_name)
    command = command_type.GetConstructors()[0].Invoke(Array[Object](arguments))
    writer = PythonJournalWriter()
    command.ToPython(writer)
    text = writer.GetText()
    check(writer.EmittedCommandCount == 1 and len(writer.Refusals) == 0, command_name + " journal emission")
    journal_fragments.append(text)
    exec(text, {"pmx": pmx})
    return text

emitted("CDuplicateCompositeLayups", [Array[String](["UI"])])
copy_name = next(n for n in pmx.model.composite_layups.get_names() if n != "UI")
copy = model.CompositeLayups[copy_name]
copy.Plies[0].AngleDeg = -17
check(source.Plies[0].AngleDeg == 0, "controller duplicate deep clones ply values")
copy.Plies[0].AngleDeg = 0
emitted("CActivateDeactivateMultiple", [Array[NamedClass]([copy]), Boolean(False), Array[String]([None])])
check(not model.CompositeLayups[copy_name].Active, "deactivate through recorded gateway")
emitted("CRenamePre", [source.GetType(), copy_name, "CopyRenamed", None, ViewGeometryModelResults.Model])
check("CopyRenamed" in list(pmx.model.composite_layups.get_names()), "recorded layup rename")
pmx.model.composite_layups.activate("CopyRenamed")
check(model.CompositeLayups["CopyRenamed"].Active, "activate scalar API")
emitted("CRemoveCompositeLayup", ["CopyRenamed"])
with open(os.path.join(pmx.work_directory, "M2b_command_journal.py"), "w") as stream:
    stream.write("# Actual command fragments replayed by this test after creating layup UI.\n" + "\n".join(journal_fragments))
pmx.model.composite_layups.remove("UI")
selection = pmx.create.selection_by_ids("femodel", "elements", "elements", list(range(1, 17)))
pmx.model.composite_layups.add("Selected", selection, plies(pmx, [0, 90], .5))
pmx.model.composite_layups.duplicate("Selected")
selected_copy = next(n for n in pmx.model.composite_layups.get_names() if n != "Selected")
set_a = model.CompositeLayups["Selected"].RegionName
set_b = model.CompositeLayups[selected_copy].RegionName
check(set_a != set_b, "duplicate must own a distinct internal set")
pmx.model.composite_layups.remove("Selected")
check(model.Mesh.ElementSets.ContainsKey(set_b), "deleting source keeps duplicate selection")
pmx.model.composite_layups.remove(selected_copy)
check(not model.Mesh.ElementSets.ContainsKey(set_a) and not model.Mesh.ElementSets.ContainsKey(set_b), "owned set cleanup")
print("PASS recorded duplicate/rename/activate/deactivate/remove and retained-selection ownership")

# Rename dependencies through the same controller paths the UI uses; deletion must invalidate.
pmx.model.composite_layups.add("UI", region, plies(pmx, [0, 90], .5), coordinate_system_name="Rect", boundary_tolerance=2e-6)
pmx.model.materials.rename("IM7_8552", "RenamedMaterial")
check(all(p.MaterialName == "RenamedMaterial" for p in model.CompositeLayups["UI"].Plies), "material rename follows plies")
pmx.model.coordinate_systems.rename("Rect", "RenamedCS")
check(model.CompositeLayups["UI"].CoordinateSystemName == "RenamedCS", "CS rename follows layup")
pmx.model.element_sets.rename("COUPON", "RenamedRegion")
check(model.CompositeLayups["UI"].RegionName == "RenamedRegion", "element set rename follows layup")
part_name = list(pmx.model.parts.get_names())[0]
pmx.model.composite_layups.remove("UI")
pmx.model.composite_layups.add("PartLayup", pmx.create.region("PartName", part_name),
    [pmx.create.composite_ply("Full", "RenamedMaterial", 1, 0, 2)], coordinate_system_name="RenamedCS")
pmx.model.parts.rename(part_name, "RenamedPart")
check(model.CompositeLayups["PartLayup"].RegionName == "RenamedPart", "part rename follows layup")
pmx.model.parts.remove("RenamedPart")
check(not model.CompositeLayups["PartLayup"].Valid, "deleted part invalidates layup")
pmx.model.composite_layups.remove("PartLayup")
# Restore a clean conforming mesh for remaining deletion tests and persistence fixture.
region = mesh(pmx, "M2b_persist", [0, .5, 1.])
pmx.model.coordinate_systems.add("Rect", [0, 0, 0], [1, 0, 0], [0, 1, 0])
pmx.model.composite_layups.add("Persist", region, plies(pmx, [30, -30], .5), coordinate_system_name="Rect", boundary_tolerance=3e-6)
pmx.model.materials.remove("IM7_8552")
check(not model.CompositeLayups["Persist"].Valid, "deleted material invalidates")
from Composite_Layup_Utils import material
material(pmx)
pmx.model.coordinate_systems.remove("Rect")
check(not model.CompositeLayups["Persist"].Valid, "deleted CS invalidates")
pmx.model.coordinate_systems.add("Rect", [0, 0, 0], [1, 0, 0], [0, 1, 0])
pmx.model.element_sets.remove("COUPON")
check(not model.CompositeLayups["Persist"].Valid, "deleted element set invalidates")
pmx.model.element_sets.add("COUPON", pmx.create.selection_by_ids("femodel", "elements", "elements", list(range(1, 17))))
check(len(CompositeLayupExpansion.Expand(model)) == 2, "repair restores core expansion")
value = model.CompositeLayups["Persist"]
with open(os.path.join(pmx.work_directory, "M2b_snapshot.json"), "w") as stream:
    json.dump(snapshot(value), stream, sort_keys=True)
pmx.save(os.path.join(pmx.work_directory, "M2b_layup.pmx"))
print("PASS rename/delete material/CS/part/element-set dependencies; Python-created persistence fixture saved")
print("TEST PASSED")
