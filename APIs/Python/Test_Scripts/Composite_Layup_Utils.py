"""Headless M2a fixtures and independent deck/result readers. No third-party imports."""
import os
import re
import subprocess


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
    pmx.model.materials.add("IM7_8552", pmx.create.elastic_engineering_constants([
        [171000, 9100, 9100, 0.32, 0.32, 0.52, 5300, 5300, 3000, 0]]))


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
    directory = os.path.join(os.path.dirname(deck), label)
    os.makedirs(directory, exist_ok=True)
    basename = os.path.join(directory, "coupon")
    with open(deck) as stream:
        text = stream.read()
    with open(basename + ".inp", "w") as stream:
        stream.write(text)
    env = dict(os.environ, OMP_NUM_THREADS="1", CCX_NPROC_RESULTS="1")
    result = subprocess.run([executable, "coupon"], cwd=directory, env=env,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=120)
    output = result.stdout.decode("utf-8", "replace")
    with open(basename + ".solver.log", "w") as stream:
        stream.write(output)
    check(result.returncode == 0 and "*ERROR" not in output, "solver " + label + ": " + output[-1500:])
    with open(basename + ".dat") as stream:
        dat = stream.read()
    matches = re.findall(r"total force.*?\n\s*\n\s*([\d.E+\-]+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)", dat, re.I | re.S)
    check(matches, "missing reaction force totals " + label)
    modulus = float(matches[-1][0 if direction == "x" else 1]) / area / strain
    rows = re.findall(r"displacements.*?\n\s*\n\s*\d+\s+([\d.E+\-]+)\s+([\d.E+\-]+)\s+([\d.E+\-]+)", dat, re.I | re.S)
    gamma = float(rows[-1][1]) / length if rows else None
    print("SOLVE %s E=%.8f MPa gamma_xy=%s" % (label, modulus, gamma))
    return modulus, gamma
