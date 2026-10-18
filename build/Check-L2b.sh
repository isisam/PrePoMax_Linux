#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -euo pipefail
repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
out=${1:-"$HOME/ppm-l2b"}
export LD_LIBRARY_PATH="$out/app"
# Test-only resource budget; delivered launcher inherits the user's choices.
export LP_NUM_THREADS=8 OMP_NUM_THREADS=8 DOTNET_PROCESSOR_COUNT=8
mkdir -p "$out/src/tests/L2b"
rsync -a "$repo/tests/L2b/" "$out/src/tests/L2b/"
rsync -a --exclude=obj --exclude=bin "$repo/PrePoMax.Viewer/" "$out/src/PrePoMax.Viewer/"
rsync -a --exclude=obj --exclude=bin "$repo/Vtkmax.Interop/" "$out/src/Vtkmax.Interop/"
python3 "$repo/tests/L2b/standard_views.py" "$out/app/libvtkmax.so"
xvfb-run -a python3 "$repo/tests/L2a/c_api_checks.py" "$out/app/libvtkmax.so"
nice -n 19 dotnet build "$out/src/tests/L2b/ViewerChecks.csproj" -c Release -p:CoreTargetFrameworks=net8.0 -m:1
env -u DISPLAY nice -n 19 dotnet "$out/src/tests/L2b/bin/Release/net8.0/ViewerChecks.dll" "$repo/../logs/L2b/viewer" "$repo/tests/L2b/fixtures" \
  "$repo/../logs/L2b/M2a/M2a_golden.inp" "$repo/../logs/L1a_linux_handoff/data/dcb_fatczm_static.inp"
