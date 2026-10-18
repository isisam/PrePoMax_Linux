#!/usr/bin/env bash
set -euo pipefail
sudo -n env DEBIAN_FRONTEND=noninteractive nice -n 19 apt-get install -y --no-install-recommends gdb
env -u DISPLAY gdb --batch -ex run -ex bt --args python3 "$(dirname "$0")/../tests/L2b/egl_probe.py" /root/ppm-l2b/app/libvtkmax.so
