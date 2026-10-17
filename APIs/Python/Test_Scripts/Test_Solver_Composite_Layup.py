#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Solver_Composite_Layup.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import os
import sys
import math
import json
sys.path.insert(0, os.path.dirname(__file__))
from Composite_Layup_Utils import check, mesh, plies, steps, solve, ply_cards
from System.Windows.Forms import Application

work = pmx.work_directory
solvers = {
    "fat": os.path.join(Application.StartupPath, "Solver", "ccx_fat.exe"),
    "release": os.path.join(Application.StartupPath, "Solver", "ccx_dynamic.exe"),
}
# The runtime keeps the release Solver folder intact. Resolve its known filename if renamed upstream.
if not os.path.isfile(solvers["release"]):
    candidates = [n for n in os.listdir(os.path.join(Application.StartupPath, "Solver"))
                  if n.lower().startswith("ccx") and n.lower().endswith(".exe") and n.lower() != "ccx_fat.exe"]
    check(len(candidates) == 1, "set release ccx filename explicitly: " + str(candidates))
    solvers["release"] = os.path.join(Application.StartupPath, "Solver", candidates[0])
results = {}
failures = []


def run_case(label, angles, thickness, symmetric=False, rotation=0, direction="x", shear_free=False,
             nx=4, ny=2, lx=4., ly=2.):
    count = len(angles) * (2 if symmetric else 1)
    region = mesh(pmx, label, [i * thickness for i in range(count + 1)], nx=nx, ny=ny, lx=lx, ly=ly)
    cs = None
    if rotation:
        # Translation must not enter the orientation a/b vectors.
        pmx.model.coordinate_systems.add("Rotated", [10, 20, 30], [10, 21, 30], [9, 20, 30])
        cs = "Rotated"
    pmx.model.composite_layups.add(label, region, plies(pmx, angles, thickness),
                                  coordinate_system_name=cs, symmetric=symmetric)
    length = lx if direction == "x" else ly
    steps(pmx, length=length, shear_free=shear_free, direction=direction)
    path = os.path.join(work, label + ".inp")
    pmx.export_to_calculix(path)
    result = {}
    for solver, executable in solvers.items():
        check(os.path.isfile(executable), "missing solver " + executable)
        result[solver] = solve(executable, path, label + "_" + solver,
                               area=(ly if direction == "x" else lx) * count * thickness,
                               length=length, direction=direction)
    check(abs(result["fat"][0] / result["release"][0] - 1) < 1E-5, label + " solver disagreement")
    results[label] = result
    return result["fat"]


# Same 4 x 2 x 1 mm geometry and IM7/8552 material as the oracle; continuous stack (M2a mode A).
# A thinner coupon also checks the CLT membrane limit if the 1 mm coupon shows finite-thickness effects.
e, _ = run_case("Balanced", [45, 90, -45, 0], .125, symmetric=True)
if abs(e / 64070. - 1) > .01:
    failures.append("CLT 64.07 GPa (1%): E=" + str(e))
print("CLT [45/90/-45/0]s E_x=%.8f GPa error=%.6f%%" % (e / 1000, 100 * abs(e / 64070 - 1)))

# Plane-stress analytic compliance for uniaxial x stress. Positive theta gives negative gamma_xy.
c, s = math.cos(math.pi / 6), math.sin(math.pi / 6)
s11, s22, s12, s66 = 1 / 171000., 1 / 9100., -.32 / 171000., 1 / 5300.
sx = s11 * c**4 + s22 * s**4 + (2 * s12 + s66) * c*c*s*s
sxy = 2*c*s*((s11-s12)*c*c - (s22-s12)*s*s) - s66*c*s*(c*c-s*s)
analytic_gamma = sxy / sx * .0001
gammas = []
for sign, label in ((1, "Plus30"), (-1, "Minus30")):
    e, gamma = run_case(label, [30 * sign], .125, shear_free=True)
    check(gamma is not None and gamma * analytic_gamma * sign > 0,
          label + " shear sign: FE=" + str(gamma) + " analytic=" + str(sign * analytic_gamma))
    check(abs(gamma / (sign * analytic_gamma) - 1) < .01, label + " shear magnitude")
    gammas.append(gamma)
check(abs(gammas[0] + gammas[1]) < 1E-8, "angle sign antisymmetry")
print("PASS +/-30 shear sign and magnitude; analytic gamma(+30)=" + str(analytic_gamma))

# An anisotropic symmetric stack proves the 90-degree CS rotation swaps Ex/Ey.
moduli = {}
for rotation in (0, 90):
    for direction in ("x", "y"):
        label = "Axis_%d_%s" % (rotation, direction)
        e, _ = run_case(label, [0], .5, symmetric=True, rotation=rotation, direction=direction)
        moduli[(rotation, direction)] = e
check(abs(moduli[(0, "x")] / moduli[(90, "y")] - 1) < 1E-5, "Ex/Ey rotation swap")
check(abs(moduli[(0, "y")] / moduli[(90, "x")] - 1) < 1E-5, "Ey/Ex rotation swap")
check(abs(moduli[(0, "x")] / 171000 - 1) < .01 and abs(moduli[(0, "y")] / 9100 - 1) < .01,
      "unrotated analytic orthotropic moduli")
print("PASS symmetric anisotropic stack with translated, 90-degree rotated CS: Ex/Ey swap")
with open(os.path.join(work, "M2a_solver_results.json"), "w") as stream:
    json.dump({"executables": solvers, "results": results, "analytic_gamma_plus30": analytic_gamma,
               "failures": failures}, stream, indent=2)
check(not failures, "; ".join(failures))
print("TEST PASSED")
