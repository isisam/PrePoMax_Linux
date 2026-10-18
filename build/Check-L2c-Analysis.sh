#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l2c}
export DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=4 OMP_NUM_THREADS=1 PPM_CCX_FAT="$out/solver/ccx_fat"
xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L2c/shell_checks.py" "$out" "$repo/../logs/L2c/analysis" "$repo/../logs/L1a_linux_handoff/data/cantilever_c3d8.inp" /nonexistent.frd --analysis
