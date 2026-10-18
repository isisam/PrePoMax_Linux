# Fatigue CZM (FATCZM) user material

M1 adds a typed mechanical `UserMaterial` property. In the material editor select
**Fatigue CZM (FATCZM)**, then edit the ordered **Data points** table and the
**State variables (DEPVAR)** property. The editor offers to prefix a material name
with `USERFATCZM`. The Python API requires the correct name explicitly:

```python
pmx.model.materials.add("USERFATCZM", pmx.create.fatczm())
pmx.model.materials.add("USERFATCZMPC", pmx.create.fatczm(pre_crack=True))
values = pmx.create.fatczm().get_values()
values["constants"][5] = 0.002  # actual layer thickness, in mm in this example
pmx.model.materials.replace("USERFATCZM", properties=pmx.create.user_material(**values))
```

The **FATCZM preset** property is read-only. Adding **Fatigue CZM (FATCZM)**
sets it to True; importing a 26-constant `USERFATCZM...` block also recognizes
the preset. Edit DEPVAR separately on **Properties**, and constants on **Data points**.

The preset has 26 constants and DEPVAR 6. Defaults reproduce the **IM7 ENF
energy-driver example**, not a calibrated material prediction. Order is taken
from `ccx_fat/fatczm.f` (constant map and `umat_fatczm`) and
`ccx_fat/gen_inp.py` (`write_deck` / `fat_mat`); numeric defaults match
`ccx_fat_win64_v1.1/examples/PrePoMax_keyword_blocks.txt` and
`ENF_fatigue_min.inp`. `apply_patch.py` dispatches the `FATCZM` UMAT and installs
the global increment bookkeeping. CalculiX strips `USER` for UMAT dispatch;
the bookkeeping checks the full `USERFATCZM` prefix. Names are case sensitive.

## Ordered constants

Units below are **N, mm, MPa**. Values and equations use model units. Unit and
description text is metadata; it does not convert numeric values. Convert all
dimensional constants yourself when using another unit system, especially
thickness (6), toughness (3–4), and penalty stiffness (17). Temperature-dependent
UMAT tables and automatic unit conversion are outside this property.

| No. | Name | Default | Unit / meaning |
| ---: | --- | ---: | --- |
| 1 | tIC | 60 | MPa; mode I strength |
| 2 | tIIC | 90 | MPa; mode II strength |
| 3 | GIC | 0.2399 | N/mm; mode I toughness |
| 4 | GIIC | 0.739 | N/mm; mode II toughness |
| 5 | Dint | 0.001 | Elastic-branch end damage; insert default 0.999999 |
| 6 | tc | 0.001 | mm; actual cohesive element thickness |
| 7 | aI | 1 | 1/cycle; mode I alpha |
| 8 | bI | 6.75 | Dimensionless; mode I beta |
| 9 | gI | -1 | Dimensionless; mode I gamma |
| 10 | aII | 1 | 1/cycle; mode II alpha |
| 11 | bII | 5.25 | Dimensionless; mode II beta |
| 12 | gII | -1 | Dimensionless; mode II gamma |
| 13 | dDmax | 0.03 | Maximum absolute damage increment per jump |
| 14 | first fatigue step | 2 | Integer step number; static ramp precedes this |
| 15 | normal axis | 2 | Global axis: 1=X, 2=Y, 3=Z |
| 16 | dNmax | 0 | Cycles/jump; 0 = unlimited |
| 17 | compression penalty | 7503126.303 | N/mm³; 0 = solver automatic value |
| 18 | dDrel | 0.5 | Maximum relative damage increment; 0 = off |
| 19 | pre-crack normal behaviour | 1 | 1 = normal bonding; 0 = contact in compression only |
| 20 | damage variable | 0 | 0 = Hong D; 1 = Camanho–Davila stiffness damage d |
| 21 | ainc | 0 | 1/cycle; initiation alpha, 0 = off |
| 22 | binc | 0 | Dimensionless; initiation exponent |
| 23 | deta | 0 | Maximum initiation increment; 0 = solver default 0.05 |
| 24 | initiation clock | 0 | 0 = local Miner clock; 1 = global pocket clock |
| 25 | F | 1 | 0 = normalized traction; 1 = local energy/Gc |
| 26 | mixed-mode rule | 1 | 0 = Hong Eq. 13; 1 = log-linear displacement share; 2 = log-linear energy share |

Constant 17 is `tIC / (1e-3 * 2 * GIC / tIC)`, rounded as the generator's
`%.10g` output. It is a supplied value, so changing strengths or toughness does
not automatically recompute it. Set it to 0 for the solver's automatic value.
The fatigue law is `dD/dN = a * F^b / (1-D)^g`; gamma -1 multiplies the rate by
`1-D`. It does not select the energy driver (constant 25 does).

## Validation and inserts

- Preset: exactly 26 finite constants; name starts with `USERFATCZM`; DEPVAR ≥ 6.
- Constants 1–6 are positive, with `0 < Dint < 1`; alpha values are positive
  (the UMAT evaluates their logarithms);
  dDmax is positive. First fatigue step is a positive integer; normal axis is 1–3.
  Constants 16–18 are nonnegative. Discrete options are checked against the table.
- A positive constant 21 enables initiation: provide constants 21–24 and DEPVAR ≥ 8.
  The 26-constant preset can be edited for this; DEPVAR defaults to 6.
- All active `USERFATCZM...` materials must agree on constants 13, 14 and 18 because
  the solver uses global cycle bookkeeping. Model validity and export check this.
- A name starting with `USERFATCZMPC` requires Dint = 0.999999, matching the generator.
  The UMAT actually treats **any Dint ≥ 0.5** as fully separated; the PC suffix is
  a generator convention, not the solver's pre-crack switch. For opening inserts
  set **both constant 5 = 0.999999 and constant 19 = 0** (DCB/MMB); the ENF preset uses 1.
  Changing constant 19 alone leaves a default interface intact. Verify an opening
  pre-crack with a one-element check: D = 1 and zero opening reaction.
- CalculiX 2.21 reads `n/8+1` data lines: a FATCZM constant count must **not** be
  divisible by 8. Generic FATCZM data need at least the generator's 19 baseline
  constants; append a dummy zero when necessary. Optional bridging constants
  27–29 belong in a generic user material, not this 26-constant preset.

For other UMATs use `pmx.create.user_material(constants, state_variables=0,
names=None, descriptions=None)` or **User Material** in the editor. Constants may
be equation strings. Generic counts divisible by 8 are permitted for other
solvers; respect the target solver's input-reader rules. `fatczm=True` explicitly
enables the preset validation and metadata. An imported 26-constant
`USERFATCZM...` block is recognized as the preset; other supported mechanical
blocks become generic properties. Unsupported keyword options stay user keywords.

Export writes `*USER MATERIAL, CONSTANTS=n`, eight values per line, followed by
`*DEPVAR` and its integer count. Metadata is preserved in `.pmx` and journal/API
replay, since `.inp` stores only the ordered numeric values. New builds read old
`.pmx` files without a schema migration; older unmodified builds cannot read the
new `UserMaterial` class. Use `.inp` export for exchange with those builds.

## Required model and step setup (outside M1)

M1 does not create a cohesive mesh, assign sections, or configure fatigue steps.
Use a single layer of **linear C3D8**, one element through its thickness, sharing
nodes with the adherends or tied appropriately. Constant 6 must match that layer;
constant 15 is a global normal axis. FATCZM does not use `*ORIENTATION`. Use small
strains with Nlgeom off. Curved cohesive layers need a different UMAT implementation.

1. Step 1 ramps to the peak fatigue load.
2. Step 2 keeps the peak load and uses **`*STATIC, DIRECT`**. One increment is
   one cycle jump: initial increment 1, time period = desired number of jumps,
   max increments ≥ jumps + 10. Constant 14 = 2 for this two-step arrangement.
3. Select **Spooles**, including existing steps. The bundled executable has no
   Pardiso or PaStiX. A run warning lists incompatible settings and active steps;
   explicit selections are never silently changed. Batch mode refuses that prompt.
4. Request SDV output. DEPVAR 6 means: D, dD/dN, dN, F, normal separation,
   shear separation. With initiation, SDV7–8 are eta and its rate.
5. Use one fatigue job per work directory: `fatczm_jumps.log` has a fixed name.
   Its third column is the cycle count at the end of the **previous** increment.

The package ENF example has 300 fatigue jumps. Element 2368 first fully fails at
jump 100, ending at N = 624.92741 (column 3 of row `2 101`). The final cycle count
is 1318.7869 + 2.1898811 = 1320.9767811. These are scaled demonstration cycles
(alpha = 1), not a calibrated lifetime.

## Headless verification

`APIs/Python/Test_Scripts/Test_Model_User_Material.py` follows the standard batch
runner convention. It calls the original generator using an existing Python with
numpy and compares the material block exactly after whitespace normalization. It
also imports both package materials, verifies typed properties, exports them and
compares their blocks, checks refusals, equations, metadata and journal maker replay.
Set `FATCZM_SOURCE`, `FATCZM_PACKAGE`, and `FATCZM_PYTHON` for another workstation.
The test saves a typed `.pmx` for a separate fresh-process reload check.

`Test_Solver_Ccx_Fat_Defaults.py` checks reset/discovery, relative storage and saved
choice preservation. It writes an isolated Spooles settings file for solver tests.
`Test_Solver_Ccx_Fat_Warning.py` expects a refused batch prompt before submission.
No GUI acceptance is implied by these headless tests.
