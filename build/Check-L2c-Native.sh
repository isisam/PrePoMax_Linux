#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-/root/ppm-l2c}
export LD_LIBRARY_PATH="$out/app" LP_NUM_THREADS=4 OMP_NUM_THREADS=4 DOTNET_PROCESSOR_COUNT=8
python3 "$repo/tests/L2b/standard_views.py" "$out/app/libvtkmax.so"
xvfb-run -a nice -n 19 python3 "$repo/tests/L2a/c_api_checks.py" "$out/app/libvtkmax.so"
for suite in L2a L2b;do
    rsync -a "$repo/tests/$suite/" "$out/src/tests/$suite/"
    nice -n 19 dotnet build "$out/src/tests/$suite/ViewerChecks.csproj" -c Release -p:CoreTargetFrameworks=net8.0 -m:1
done
cc -std=c11 -I "$repo/native/vtkmax" "$repo/tests/L2a/c_header.c" -L "$out/app" -Wl,-rpath,"$out/app" -lvtkmax -o "$out/c-header"
"$out/c-header"
xvfb-run -a nice -n 19 dotnet "$out/src/tests/L2a/bin/Release/net8.0/ViewerChecks.dll" "$repo/../logs/L2c/legacy-viewer" "$repo/../logs/L2b/M2a/M2a_golden.inp" "$repo/../logs/L1a_linux_handoff/data/dcb_fatczm_static.inp"
env -u DISPLAY nice -n 19 dotnet "$out/src/tests/L2b/bin/Release/net8.0/ViewerChecks.dll" "$repo/../logs/L2c/native-viewer" "$repo/tests/L2b/fixtures" "$repo/../logs/L2b/M2a/M2a_golden.inp" "$repo/../logs/L1a_linux_handoff/data/dcb_fatczm_static.inp"
