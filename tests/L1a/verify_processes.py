"""Windows net8 CaeJob failure, timeout/tree kill and abrupt-parent-death checks.
Usage: python verify_processes.py --cli path/to/prepomax-cli.exe --fixture path/to/RuntimeChecks.exe --work output
Fixtures only; this does not validate a physical solver. Every child is hidden.
"""
import argparse
import ctypes
import os
from pathlib import Path
import subprocess
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cli", type=Path, required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--work", type=Path, required=True)
    args = parser.parse_args()
    assert os.name == "nt", "Windows process acceptance only"
    args.work.mkdir(parents=True, exist_ok=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.argtypes = [ctypes.c_uint32, ctypes.c_int, ctypes.c_uint32]
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.WaitForSingleObject.argtypes = [ctypes.c_void_p, ctypes.c_uint32]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    hidden = {"creationflags": 0x08000000}

    def wait_dead(pid):
        handle = kernel.OpenProcess(0x100000, False, pid)
        if not handle:
            assert ctypes.get_last_error() == 87, "Cannot inspect fixture PID"
            return
        try:
            assert kernel.WaitForSingleObject(handle, 5000) == 0, "Fixture process survived: " + str(pid)
        finally:
            kernel.CloseHandle(handle)

    def command(name, timeout):
        deck = args.work / (name + ".inp")
        deck.write_text("** dummy deck; process fixture only\n")
        return [str(args.cli.resolve()), "run", str(deck.resolve()), "--solver", str(args.fixture.resolve()), "--timeout", str(timeout)]

    for name, timeout, status in [("L1a_fake_failure", 10, "Failed"), ("L1a_fake_timeout", 1, "TimedOut")]:
        result = subprocess.run(command(name, timeout), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=20, **hidden)
        (args.work / (name + ".log")).write_bytes(result.stdout)
        assert result.returncode == 2 and ("status=" + status) in result.stdout.decode(), result.stdout.decode()
        if status == "TimedOut":
            for pidfile in args.work.glob("*.pid"):
                wait_dead(int(pidfile.read_text()))
                pidfile.unlink()
        print("PASS", name, flush=True)

    process = subprocess.Popen(command("L1a_fake_parent_death", 30), stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, **hidden)
    try:
        deadline = time.monotonic() + 10
        while len(list(args.work.glob("*.pid"))) < 2 and time.monotonic() < deadline:
            time.sleep(0.05)
        pids = [int(p.read_text()) for p in args.work.glob("*.pid")]
        assert len(pids) == 2, "Process fixtures did not start"
        process.kill()  # Windows TerminateProcess: no managed cleanup/finally executes.
        process.wait(timeout=10)
        for pid in pids:
            wait_dead(pid)
        print("PASS abrupt CLI death kills fixture and descendant via Windows job object", flush=True)
    finally:
        if process.poll() is None:
            process.kill()
            process.wait()


if __name__ == "__main__":
    main()
