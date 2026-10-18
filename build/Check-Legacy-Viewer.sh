#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2b"}
export LD_LIBRARY_PATH="$out/app" LP_NUM_THREADS=8 DOTNET_PROCESSOR_COUNT=8 OMP_NUM_THREADS=8
rsync -a "$repo/tests/L2a/" "$out/src/tests/L2a/"
nice -n 19 dotnet build "$out/src/tests/L2a/ViewerChecks.csproj" -c Release -p:CoreTargetFrameworks=net8.0 -m:1
cc -std=c11 -I "$repo/native/vtkmax" "$repo/tests/L2a/c_header.c" -L "$out/app" -Wl,-rpath,"$out/app" -lvtkmax -o "$out/c-header"
"$out/c-header"
xvfb-run -a dotnet "$out/src/tests/L2a/bin/Release/net8.0/ViewerChecks.dll" "$repo/../logs/L2b/legacy-viewer" "$repo/../logs/L2b/M2a/M2a_golden.inp" "$repo/../logs/L1a_linux_handoff/data/dcb_fatczm_static.inp"
