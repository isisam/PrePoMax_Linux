# Composite layup core (M2a)

Mode A assigns a flat stack to an **existing conforming 3D solid mesh**. The core
is in CaeModel, using CaeMesh geometry and the CalculiX writer. No layup UI,
extrusion, cohesive insertion or input-deck layup reconstruction is provided.

## Model and persistence

`FeModel.CompositeLayups` is an ordered, case-insensitive collection.
`CompositeLayup` owns Name/Active, RegionName/RegionType, retained
CreationIds/CreationData, CoordinateSystemName (null = global), StackingAxis
(1..3), Symmetric, bottom-to-top Plies, and BoundaryTolerance (default 1e-6).
Regions use the existing part, element-set and retained-selection patterns.
Part-derived element sets resolve part IDs to actual element IDs.

Each `CompositePly` has Name, MaterialName, Thickness, AngleDeg, and nullable
ElementsThroughThickness. Thickness is a positive finite length in model units;
angles are finite degrees. There is no equation/unit conversion on these new
numeric values. Symmetric appends the complete reversed list: `[a,b]` becomes
`[a,b,b,a]`; the centre ply is duplicated, not shared.

The new collection is explicitly serialized by FeModel, initialized empty when
reading historical files, and copied with the model. Generated sets, sections
and orientations are never saved. The field is omitted for an empty collection.
**Older builds cannot open new .pmx files that contain layups.** They lack the
new types and reject unknown FeModel fields. Reading old files in this build is
supported and tested; forward compatibility is not promised. Keep the original
file when exchanging with an older build; a solver .inp is a flattened export,
not a lossless editable layup interchange format.

## Angle convention and CalculiX semantics

The material's direction 1 is the fibre. A positive angle is a right-handed
rotation about the positive stacking axis. The zero-degree fibre direction is
the next cyclic CS axis:

| Stacking CS axis | Zero-degree fibre | In-plane second axis | Export rotation axis |
| --- | --- | --- | --- |
| 1 | CS 2 | CS 3 | local 3 |
| 2 | CS 3 | CS 1 | local 3 |
| 3 | CS 1 | CS 2 | local 3 |

These cyclic permutations are right handed. With unit vectors `f`, `g`, `n`
from the table, the final fibre is `cos(theta)*f + sin(theta)*g` and the second
material direction is `-sin(theta)*f + cos(theta)*g`; direction 3 is `n`.
For the global CS and stacking axis 3, +30 degrees has a positive y fibre
component. Under tensile global x stress, the supplied IM7/8552 ply has
**negative** engineering shear strain gamma_xy; -30 degrees reverses its sign.

The [CalculiX 2.21 manual, section 7.102, pp. 559-561](https://www.dhondt.de/ccx_2.21.pdf)
defines rectangular orientation using point a on local X and point b in the
local XY plane, followed by an optional local rotation-axis number and angle.
The implementation was also checked against the installed 2.21 sources
`orientations.f` and `transformatrix.f`: for rectangular orientations a and b
are vectors from the orientation origin; the routine normalizes a, projects b
perpendicular to a, and forms their cross product. It has no rectangular-origin
argument. Therefore export **directions relative to the CS origin**, not the
translated absolute defining points of the mesh coordinate system.

The writer uses `a=f`, `b=g`, followed by `3, theta`. A nonzero CS translation
affects projected stacking coordinates but never the orientation vectors.
Only rectangular, active, finite, nondegenerate coordinate systems are accepted.
Numeric orientation fields use invariant G12 formatting, within CalculiX 2.21's
20-character input-field width. An explicit-vector solver check reproduces the
angle-card result.

## Expansion and validation

`CompositeLayupExpansion.Expand` is the single non-UI classifier used by model
validity, effective section/material assignment, keyword preview and export.
It produces descriptors containing a transient SolidSection, element IDs and
orientation data, without modifying the model.

The stack starts at the minimum node projection onto its stacking axis. Its
total thickness is the sum of the expanded ply thicknesses. The complete
region extent must match that total within `total * BoundaryTolerance`.
A mismatch reports both values and the absolute tolerance. Tolerance must be
finite, nonnegative and less than 0.5; a larger value would make assignment
meaningless. Zero is allowed for exactly aligned meshes.

Every node, including midside nodes, contributes to each element's min/max
projection. An element must fit exactly one interval
`[boundary_i-tol, boundary_i+1+tol]`. Crossing, outside, ambiguous or zero-extent
elements are rejected with counts and up to eight sorted IDs. Empty plies,
missing/inactive materials or parts, nonsolid regions, overlapping active
layups and ordinary material-section coverage are errors. An ordinary-section
overlap identifies the section. Mass sections do not assign a material volume
and may coexist. Inactive layups are ignored.

If ElementsThroughThickness is supplied, it must be positive. Distinct
projected element intervals are clustered within tolerance and must form the
specified number of consecutive bands covering the entire ply. It is only a
mesh check; it does not generate layers. Omit it for unstructured meshes whose
projected intervals do not form uniform bands. Band thicknesses may differ.

Generated names are `LU_<layup>_<index>_<ply>` and
`OR_<layup>_<index>_<ply>`. The 1-based expanded index uses at least two digits;
it is part of the ply token and distinguishes mirrored/repeated names.
`CalculixNameShortener.GetShortenedName` supplies the existing writer's
79-character limit and case-insensitive SHA1-derived eight-hex suffix.
Reserved-set or shortened-name collisions are errors, never silent overwrites.

`GetModelKeywords` adds the generated sets within its existing try/finally
restoration scope. Orientations are appended to the existing Materials group,
before Sections. Transient ply sections are appended to the Sections group.
There is no new top-level group, so existing top-level keyword indices stay
stable. Child edits to previously generated ply objects remain dependent on
their positions/signatures, as with the upstream keyword editor.

`CalSolidSection` accepts an optional **writer-only** orientation name. This
keeps the persisted ordinary SolidSection schema unchanged: only the expansion
owns orientation definitions, so an ordinary section cannot acquire a dangling
orientation reference. Invalid active layups block export before a new .inp
is opened. Existing files at the target path are not deleted on refusal.

Named region/material/CS references retain the upstream missing-reference
behavior after deletion or renaming: validation identifies the missing target;
use replace to repair it. Geometry/mesh changes trigger fresh classification.
Selection-based layups own an internal element set and retain their selection
for command/journal replay; replace/remove cleans up that owned set.
Effective section assignment is recomputed when the collection is nonempty,
since projections also depend on node positions and CS equations.

## Python and journal

```python
plies = [pmx.create.composite_ply('P1', 'IM7_8552', .125, 45, 1),
         pmx.create.composite_ply('P2', 'IM7_8552', .125, 90, 1),
         pmx.create.composite_ply('P3', 'IM7_8552', .125, -45, 1),
         pmx.create.composite_ply('P4', 'IM7_8552', .125, 0, 1)]
region = pmx.create.region('ElementSetName', 'Coupon')
pmx.model.composite_layups.add('Layup', region, plies, symmetric=True)
pmx.model.composite_layups.replace('Layup', 'Layup', region, plies,
    coordinate_system_name=None, stacking_axis=3, symmetric=True,
    boundary_tolerance=1e-6, active=True)
snapshot = pmx.model.composite_layups.get('Layup')
angles = [p.angle_deg for p in snapshot.get_plies(expanded=True)]
pmx.model.composite_layups.remove('Layup')
```

Ply makers and get return detached, read-only API values. Add/replace/remove
route through AddCompositeLayup/ReplaceCompositeLayup/RemoveCompositeLayup
gateway and controller commands. Each command emits replayable Python using
the same region/selection journal emitters as sections. Replace supplies the
complete definition and preserves collection order. Geometric validation is
deferred to validity/export so an invalid definition can be repaired.

## Acceptance and the reference coupon

Run `Run_Composite_Layup_Tests.py --exe <run/PrePoMax.com> --work <empty folder>`
with the existing external Python. LAYUP_REFERENCE and FATCZM_PYTHON can point
at the reference generator and an existing Python with NumPy; no installation
is performed. The tests run the application with `-g No`, and both runtime
solvers with Spooles. Reports and solver outputs remain in the work directory.

The supplied oracle contains six 0.001 mm FATCZM interfaces between solid ply
blocks. M2a has no interface insertion or gap metadata: the exact golden uses
eight independent single-ply layups, with ordinary sections on the separate
interface elements. Symmetric-list tests use a continuous stack. The reference
importer's per-part element formulations are explicitly restored through the
existing part API before comparison. Nodes, connectivity, formulations,
memberships, materials and orientation semantics must match the oracle.

**The original 4 x 2 x 1 mm coupon does not pass the specified CLT gate.**
The unchanged reference and matching writer deck give 60.21798 GPa with
ccx_fat, about 6.01% below 64.07 GPa. The release solver fails the original
FATCZM-interface reference with too many cutbacks and produces no completed
reaction output; the original cannot be a shared stock/fat solver oracle.
A continuous symmetric stack gives 60.50075 GPa on both solvers, 5.57% below
target. The solver test deliberately returns failure;
the 1% tolerance has not been relaxed. Independent +/-30-degree and 90-degree
rotation tests pass. Reducing continuous-stack thickness to 0.01 mm yields
64.387 GPa, within 1%, supporting finite-specimen/edge effects as the cause;
this diagnostic is not substituted for the required original-coupon test.
See M2a_REPORT.md and retained logs for evidence and the unresolved gate.
