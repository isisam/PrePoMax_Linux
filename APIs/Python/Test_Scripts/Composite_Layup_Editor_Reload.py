#PrePoMax Python Script
import json
import os
from System.Reflection import BindingFlags
resolver = pmx.model.GetType().GetField("_modelResolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pmx.model)
model = resolver.GetModel()
value = model.CompositeLayups["Persist"]
actual = dict(name=value.Name, active=value.Active, region_name=value.RegionName, region_type=str(value.RegionType),
              coordinate_system=value.CoordinateSystemName, axis=value.StackingAxis,
              symmetric=value.Symmetric, tolerance=value.BoundaryTolerance,
              creation_ids=None if value.CreationIds is None else list(value.CreationIds),
              plies=[dict(name=p.Name, material=p.MaterialName, thickness=p.Thickness, angle=p.AngleDeg,
                          elements=p.ElementsThroughThickness) for p in value.Plies])
with open(os.path.join(pmx.work_directory, "M2b_snapshot.json")) as stream:
    expected = json.load(stream)
assert actual == expected, (actual, expected)
pmx.export_to_calculix(os.path.join(pmx.work_directory, "M2b_reloaded.inp"))
print("PASS fresh-process Python -> pmx -> reopened data round-trip unchanged")
print("TEST PASSED")
