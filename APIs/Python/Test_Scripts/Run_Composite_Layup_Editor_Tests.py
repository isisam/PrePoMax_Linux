"""Headless M2b editor/data/persistence checks, optionally including M1 and M2a regressions."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
HIDDEN = {"creationflags": 0x08000000} if os.name == "nt" else {}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, required=True)
    parser.add_argument("--work", type=Path, required=True)
    parser.add_argument("--regressions", action="store_true")
    args = parser.parse_args()
    scripts = Path(__file__).resolve().parent
    exe = args.exe.resolve()
    work = args.work.resolve()
    work.mkdir(parents=True, exist_ok=True)
    results = {}
    env = dict(os.environ, OMP_NUM_THREADS="1", MKL_NUM_THREADS="1", OPENBLAS_NUM_THREADS="1")

    def command(label, cmd, timeout=660):
        process = subprocess.run(cmd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=timeout, **HIDDEN)
        output = process.stdout.decode("utf-8", "replace")
        (work / (label + ".log")).write_text(output, encoding="utf-8")
        results[label] = {"exit": process.returncode, "passed": process.returncode == 0}
        print(label, "PASS" if process.returncode == 0 else "FAIL", "exit", process.returncode, flush=True)
        if process.returncode:
            print(output[-3500:], flush=True)
        return process.returncode == 0 and "TEST PASSED" in output

    def application(label, script, model):
        report = work / (label + ".json")
        if report.exists(): report.unlink()
        success = command(label, [str(exe), "-f", str(model), "-u", "MM_TON_S_C", "--script", str(scripts / script),
            "-g", "No", "-x", "Yes", "-w", str(work), "--report", str(report), "--timeout", "600"])
        data = json.loads(report.read_text(encoding="utf-8-sig")) if report.exists() else {}
        results[label]["passed"] = (success and data.get("exitStatus") == "Ok"
            and data.get("invocation", {}).get("settingsSaved") is False
            and data.get("invocation", {}).get("showGui") == "No")
        return results[label]["passed"]

    if application("editor", "Test_Model_Composite_Layup_Editor.py", scripts / "Models/cube.inp"):
        application("reload", "Composite_Layup_Editor_Reload.py", work / "M2b_layup.pmx")
    if args.regressions:
        command("M1", [sys.executable, "-B", str(scripts / "Run_Tests.py"), "--exe", str(exe), "--work", str(work / "M1"),
            "--filter", "Test_Model_Materials_Sections.py", "Test_Model_User_Material.py", "Test_Solver_Ccx_Fat_Defaults.py", "Test_Solver_Ccx_Fat_Warning.py"])
        command("M2a", [sys.executable, "-B", str(scripts / "Run_Composite_Layup_Tests.py"), "--exe", str(exe), "--work", str(work / "M2a")], timeout=1800)
    (work / "summary.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    passed = bool(results) and all(r["passed"] for r in results.values())
    print("TEST PASSED" if passed else "TEST FAILED", flush=True)
    return 0 if passed else 1

if __name__ == "__main__":
    sys.exit(main())
