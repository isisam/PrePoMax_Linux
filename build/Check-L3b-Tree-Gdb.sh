#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
for n in $(seq 1 "${2:-8}");do
    xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/tree_ui.py" \
        /opt/prepomax-rcm "$repo/../logs/L2b/dcb/dcb.pmx" "$repo/../logs/L3b/shutdown/${1:-debian-tree-gdb}/$n" --gdb || true
done
