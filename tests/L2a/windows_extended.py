"""Existing L1a runtime/process and M1 300-jump checks after windows_regression.py."""
from pathlib import Path
import json
import os
import shutil
import subprocess
import sys
import time

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
repo = Path(__file__).resolve().parents[2]
logs = repo.parent / "logs"
runtime = logs / "L2a_windows_runtime"
results = []
def run(name, command):
    start = time.monotonic()
    with (logs / ("L2a_" + name + ".log")).open("wb") as f:
        result = subprocess.run(list(map(str, command)), cwd=repo, stdout=f, stderr=subprocess.STDOUT, creationflags=0x08000000,
                                env=dict(os.environ, DOTNET_PROCESSOR_COUNT="4", OMP_NUM_THREADS="1"), timeout=900)
    results.append(dict(name=name, exit=result.returncode, seconds=round(time.monotonic()-start, 2)))
    (logs / "L2a_windows_extended_summary.json").write_text(json.dumps(results, indent=2))
    print(results[-1], flush=True)
    if result.returncode:
        raise RuntimeError(name + " failed; see its log")

run("windows_runtime_publish", ["dotnet.exe", "publish", "tests/L1a/RuntimeChecks.csproj", "-c", "Release", "-p:CoreTargetFrameworks=net8.0", "-m:1",
                               "-r", "win-x64", "--self-contained", "true", "-o", logs / "L2a_windows_runtime_checks"])
run("windows_L1a_runtime", [logs / "L2a_windows_runtime_checks/RuntimeChecks.exe"])
run("windows_L1a_processes", [sys.executable, "-B", repo / "tests/L1a/verify_processes.py", "--cli", logs / "L2a_windows_cli/prepomax-cli.exe",
                             "--fixture", logs / "L2a_windows_runtime_checks/RuntimeChecks.exe", "--work", logs / "L2a_windows_processes"])
e2e = logs / "L2a_M1_e2e"
e2e.mkdir(exist_ok=True)
for filename in ("M1_mesh.inp", "M1_package_steps.txt"):
    shutil.copy2(logs / "M1_e2e_work" / filename, e2e / filename)
run("M1_e2e", [runtime / "PrePoMax.com", "-f", e2e / "M1_mesh.inp", "-u", "MM_TON_S_C", "--script", logs / "M1_e2e.py",
               "-g", "No", "-x", "Yes", "-w", e2e, "--settings", logs / "L2a_M1_suite/M1_spooles_settings.bin",
               "--report", e2e / "run.json", "--timeout", "600"])
report = json.loads((e2e / "run.json").read_text(encoding="utf-8-sig"))
assert report["exitStatus"] == "Ok" and report["invocation"]["settingsSaved"] is False
print("ALL EXTENDED WINDOWS REGRESSIONS PASS", flush=True)
