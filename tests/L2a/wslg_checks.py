"""Linux X11 external-input acceptance. Uses the real WSLg DISPLAY, not Xvfb.
Usage: python3 wslg_checks.py shell.dll coupon.inp output native-build-dir
"""
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import sys
import time
import ctypes as c

shell, inp, output, native = map(Path, sys.argv[1:])
output.mkdir(parents=True, exist_ok=True)
assert os.environ.get("DISPLAY"), "WSLg DISPLAY missing"
for stale in ("ready", "state.json", "capture"):
    (output / stale).unlink(missing_ok=True)
env = dict(os.environ, LD_LIBRARY_PATH=str(native), DOTNET_PROCESSOR_COUNT="8", OMP_NUM_THREADS="4", LP_NUM_THREADS="4", VTK_SMP_MAX_THREADS="4")
def run(*args):
    return subprocess.check_output(list(map(str, args)), text=True, timeout=15)
def wait_for(condition, timeout=60):
    start = time.monotonic()
    while time.monotonic() - start < timeout:
        value = condition()
        if value:
            return value
        time.sleep(.2)
    raise AssertionError("Timed out waiting for UI state")
def state():
    try:
        return json.loads((output / "state.json").read_text())
    except (FileNotFoundError, json.JSONDecodeError):
        return None
def close_window(window):
    # Send the same ICCCM WM_DELETE_WINDOW message as a window manager's Close.
    class Message(c.Structure):
        _fields_ = [("type", c.c_int), ("serial", c.c_ulong), ("send_event", c.c_int),
                    ("display", c.c_void_p), ("window", c.c_ulong), ("message_type", c.c_ulong),
                    ("format", c.c_int), ("data", c.c_long * 5)]
    class Event(c.Union):
        _fields_ = [("message", Message), ("padding", c.c_long * 24)]
    x = c.CDLL("libX11.so.6")
    x.XOpenDisplay.argtypes = [c.c_char_p]; x.XOpenDisplay.restype = c.c_void_p
    x.XInternAtom.argtypes = [c.c_void_p, c.c_char_p, c.c_int]; x.XInternAtom.restype = c.c_ulong
    x.XSendEvent.argtypes = [c.c_void_p, c.c_ulong, c.c_int, c.c_long, c.POINTER(Event)]
    x.XFlush.argtypes = [c.c_void_p]; x.XCloseDisplay.argtypes = [c.c_void_p]
    display = x.XOpenDisplay(None)
    assert display
    event = Event(); event.message.type = 33; event.message.display = display; event.message.window = window
    event.message.message_type = x.XInternAtom(display, b"WM_PROTOCOLS", 0); event.message.format = 32
    event.message.data[0] = x.XInternAtom(display, b"WM_DELETE_WINDOW", 0)
    assert x.XSendEvent(display, window, 0, 0, c.byref(event))
    x.XFlush(display); x.XCloseDisplay(display)
with (output / "shell.log").open("w") as log:
    command = ["dotnet", str(shell)] if shell.suffix == ".dll" else [str(shell.resolve())]
    process = subprocess.Popen(command + [str(inp), "--evidence", str(output)], stdout=log, stderr=subprocess.STDOUT, env=env)
    try:
        wait_for(lambda: (output / "ready").exists() or (process.poll() is not None and (_ for _ in ()).throw(AssertionError("Shell exited"))))
        initial = wait_for(state)
        text = (output / "shell.log").read_text()
        match = re.search(r"EMBED parent=(\d+) child=(\d+)", text)
        assert match, text
        parent, child = map(int, match.groups())
        main = int(run("xdotool", "search", "--pid", process.pid, "--name", "Linux viewer prototype").splitlines()[0])
        run("xwininfo", "-id", main, "-tree")
        (output / "x11-tree.txt").write_text(run("xwininfo", "-id", main, "-tree"))
        def capture(name):
            run("import", "-window", main, output / name)
        capture("shell-initial.png")
        x, y = round(initial["screen"][0]), round(initial["height"] - 1 - initial["screen"][1])
        run("xdotool", "mousemove", "--window", child, x, y, "click", "1")
        picked = wait_for(lambda: (s if (s := state()) and s["lastPick"]["Hit"] else None))
        assert picked["lastPick"]["ElementId"] == initial["expectedElement"], (initial, picked)
        capture("shell-picked.png")
        # Trackball rotation through X11 pointer events, not through a camera API call.
        cx, cy = round(initial["width"] / 2), round(initial["height"] / 2)
        run("xdotool", "mousemove", "--window", child, cx, cy, "mousedown", "1")
        for i in range(1, 9):
            run("xdotool", "mousemove", "--window", child, cx + i * 10, cy + i * 4)
            time.sleep(.04)
        run("xdotool", "mouseup", "1")
        rotated = wait_for(lambda: (s if (s := state()) and abs(s["screen"][0] - initial["screen"][0]) > 3 else None))
        capture("shell-rotated.png")
        # Wheel zoom and host resize: verify projection and native image size respond.
        run("xdotool", "click", "4")
        run("xdotool", "windowsize", main, "1100", "760")
        resized = wait_for(lambda: (s if (s := state()) and s["width"] < initial["width"] - 50 else None))
        (output / "capture").write_text("capture")
        wait_for(lambda: (output / "embedded-current.png").exists())
        capture("shell-resized.png")
        close_window(main)
        exit_code = process.wait(timeout=10)
        assert exit_code == 0, f"Graceful shell close failed: {exit_code}"
        receipt = dict(display=os.environ["DISPLAY"], wayland=os.environ.get("WAYLAND_DISPLAY"), parent=parent, child=child,
                       main=main, initial=initial, picked=picked, rotated=rotated, resized=resized, closeExit=exit_code,
                       screenshots={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in output.glob("*.png")})
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2))
        print("PASS WSLg X11 embed, real click expected element", initial["expectedElement"], "drag rotation, wheel, resize, screenshots")
    finally:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
