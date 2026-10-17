"""Headless M2a fixtures and independent deck/result readers. No third-party imports."""
import os
import re
import subprocess
import math

HIDDEN = {"creationflags": 0x08000000} if os.name == "nt" else {}
# E1,E2,E3,nu12,nu13,nu23,G12,G13,G23,temperature. Shared by FE and CLT.
LAMINA = (171000., 9100., 9100., .32, .32, .52, 5300., 5300., 3000., 0.)


def check(value, message):
    if not value:
        raise Exception("TEST FAILED: " + message)


def blocks(text):
    result = []
    for line in text.splitlines():
        line = line.strip()
        if not line or line.startswith("**"):
            continue
        if line.startswith("*"):
            fields = [s.strip().upper() for s in line.split(",")]
            result.append([fields[0], dict(s.split("=", 1) for s in fields[1:] if "=" in s), []])
        else:
            result[-1][2].append(line)
    return result


def ply_cards(text):
    sets, orientations, sections = {}, {}, []
    for keyword, options, rows in blocks(text):
        if keyword == "*ELSET":
            sets[options["ELSET"]] = tuple(int(s) for r in rows for s in r.split(",") if s.strip())
        elif keyword == "*ORIENTATION":
            orientations[options["NAME"]] = tuple(float(s) for r in rows for s in r.split(",") if s.strip())
        elif keyword == "*SOLID SECTION" and "ORIENTATION" in options:
            sections.append(options)
    return sorted((sets[s["ELSET"]], s["MATERIAL"], orientations[s["ORIENTATION"]]) for s in sections)


def material(pmx):
    pmx.model.materials.add("IM7_8552", pmx.create.elastic_engineering_constants([list(LAMINA)]))


def clear(pmx):
    # Batch scripts are recorded commands: new_model/open intentionally refuse to replace their history.
    for name in list(pmx.model.composite_layups.get_names()):
        pmx.model.composite_layups.remove(name)
    for collection in (pmx.model.sections, pmx.model.parts, pmx.model.element_sets,
                       pmx.model.node_sets, pmx.model.materials, pmx.model.coordinate_systems):
        names = list(collection.get_names())
        if names:
            collection.remove(names)
    pmx.model.keywords.replace_user_keywords([])
    if pmx.model.keywords.override_count:
        pmx.model.keywords.clear_overrides()


def mesh(pmx, label, levels, nx=4, ny=2, lx=4., ly=2., omit=(), mapping=(0, 1, 2)):
    """One C3D8I layer per supplied interval, shared nodes; optional omitted layers for negative tests."""
    clear(pmx)
    npl = (nx + 1) * (ny + 1)
    nid = lambda k, j, i: 1 + k * npl + j * (nx + 1) + i
    lines = ["*NODE"]
    for k, z in enumerate(levels):
        for j in range(ny + 1):
            for i in range(nx + 1):
                xyz = [lx * i / nx, ly * j / ny, z]
                xyz = [xyz[mapping[q]] for q in range(3)]
                lines.append("%d,%.16g,%.16g,%.16g" % (nid(k, j, i), *xyz))
    lines.append("*ELEMENT,TYPE=C3D8I,ELSET=COUPON")
    eid = 0
    for k in range(len(levels) - 1):
        for j in range(ny):
            for i in range(nx):
                eid += 1
                if k in omit:
                    continue
                n = [nid(k, j, i), nid(k, j, i + 1), nid(k, j + 1, i + 1), nid(k, j + 1, i)]
                n += [x + npl for x in n]
                lines.append(",".join(map(str, [eid] + n)))
    sets = {
        "X0": [nid(k, j, 0) for k in range(len(levels)) for j in range(ny + 1)],
        "X1": [nid(k, j, nx) for k in range(len(levels)) for j in range(ny + 1)],
        "Y0": [nid(k, 0, i) for k in range(len(levels)) for i in range(nx + 1)],
        "Y1": [nid(k, ny, i) for k in range(len(levels)) for i in range(nx + 1)],
        "ORIGIN": [nid(0, 0, 0)], "YPOINT": [nid(0, ny, 0)],
        "XPOINT": [nid(0, 0, nx)],
    }
    for name, ids in sets.items():
        lines.append("*NSET,NSET=" + name)
        lines += [",".join(map(str, ids[i:i + 16])) for i in range(0, len(ids), 16)]
    path = os.path.join(pmx.work_directory, label + "_mesh.inp")
    with open(path, "w") as stream:
        stream.write("\n".join(lines) + "\n")
    pmx.import_file(path)
    material(pmx)
    return pmx.create.region("ElementSetName", "COUPON")


def plies(pmx, angles, thickness=0.125, count=1):
    return [pmx.create.composite_ply("P%02d" % (i + 1), "IM7_8552", thickness, angle, count)
            for i, angle in enumerate(angles)]


def steps(pmx, length=4., shear_free=False, direction="x"):
    # Minimal rigid-motion restraints. Removing XPOINT uy admits off-axis extension/shear coupling.
    text = "*STEP\n*STATIC,SOLVER=SPOOLES\n1.,1.\n*BOUNDARY\nX0,1,1,0\nORIGIN,2,3,0\nYPOINT,3,3,0\n"
    if not shear_free:
        text += "XPOINT,2,2,0\n"
    text += "X1,1,1,%.16g\n*NODE PRINT,NSET=X1,TOTALS=ONLY\nRF\n" % (0.0001 * length)
    text += "*NODE PRINT,NSET=XPOINT\nU\n*NODE FILE\nU\n*EL FILE,GLOBAL=YES\nE\n*END STEP\n"
    if direction == "y":
        text = "*STEP\n*STATIC,SOLVER=SPOOLES\n1.,1.\n*BOUNDARY\nY0,2,2,0\nORIGIN,1,1,0\nORIGIN,3,3,0\nXPOINT,3,3,0\n"
        if not shear_free:
            text += "YPOINT,1,1,0\n"
        text += "Y1,2,2,%.16g\n*NODE PRINT,NSET=Y1,TOTALS=ONLY\nRF\n*NODE FILE\nU\n*END STEP\n" % (0.0001 * length)
    roots = [k for k in pmx.model.keywords.get_model_keywords() if len(k.indices) == 1]
    pmx.model.keywords.set_user_keyword([max(list(k.indices)[0] for k in roots) + 1], text)


def solve(executable, deck, label, area=2., strain=0.0001, length=4., direction="x"):
    basename = run_solver(executable, deck, label)
    with open(basename + ".dat") as stream:
        dat = stream.read()
    matches = re.findall(r"total force.*?\n\s*\n\s*([\d.E+\-]+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)", dat, re.I | re.S)
    check(matches, "missing reaction force totals " + label)
    modulus = float(matches[-1][0 if direction == "x" else 1]) / area / strain
    rows = re.findall(r"displacements.*?\n\s*\n\s*\d+\s+([\d.E+\-]+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)", dat, re.I | re.S)
    gamma = float(rows[-1][1]) / length if rows else None
    print("SOLVE %s E=%.8f MPa gamma_xy=%s" % (label, modulus, gamma))
    return modulus, gamma


def run_solver(executable, deck, label):
    with open(deck) as stream:
        text = stream.read()
    # The stock binary has no FATCZM UMAT. Refuse before launching rather than
    # misreporting a constitutive incompatibility as a numerical failure.
    has_fatczm = any(k == "*MATERIAL" and o.get("NAME", "").startswith("USERFATCZM")
                     for k, o, _ in blocks(text))
    check(not has_fatczm or os.path.basename(executable).lower() == "ccx_fat.exe",
          "FATCZM deck requires ccx_fat.exe: " + label)
    directory = os.path.join(os.path.dirname(deck), label)
    os.makedirs(directory, exist_ok=True)
    basename = os.path.join(directory, "coupon")
    with open(basename + ".inp", "w") as stream:
        stream.write(text)
    env = dict(os.environ, OMP_NUM_THREADS="1", CCX_NPROC_RESULTS="1")
    result = subprocess.run([executable, "coupon"], cwd=directory, env=env,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=120, **HIDDEN)
    output = result.stdout.decode("utf-8", "replace")
    with open(basename + ".solver.log", "w") as stream:
        stream.write(output)
    check(result.returncode == 0 and "*ERROR" not in output, "solver " + label + ": " + output[-1500:])
    return basename


def clt(ply_data, lamina=LAMINA):
    """Independent plane-stress Qbar integration; (angle, exact thickness) per ply.

    All acceptance laminates are symmetric, hence B=0 and membrane compliance
    is inverse(A). No nominal laminate thickness or precomputed modulus is used.
    """
    e1, e2, _, nu12, _, _, g12 = lamina[:7]
    d = 1 - nu12 * nu12 * e2 / e1
    q11, q22, q12, q66 = e1 / d, e2 / d, nu12 * e2 / d, g12
    a = [[0.] * 3 for _ in range(3)]
    h = sum(t for _, t in ply_data)
    for angle, t in ply_data:
        c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
        c2, s2 = c*c, s*s
        q16 = (q11-q12-2*q66)*c*c2*s - (q22-q12-2*q66)*c*s*s2
        q26 = (q11-q12-2*q66)*c*s*s2 - (q22-q12-2*q66)*c*c2*s
        qb = [[q11*c2*c2 + 2*(q12+2*q66)*c2*s2 + q22*s2*s2,
               (q11+q22-4*q66)*c2*s2 + q12*(c2*c2+s2*s2), q16],
              [0, q11*s2*s2 + 2*(q12+2*q66)*c2*s2 + q22*c2*c2, q26],
              [0, 0, (q11+q22-2*q12-2*q66)*c2*s2 + q66*(c2*c2+s2*s2)]]
        for i in range(3):
            for j in range(3):
                a[i][j] += t * qb[min(i,j)][max(i,j)]
    # Gauss-Jordan inverse, independent of solver/native material implementation.
    aug = [a[i][:] + [float(i == j) for j in range(3)] for i in range(3)]
    for i in range(3):
        pivot = aug[i][i]
        aug[i] = [v / pivot for v in aug[i]]
        for j in range(3):
            if i != j:
                f = aug[j][i]
                aug[j] = [v - f*w for v, w in zip(aug[j], aug[i])]
    compliance = [row[3:] for row in aug]
    return {"Ex": 1/(h*compliance[0][0]), "Ey": 1/(h*compliance[1][1]),
            "nu_xy": -compliance[0][1]/compliance[0][0], "A": a, "H": h}


def periodic_deck(path, nx, ny, levels, lx=4., ly=2., direction="x", strain=.0001):
    """Append in-plane periodic MPCs to a writer-exported mesh/material/layup.

    ux jump across X = RX.1; uy jump across Y = RY.2;
    ux jump across Y = RY.1 (free engineering shear); uy jump across X = 0
    fixes the rigid rotation only. All uz jumps are zero. Top/bottom are free.
    Corner equations are included once, so each dependent DOF occurs once.
    """
    with open(path) as stream:
        source = stream.read()
    check(not any(k == "*STEP" for k, _, _ in blocks(source)), "periodic fixture must have no steps")
    npl = (nx+1)*(ny+1)
    nid = lambda k,j,i: 1+k*npl+j*(nx+1)+i
    rx, ry = len(levels)*npl+1, len(levels)*npl+2
    lines = ["*NODE", "%d,0,0,0" % rx, "%d,0,0,0" % ry,
             "*NSET,NSET=MACRO", "%d,%d" % (rx,ry), "*EQUATION"]
    def equation(high, low, dof, ref=None):
        lines.extend(["3" if ref else "2", "%d,%d,1,%d,%d,-1" % (high,dof,low,dof)
                      + (",%d,%d,-1" % ref if ref else "")])
    for k in range(len(levels)):
        for j in range(ny+1):
            for dof in (1,2,3):
                equation(nid(k,j,nx), nid(k,j,0), dof, (rx,1) if dof == 1 else None)
        for i in range(nx):
            for dof in (1,2,3):
                equation(nid(k,ny,i), nid(k,0,i), dof, (ry,dof) if dof < 3 else None)
    # No reference-node RF is used: MPC forces are not reliable RF output in ccx.
    # Integrate global integration-point stress over exact ply volumes instead.
    lines += ["*STEP", "*STATIC,SOLVER=SPOOLES", "1.,1.", "*BOUNDARY", "1,1,3,0",
              "%d,2,3,0" % rx, "%d,3,3,0" % ry]
    if direction == "x":
        lines += ["%d,1,1,%.16g" % (rx,strain*lx)]
    else:
        lines += ["%d,2,2,%.16g" % (ry,strain*ly)]
    lines += ["*NODE PRINT,NSET=MACRO", "U", "*EL PRINT,ELSET=COUPON,GLOBAL=YES", "S",
              "*END STEP"]
    with open(path, "w") as stream:
        stream.write(source + "\n" + "\n".join(lines) + "\n")
    return rx, ry


def solve_periodic(executable, deck, label, nx, ny, levels, lx=4., ly=2., direction="x", strain=.0001):
    basename = run_solver(executable, deck, label)
    with open(basename + ".dat") as stream:
        dat = stream.read()
    stresses, displacements = {}, {}
    mode = None
    for line in dat.splitlines():
        if "stresses (" in line:
            mode = "S"
        elif "displacements (" in line:
            mode = "U"
        fields = line.split()
        if mode == "S" and len(fields) == 8 and fields[0].isdigit() and fields[1].isdigit():
            key = (int(fields[0]), int(fields[1]))
            check(key not in stresses, "duplicate integration point " + label)
            stresses[key] = list(map(float, fields[2:]))
        elif mode == "U" and len(fields) == 4 and fields[0].isdigit():
            displacements[int(fields[0])] = list(map(float, fields[1:]))
    count = nx*ny*(len(levels)-1)
    check(set(stresses) == {(e,ip) for e in range(1,count+1) for ip in range(1,9)},
          "incomplete C3D8I stress output " + label)
    avg = [0.] * 6
    for (e, _), stress in stresses.items():
        k = (e-1)//(nx*ny)
        weight = (levels[k+1]-levels[k])/(levels[-1]-levels[0])/(nx*ny*8)
        avg = [a+weight*s for a,s in zip(avg,stress)]
    rx = len(levels)*(nx+1)*(ny+1)+1
    ex, ey, gamma = displacements[rx][0]/lx, displacements[rx+1][1]/ly, displacements[rx+1][0]/ly
    axial = 0 if direction == "x" else 1
    measured = ex if axial == 0 else ey
    check(abs(measured/strain-1) < 1E-6, "prescribed macro strain " + label)
    check(max(abs(avg[i]) for i in range(6) if i != axial)/abs(avg[axial]) < 1E-5,
          "nonzero mean transverse/shear stress " + label + ": " + str(avg))
    result = {"E": avg[axial]/measured, "nu": -ey/ex if axial == 0 else -ex/ey,
              "macro_strain": [ex,ey,gamma], "mean_stress": avg, "integration_points": len(stresses)}
    print("PERIODIC %s E=%.8f MPa nu=%.8f" % (label,result["E"],result["nu"]))
    return result
