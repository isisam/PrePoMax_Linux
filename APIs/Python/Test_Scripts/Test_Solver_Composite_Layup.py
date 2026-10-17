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
from Composite_Layup_Utils import (check, mesh, plies, steps, solve, LAMINA,
                                  clt, periodic_deck, solve_periodic)
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


# Preserve the original finite-coupon result as a diagnostic, not a CLT oracle.
run_case("Balanced", [45, 90, -45, 0], .125, symmetric=True)

# Symmetric membrane cells, periodic on both in-plane pairs of faces and free
# top/bottom. Free transverse/shear macro DOFs enforce zero transverse/shear
# resultants. Refinement must preserve Ex, Ey and nu_xy as well as match CLT.
periodic_results = {}
for name, half_angles in (("Balanced", [45,90,-45,0]), ("Zero8", [0]*4),
                          ("Ninety8", [90]*4), ("PlusMinus45_2s", [45,-45]*2)):
    half_data = [(a,.125) for a in half_angles]
    ply_data = half_data + list(reversed(half_data))
    target = clt(ply_data)
    case = {"plies": ply_data, "lamina": LAMINA, "CLT": target, "meshes": {}}
    previous = None
    for n in (2,4):
        values = {}
        for direction in ("x","y"):
            label = "PBC_%s_%d_%s" % (name,n,direction)
            levels = [0.]
            for _, t in ply_data:
                levels.append(levels[-1]+t)
            region = mesh(pmx, label, levels, nx=n, ny=n)
            fe_plies = [pmx.create.composite_ply("P%02d" % (i+1), "IM7_8552", t, a, 1)
                        for i,(a,t) in enumerate(half_data)]
            pmx.model.composite_layups.add(label, region, fe_plies, symmetric=True)
            path = os.path.join(work,label+".inp")
            pmx.export_to_calculix(path)
            periodic_deck(path,n,n,levels,direction=direction)
            for solver, executable in solvers.items():
                value = solve_periodic(executable,path,label+"_"+solver,n,n,levels,direction=direction)
                values.setdefault(solver,{})["E"+direction] = value["E"]
                values[solver][direction+"_solve"] = value
                if direction == "x":
                    values[solver]["nu_xy"] = value["nu"]
        for solver, value in values.items():
            errors = {key: abs(value[key]/target[key]-1) for key in ("Ex","Ey","nu_xy")}
            value["relative_errors"] = errors
            for key, error in errors.items():
                check(error < .01, "%s %s %s CLT 1%%: %s" % (name,solver,key,error))
                if previous:
                    check(abs(value[key]/previous[solver][key]-1) < .001,
                          "%s %s %s in-plane refinement 0.1%%" % (name,solver,key))
                check(abs(value[key]/values["fat"][key]-1) < 1E-5,
                      "%s %s solver disagreement" % (name,key))
            # Reciprocal compliance under independent x and y load cases.
            check(abs(value["nu_xy"]/value["Ex"] /
                      (value["y_solve"]["nu"]/value["Ey"])-1) < 1E-5,
                  name+" Poisson reciprocity")
            print("PASS %s %dx%d %s CLT relative errors %s" % (name,n,n,solver,errors))
        case["meshes"][str(n)] = values
        previous = values
    periodic_results[name] = case

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
               "periodic": periodic_results, "failures": failures}, stream, indent=2)
check(not failures, "; ".join(failures))
print("TEST PASSED")
