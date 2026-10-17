"""Acceptance checks using the Task A fixtures and optional net48 PMX/real solver.

python verify_cli.py --cli path/to/prepomax-cli[.dll|.exe] --fixtures path/to/linux/tests/data --work output
Add --net48-pmx file.pmx and --solver /usr/bin/ccx for the cross-runtime and solver gates.
No packages or system changes. Every Windows child is hidden; solver uses one CPU.
"""
import argparse
import hashlib
import os
from pathlib import Path
import subprocess


def material_cards(path):
    cards, active = [], None
    for line in path.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("**"):
            continue
        if line.startswith("*"):
            if active is not None:
                cards.append(active)
            active = [line.replace(" ", "").upper()] if line.upper().startswith(("*USER MATERIAL", "*DEPVAR")) else None
        elif active is not None:
            active.append(line.replace(" ", "").upper())
    if active is not None:
        cards.append(active)
    return sorted("".join(card) for card in cards)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cli", type=Path, required=True)
    parser.add_argument("--fixtures", type=Path, required=True)
    parser.add_argument("--work", type=Path, required=True)
    parser.add_argument("--net48-pmx", type=Path)
    parser.add_argument("--solver", type=Path)
    args = parser.parse_args()
    args.work.mkdir(parents=True, exist_ok=True)
    command = ["dotnet", str(args.cli.resolve())] if args.cli.suffix == ".dll" else [str(args.cli.resolve())]
    hidden = {"creationflags": 0x08000000} if os.name == "nt" else {}

    def run(label, *arguments):
        result = subprocess.run(command + list(map(str, arguments)), stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, timeout=180, env={**os.environ, "OMP_NUM_THREADS": "1",
                                "DOTNET_PROCESSOR_COUNT": "8"}, **hidden)
        (args.work / (label + ".log")).write_bytes(result.stdout)
        if result.returncode:
            raise AssertionError(label + ": " + result.stdout.decode("utf-8", "replace")[-4000:])
        print("PASS", label, flush=True)

    run("help", "--help")
    for name in ("cantilever_c3d8", "dcb_fatczm_static"):
        source = args.fixtures / (name + ".inp")
        exported, saved, reloaded = [args.work / (name + suffix) for suffix in (".inp", ".pmx", "_reload.inp")]
        run(name, "import", source, "export-inp", exported, "save", saved)
        run(name + "_reload", "import", saved, "export-inp", reloaded)
        assert exported.read_bytes() == reloaded.read_bytes(), name + " changed after PMX reload"
        assert material_cards(source) == material_cards(exported), name + " UMAT/DEPVAR cards changed"
        print("SHA256", name, hashlib.sha256(exported.read_bytes()).hexdigest())
    if args.net48_pmx:
        run("net48_pmx", "import", args.net48_pmx, "export-inp", args.work / "net48.inp", "save", args.work / "from_net48.pmx")
        run("net48_pmx_reload", "import", args.work / "from_net48.pmx", "export-inp", args.work / "net48_reload.inp")
        assert (args.work / "net48.inp").read_bytes() == (args.work / "net48_reload.inp").read_bytes()
    if args.solver:
        run("solve", "run", args.work / "cantilever_c3d8.inp", "--solver", args.solver, "--cpus", "1")
        run("results", "results", args.work / "cantilever_c3d8.frd", "--summary")
        result = (args.work / "results.log").read_text()
        assert "DISP" in result and "STRESS" in result and "min=" in result
    print("ALL REQUESTED CLI CHECKS PASS", flush=True)


if __name__ == "__main__":
    main()
