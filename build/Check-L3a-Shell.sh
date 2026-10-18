#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
export DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=4 OMP_NUM_THREADS=1 PPM_USE_PACKAGED_SHELL=1
xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L2c/shell_checks.py" \
    "${1:-/root/ppm-l3a}" "$repo/../logs/L3a/shell" "$repo/../logs/L2b/dcb/dcb.pmx" "$repo/../logs/L2b/dcb/dcb.frd"
nice -n 19 "$repo/../logs/L3a/headless-linux/prepomax-headless" inspect "$repo/../logs/L3a/shell/saved.pmx"
