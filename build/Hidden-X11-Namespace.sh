#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=${1:?repository};shift
mount -t tmpfs -o mode=1777 ppm-l2b-x11 /tmp/.X11-unix
unset DISPLAY WAYLAND_DISPLAY
exec nice -n 19 python3 "$repo/tests/L2b/embedding_matrix.py" "$@"
