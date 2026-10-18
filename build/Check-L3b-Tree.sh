#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=/root/ppm-l3b
logs="$repo/../logs/L3b"
export DOTNET_PROCESSOR_COUNT=4 LP_NUM_THREADS=2 OMP_NUM_THREADS=1
rsync -a "$repo/PrePoMax.Avalonia/" "$out/src/PrePoMax.Avalonia/"
nice -n 19 dotnet publish "$out/src/PrePoMax.Avalonia" -c Release -p:CoreTargetFrameworks=net8.0 -m:1 -r linux-x64 --self-contained true -o "$out/app"
for pair in M2a DCB; do
    fixture="$repo/../logs/M2b_acceptance/M2b_layup.pmx"
    [[ "$pair" != DCB ]] || fixture="$repo/../logs/L2b/dcb/dcb.pmx"
    nice -n 19 "$out/app/PrePoMax.Avalonia" --tree-check "$fixture" "$logs/tree-$pair.json"
done
xvfb-run -a -s '-screen 0 1600x1100x24' nice -n 19 python3 "$repo/tests/L3b/tree_ui.py" "$out/app" "$repo/../logs/L2b/dcb/dcb.pmx" "$logs/tree-ui"
